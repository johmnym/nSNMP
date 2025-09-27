using System.Net;
using nSNMP.Logging;
using nSNMP.Message;
using nSNMP.Security;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Telemetry;
using nSNMP.Transport;

namespace nSNMP.Manager
{
    /// <summary>
    /// SNMPv3 client with USM security support
    /// </summary>
    public class SnmpClientV3 : IDisposable
    {
        private readonly IUdpChannel _transport;
        private readonly IPEndPoint _endpoint;
        private readonly V3Credentials _credentials;
        private readonly TimeSpan _timeout;
        private readonly ISnmpLogger _logger;
        private EngineParameters? _engineParameters;
        private int _messageId;
        private bool _disposed;

        public SnmpClientV3(IPEndPoint endpoint, V3Credentials credentials, TimeSpan? timeout = null, IUdpChannel? transport = null, ISnmpLogger? logger = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _credentials.Validate();
            _timeout = timeout ?? TimeSpan.FromSeconds(5);
            _transport = transport ?? new UdpChannel();
            _logger = logger ?? NullSnmpLogger.Instance;
        }

        /// <summary>
        /// Factory method for noAuthNoPriv security level
        /// </summary>
        public static SnmpClientV3 CreateNoAuthNoPriv(string host, string userName, int port = 161, TimeSpan? timeout = null)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            var credentials = V3Credentials.NoAuthNoPriv(userName);
            return new SnmpClientV3(endpoint, credentials, timeout);
        }

        /// <summary>
        /// Factory method for authNoPriv security level
        /// </summary>
        public static SnmpClientV3 CreateAuthNoPriv(string host, string userName, AuthProtocol authProtocol, string authPassword, int port = 161, TimeSpan? timeout = null)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            var credentials = V3Credentials.AuthNoPriv(userName, authProtocol, authPassword);
            return new SnmpClientV3(endpoint, credentials, timeout);
        }

        /// <summary>
        /// Factory method for authPriv security level
        /// </summary>
        public static SnmpClientV3 CreateAuthPriv(string host, string userName, AuthProtocol authProtocol, string authPassword, PrivProtocol privProtocol, string privPassword, int port = 161, TimeSpan? timeout = null)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            var credentials = V3Credentials.AuthPriv(userName, authProtocol, authPassword, privProtocol, privPassword);
            return new SnmpClientV3(endpoint, credentials, timeout);
        }

        /// <summary>
        /// Discover engine parameters (must be called before other operations)
        /// </summary>
        public async Task DiscoverEngineAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var operation = "ENGINE_DISCOVERY";

            // Start telemetry activity
            using var activity = SnmpTelemetry.StartActivity(operation, _endpoint.ToString(), userName: _credentials.UserName);

            try
            {
                _logger.LogSecurityOperation(operation, _credentials.UserName, _credentials.SecurityLevel.ToString(), false);

                var discovery = new EngineDiscovery(_endpoint, _transport);
                _engineParameters = await discovery.DiscoverAsync(_timeout, cancellationToken);

                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogSecurityOperation(operation, _credentials.UserName, _credentials.SecurityLevel.ToString(), true);
                _logger.LogPerformance(operation, elapsed);

                // Record telemetry
                SnmpTelemetry.RecordSecurityOperation(operation, _credentials.UserName, _credentials.SecurityLevel.ToString(), elapsed, true);
                SnmpTelemetry.SetActivitySuccess(activity);
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogError(operation, ex, $"Engine discovery failed for {_endpoint}");

                // Record telemetry for error
                SnmpTelemetry.RecordSecurityOperation(operation, _credentials.UserName, _credentials.SecurityLevel.ToString(), elapsed, false);
                SnmpTelemetry.SetActivityError(activity, ex);

                throw;
            }
        }

        /// <summary>
        /// Performs SNMP GET operation
        /// </summary>
        public async Task<VarBind[]> GetAsync(params string[] oids)
        {
            return await GetAsync(oids.Select(ObjectIdentifier.Create).ToArray());
        }

        /// <summary>
        /// Performs SNMP GET operation
        /// </summary>
        public async Task<VarBind[]> GetAsync(params ObjectIdentifier[] oids)
        {
            await EnsureEngineDiscovered();

            if (oids == null || oids.Length == 0)
                throw new ArgumentException("At least one OID must be specified", nameof(oids));

            var varbinds = oids.Select(oid => new Sequence(new IDataType[] { oid, new Null() })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new GetRequest(null, Integer.Create(GetNextMessageId()), Integer.Create(0), Integer.Create(0), varbindList);
            var response = await SendV3RequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Performs SNMP SET operation
        /// </summary>
        public async Task<VarBind[]> SetAsync(params VarBind[] varBinds)
        {
            await EnsureEngineDiscovered();

            if (varBinds == null || varBinds.Length == 0)
                throw new ArgumentException("At least one variable binding must be specified", nameof(varBinds));

            var varbinds = varBinds.Select(vb => new Sequence(new IDataType[] { vb.Oid, vb.Value })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new SetRequest(null, Integer.Create(GetNextMessageId()), Integer.Create(0), Integer.Create(0), varbindList);
            var response = await SendV3RequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Send SNMPv3 request with USM security
        /// </summary>
        private async Task<GetResponse> SendV3RequestAsync(PDU request)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SnmpClientV3));

            if (_engineParameters == null)
                throw new InvalidOperationException("Engine discovery must be performed before sending requests");

            try
            {
                // Create scoped PDU
                var scopedPdu = ScopedPdu.Create(request);
                var scopedPduData = scopedPdu.ToBytes();

                // Apply privacy if configured
                byte[] finalScopedPduData;
                byte[] privacyParameters;

                if (_credentials.PrivProtocol != PrivProtocol.None)
                {
                    var privKey = _credentials.GetPrivKey(_engineParameters.EngineId);
                    (finalScopedPduData, privacyParameters) = PrivacyProvider.Encrypt(
                        scopedPduData, privKey, _credentials.PrivProtocol,
                        _engineParameters.EngineBoots, _engineParameters.EngineTime);
                }
                else
                {
                    finalScopedPduData = scopedPduData;
                    privacyParameters = Array.Empty<byte>();
                }

                // Create USM security parameters
                var usmParams = UsmSecurityParameters.Create(
                    Convert.ToHexString(_engineParameters.EngineId),
                    _engineParameters.EngineBoots,
                    _engineParameters.EngineTime,
                    _credentials.UserName,
                    authenticationParameters: new byte[12], // Placeholder for auth params
                    privacyParameters: privacyParameters
                );

                // Create SNMPv3 message
                var v3Message = new SnmpMessageV3(
                    Integer.Create(GetNextMessageId()),
                    Integer.Create(65507),
                    new OctetString(new byte[] { CreateFlags() }),
                    Integer.Create(3), // USM
                    new OctetString(usmParams.ToBytes()),
                    new ScopedPdu(null, null, null) // Will be replaced with encrypted data
                );

                // Calculate authentication if required
                byte[] messageData;
                if (_credentials.AuthProtocol != AuthProtocol.None)
                {
                    var authKey = _credentials.GetAuthKey(_engineParameters.EngineId);
                    var tempMessage = v3Message.ToBytes();

                    var authParams = KeyLocalization.CalculateDigest(tempMessage, authKey, _credentials.AuthProtocol);

                    // Update USM params with real auth parameters
                    usmParams = UsmSecurityParameters.Create(
                        Convert.ToHexString(_engineParameters.EngineId),
                        _engineParameters.EngineBoots,
                        _engineParameters.EngineTime,
                        _credentials.UserName,
                        authParams,
                        privacyParameters
                    );

                    // Recreate message with auth params
                    v3Message = v3Message with { SecurityParameters = new OctetString(usmParams.ToBytes()) };
                }

                messageData = v3Message.ToBytes();

                // Send request and receive response
                var responseData = await _transport.SendReceiveAsync(messageData, _endpoint, _timeout);

                // Parse and decrypt response
                var responseMessage = SnmpMessageV3.Parse(responseData);
                var response = await ProcessV3Response(responseMessage);

                if (response is not GetResponse getResponse)
                    throw new SnmpException($"Expected GetResponse, received {response?.GetType().Name}");

                // Check for SNMP errors
                if (getResponse.Error?.Value != 0)
                {
                    throw SnmpErrorException.FromErrorStatus(getResponse.Error?.Value ?? 0, getResponse.ErrorIndex?.Value ?? 0);
                }

                return getResponse;
            }
            catch (TimeoutException)
            {
                throw new SnmpTimeoutException(_timeout);
            }
        }

        /// <summary>
        /// Process SNMPv3 response (decrypt if needed)
        /// </summary>
        private Task<PDU> ProcessV3Response(SnmpMessageV3 response)
        {
            // Decrypt scoped PDU if privacy is enabled
            if (response.PrivFlag && _credentials.PrivProtocol != PrivProtocol.None)
            {
                var usmParams = UsmSecurityParameters.Parse(response.SecurityParameters.Data ?? Array.Empty<byte>());
                var privKey = _credentials.GetPrivKey(_engineParameters!.EngineId);

                var decryptedData = PrivacyProvider.Decrypt(
                    response.ScopedPdu.ToBytes(),
                    privKey,
                    usmParams.PrivacyParameters.Data ?? Array.Empty<byte>(),
                    _credentials.PrivProtocol,
                    usmParams.AuthoritativeEngineBoots.Value,
                    usmParams.AuthoritativeEngineTime.Value
                );

                var decryptedScopedPdu = ScopedPdu.Parse(decryptedData);
                return Task.FromResult(decryptedScopedPdu.Pdu!);
            }

            return Task.FromResult(response.ScopedPdu.Pdu!);
        }

        /// <summary>
        /// Create message flags based on security configuration
        /// </summary>
        private byte CreateFlags()
        {
            byte flags = 0x04; // Reportable
            if (_credentials.AuthProtocol != AuthProtocol.None) flags |= 0x01; // Auth
            if (_credentials.PrivProtocol != PrivProtocol.None) flags |= 0x02; // Priv
            return flags;
        }

        /// <summary>
        /// Ensure engine discovery has been performed
        /// </summary>
        private async Task EnsureEngineDiscovered()
        {
            if (_engineParameters == null)
            {
                await DiscoverEngineAsync();
            }
        }

        /// <summary>
        /// Parse variable bindings from response
        /// </summary>
        private VarBind[] ParseVarBinds(GetResponse response)
        {
            if (response.VarbindList?.Elements == null)
                return Array.Empty<VarBind>();

            var varBinds = new List<VarBind>();

            foreach (var element in response.VarbindList.Elements)
            {
                if (element is Sequence seq && seq.Elements?.Count >= 2)
                {
                    var oid = (ObjectIdentifier)seq.Elements[0];
                    var value = seq.Elements[1];
                    varBinds.Add(new VarBind(oid, value));
                }
            }

            return varBinds.ToArray();
        }

        /// <summary>
        /// Get next message ID
        /// </summary>
        private int GetNextMessageId()
        {
            return Interlocked.Increment(ref _messageId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _transport?.Dispose();
        }
    }
}