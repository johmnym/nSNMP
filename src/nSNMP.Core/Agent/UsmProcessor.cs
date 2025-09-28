using nSNMP.Security;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Message;

namespace nSNMP.Agent
{
    /// <summary>
    /// USM security processing for SNMPv3 agent
    /// </summary>
    public class UsmProcessor
    {
        private readonly SnmpEngine _engine;
        private readonly V3UserDatabase _userDatabase;

        public UsmProcessor(SnmpEngine engine, V3UserDatabase userDatabase)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _userDatabase = userDatabase ?? throw new ArgumentNullException(nameof(userDatabase));
        }

        /// <summary>
        /// Process incoming SNMPv3 message and extract authenticated PDU
        /// </summary>
        public UsmProcessResult ProcessIncomingMessage(byte[] messageData)
        {
            try
            {
                // Parse SNMPv3 message
                var v3Message = SnmpMessageV3.Parse(messageData);
                var usmParams = UsmSecurityParameters.Parse(v3Message.SecurityParameters.Data ?? Array.Empty<byte>());

                // Handle discovery requests
                if (usmParams.IsDiscovery)
                {
                    return new UsmProcessResult
                    {
                        IsDiscovery = true,
                        RequiresReport = true,
                        ReportError = UsmError.UnknownEngineId,
                        EngineParameters = _engine.GetParameters()
                    };
                }

                // Validate engine ID
                if (!usmParams.AuthoritativeEngineId.Data?.SequenceEqual(_engine.EngineId) ?? true)
                {
                    return new UsmProcessResult
                    {
                        RequiresReport = true,
                        ReportError = UsmError.UnknownEngineId
                    };
                }

                // Validate timeliness
                if (!_engine.IsTimeValid(usmParams.AuthoritativeEngineBoots.Value, usmParams.AuthoritativeEngineTime.Value))
                {
                    return new UsmProcessResult
                    {
                        RequiresReport = true,
                        ReportError = UsmError.NotInTimeWindow
                    };
                }

                // Get user
                var userName = usmParams.UserName.Value;
                var user = _userDatabase.GetUser(userName);
                if (user == null)
                {
                    return new UsmProcessResult
                    {
                        RequiresReport = true,
                        ReportError = UsmError.UnknownUserName
                    };
                }

                // Validate security level compatibility
                if ((v3Message.AuthFlag && !user.HasAuth) || (v3Message.PrivFlag && !user.HasPriv))
                {
                    return new UsmProcessResult
                    {
                        RequiresReport = true,
                        ReportError = UsmError.UnsupportedSecurityLevel
                    };
                }

                // Process authentication
                var scopedPduData = ExtractScopedPduData(v3Message);
                if (user.HasAuth)
                {
                    // Validate authentication
                    var messageForAuth = PrepareMessageForAuth(messageData, usmParams.AuthenticationParameters.Data?.Length ?? 0);
                    if (!user.ValidateAuth(messageForAuth, usmParams.AuthenticationParameters.Data ?? Array.Empty<byte>()))
                    {
                        return new UsmProcessResult
                        {
                            RequiresReport = true,
                            ReportError = UsmError.AuthenticationFailure
                        };
                    }
                }

                // Process privacy
                PDU? pdu = null;
                if (user.HasPriv)
                {
                    // Decrypt scoped PDU
                    var decryptedData = PrivacyProvider.Decrypt(
                        scopedPduData,
                        user.PrivKey,
                        usmParams.PrivacyParameters.Data ?? Array.Empty<byte>(),
                        user.PrivProtocol,
                        usmParams.AuthoritativeEngineBoots.Value,
                        usmParams.AuthoritativeEngineTime.Value);

                    pdu = ExtractPduFromScopedData(decryptedData);
                }
                else
                {
                    pdu = ExtractPduFromScopedData(scopedPduData);
                }

                return new UsmProcessResult
                {
                    IsSuccess = true,
                    User = user,
                    Pdu = pdu,
                    MessageId = v3Message.MessageId.Value,
                    MaxSize = v3Message.MaxSize.Value,
                    ContextEngineId = GetContextEngineId(scopedPduData),
                    ContextName = GetContextName(scopedPduData)
                };
            }
            catch (Exception ex)
            {
                return new UsmProcessResult
                {
                    RequiresReport = true,
                    ReportError = UsmError.GenericError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Create outgoing SNMPv3 response message
        /// </summary>
        public byte[] CreateResponseMessage(PDU responsePdu, UsmProcessResult originalRequest)
        {
            if (originalRequest.IsDiscovery)
            {
                return CreateDiscoveryResponse(responsePdu, originalRequest);
            }

            if (originalRequest.RequiresReport)
            {
                return CreateReportMessage(originalRequest);
            }

            return CreateAuthenticatedResponse(responsePdu, originalRequest);
        }

        /// <summary>
        /// Create discovery response with engine parameters
        /// </summary>
        private byte[] CreateDiscoveryResponse(PDU responsePdu, UsmProcessResult request)
        {
            var usmParams = UsmSecurityParameters.Create(
                Convert.ToHexString(_engine.EngineId),
                _engine.EngineBoots,
                _engine.EngineTime,
                "",
                Array.Empty<byte>(),
                Array.Empty<byte>());

            var scopedPdu = ScopedPdu.Create(responsePdu, "", "");
            var v3Message = SnmpMessageV3.Create(
                request.MessageId,
                request.MaxSize,
                authFlag: false,
                privFlag: false,
                reportableFlag: false,
                usmParams.ToBytes(),
                scopedPdu);

            return v3Message.ToBytes();
        }

        /// <summary>
        /// Create error report message
        /// </summary>
        private byte[] CreateReportMessage(UsmProcessResult request)
        {
            var reportPdu = CreateReportPdu(request.ReportError, request.MessageId);
            var usmParams = UsmSecurityParameters.Create(
                Convert.ToHexString(_engine.EngineId),
                _engine.EngineBoots,
                _engine.EngineTime,
                "",
                Array.Empty<byte>(),
                Array.Empty<byte>());

            var scopedPdu = ScopedPdu.Create(reportPdu, "", "");
            var v3Message = SnmpMessageV3.Create(
                request.MessageId,
                65507,
                authFlag: false,
                privFlag: false,
                reportableFlag: false,
                usmParams.ToBytes(),
                scopedPdu);

            return v3Message.ToBytes();
        }

        /// <summary>
        /// Create authenticated response message
        /// </summary>
        private byte[] CreateAuthenticatedResponse(PDU responsePdu, UsmProcessResult request)
        {
            if (request.User == null)
                throw new InvalidOperationException("User required for authenticated response");

            var user = request.User;
            var scopedPdu = ScopedPdu.Create(responsePdu, request.ContextEngineId ?? "", request.ContextName ?? "");
            var scopedPduData = ExtractScopedPduData(scopedPdu);

            byte[] encryptedData = scopedPduData;
            byte[] privacyParams = Array.Empty<byte>();

            // Apply privacy if needed
            if (user.HasPriv)
            {
                (encryptedData, privacyParams) = PrivacyProvider.Encrypt(
                    scopedPduData,
                    user.PrivKey,
                    user.PrivProtocol,
                    _engine.EngineBoots,
                    _engine.EngineTime);
            }

            // Prepare USM parameters
            var usmParams = UsmSecurityParameters.Create(
                Convert.ToHexString(_engine.EngineId),
                _engine.EngineBoots,
                _engine.EngineTime,
                user.UserName,
                user.HasAuth ? new byte[12] : Array.Empty<byte>(), // Placeholder for auth params
                privacyParams);

            // Create message
            var v3Message = SnmpMessageV3.Create(
                request.MessageId,
                request.MaxSize,
                authFlag: user.HasAuth,
                privFlag: user.HasPriv,
                reportableFlag: false,
                usmParams.ToBytes(),
                user.HasPriv ? ScopedPdu.CreateEncrypted(encryptedData) : scopedPdu);

            var messageData = v3Message.ToBytes();

            // Calculate authentication if needed
            if (user.HasAuth)
            {
                var authDigest = user.CalculateAuthDigest(messageData);
                // Update message with actual auth parameters
                // This requires rebuilding the message with correct auth params
                usmParams = UsmSecurityParameters.Create(
                    Convert.ToHexString(_engine.EngineId),
                    _engine.EngineBoots,
                    _engine.EngineTime,
                    user.UserName,
                    authDigest,
                    privacyParams);

                v3Message = SnmpMessageV3.Create(
                    request.MessageId,
                    request.MaxSize,
                    authFlag: user.HasAuth,
                    privFlag: user.HasPriv,
                    reportableFlag: false,
                    usmParams.ToBytes(),
                    user.HasPriv ? ScopedPdu.CreateEncrypted(encryptedData) : scopedPdu);

                messageData = v3Message.ToBytes();
            }

            return messageData;
        }

        /// <summary>
        /// Create Report PDU for USM errors
        /// </summary>
        private Report CreateReportPdu(UsmError error, int messageId)
        {
            var errorOid = error switch
            {
                UsmError.UnknownEngineId => "1.3.6.1.6.3.15.1.1.4.0",
                UsmError.UnknownUserName => "1.3.6.1.6.3.15.1.1.3.0",
                UsmError.UnsupportedSecurityLevel => "1.3.6.1.6.3.15.1.1.1.0",
                UsmError.AuthenticationFailure => "1.3.6.1.6.3.15.1.1.5.0",
                UsmError.NotInTimeWindow => "1.3.6.1.6.3.15.1.1.2.0",
                _ => "1.3.6.1.6.3.15.1.1.1.0"
            };

            // Create varbind with error counter
            var varbind = new nSNMP.SMI.DataTypes.V1.Constructed.Sequence(new nSNMP.SMI.DataTypes.IDataType[]
            {
                ObjectIdentifier.Create(errorOid),
                nSNMP.SMI.DataTypes.V1.Primitive.Counter32.Create(1) // Error counter
            });

            var varbindList = new nSNMP.SMI.DataTypes.V1.Constructed.Sequence(new nSNMP.SMI.DataTypes.IDataType[] { varbind });

            return new Report(
                null,
                Integer.Create(messageId),
                Integer.Create(0),
                Integer.Create(0),
                varbindList);
        }

        // Helper methods for data extraction
        private byte[] ExtractScopedPduData(SnmpMessageV3 message)
        {
            // Extract the scoped PDU bytes from the V3 message
            return message.ScopedPdu.ToBytes();
        }

        private byte[] ExtractScopedPduData(ScopedPdu scopedPdu)
        {
            // Convert scoped PDU to bytes for encryption/decryption
            return scopedPdu.ToBytes();
        }

        private PDU? ExtractPduFromScopedData(byte[] scopedData)
        {
            try
            {
                // Parse scoped PDU from decrypted data
                var scopedPdu = ScopedPdu.Parse(scopedData);
                return scopedPdu.Pdu;
            }
            catch
            {
                return null;
            }
        }

        private byte[] PrepareMessageForAuth(byte[] messageData, int authParamsLength)
        {
            // For authentication calculation, we need to zero out the auth parameters
            var result = new byte[messageData.Length];
            messageData.CopyTo(result, 0);

            // Locate and zero out authentication parameters in the USM security parameters
            // The auth parameters are typically located within the USM parameters OCTET STRING
            // This is a simplified approach that looks for the auth params pattern

            if (authParamsLength > 0 && authParamsLength <= 48) // Reasonable auth param length
            {
                // Search for potential auth parameter locations in the message
                // This is a heuristic approach - in production, proper ASN.1 parsing should be used
                for (int i = 0; i <= messageData.Length - authParamsLength; i++)
                {
                    // Look for OCTET STRING tag followed by length indicating auth params
                    if (i + 1 < messageData.Length &&
                        messageData[i] == 0x04 && // OCTET STRING tag
                        messageData[i + 1] == authParamsLength)
                    {
                        // Zero out the authentication parameters
                        Array.Fill(result, (byte)0, i + 2, authParamsLength);
                        break;
                    }
                }
            }

            return result;
        }

        private string? GetContextEngineId(byte[] scopedData)
        {
            try
            {
                var scopedPdu = ScopedPdu.Parse(scopedData);
                return scopedPdu.ContextEngineId?.Value ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string? GetContextName(byte[] scopedData)
        {
            try
            {
                var scopedPdu = ScopedPdu.Parse(scopedData);
                return scopedPdu.ContextName?.Value ?? "";
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>
    /// Result of USM message processing
    /// </summary>
    public class UsmProcessResult
    {
        public bool IsSuccess { get; set; }
        public bool IsDiscovery { get; set; }
        public bool RequiresReport { get; set; }
        public UsmError ReportError { get; set; }
        public string? ErrorMessage { get; set; }
        public V3User? User { get; set; }
        public PDU? Pdu { get; set; }
        public int MessageId { get; set; }
        public int MaxSize { get; set; } = 65507;
        public string? ContextEngineId { get; set; }
        public string? ContextName { get; set; }
        public EngineParameters? EngineParameters { get; set; }
    }

    /// <summary>
    /// USM error types for report generation
    /// </summary>
    public enum UsmError
    {
        UnknownEngineId,
        UnknownUserName,
        UnsupportedSecurityLevel,
        AuthenticationFailure,
        NotInTimeWindow,
        GenericError
    }
}