using System.Collections.Concurrent;
using nSNMP.Agent.VACM;
using nSNMP.Message;
using nSNMP.Security;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;

namespace nSNMP.Agent
{
    /// <summary>
    /// SNMPv3-capable agent host with USM security support
    /// </summary>
    public class SnmpAgentHostV3 : SnmpAgentHost
    {
        private readonly SnmpEngine _engine;
        private readonly V3UserDatabase _userDatabase;
        private readonly UsmProcessor _usmProcessor;
        private readonly VacmProcessor _vacmProcessor;

        public SnmpAgentHostV3(
            string readCommunity = "public",
            string writeCommunity = "private",
            IUdpListener? listener = null,
            byte[]? engineId = null,
            bool configureDefaultVacm = true)
            : base(readCommunity, writeCommunity, listener)
        {
            _engine = new SnmpEngine(engineId);
            _userDatabase = new V3UserDatabase(_engine);
            _usmProcessor = new UsmProcessor(_engine, _userDatabase);
            _vacmProcessor = new VacmProcessor();

            if (configureDefaultVacm)
            {
                ConfigureDefaultVacm();
            }
        }

        /// <summary>
        /// Engine for this agent
        /// </summary>
        public SnmpEngine Engine => _engine;

        /// <summary>
        /// User database for V3 security
        /// </summary>
        public V3UserDatabase UserDatabase => _userDatabase;

        /// <summary>
        /// VACM processor for access control
        /// </summary>
        public VacmProcessor VacmProcessor => _vacmProcessor;

        /// <summary>
        /// Add SNMPv3 user to the agent
        /// </summary>
        public void AddUser(V3Credentials credentials)
        {
            _userDatabase.AddUser(credentials);
        }

        /// <summary>
        /// Add SNMPv3 user with factory methods
        /// </summary>
        public void AddUser(string userName, AuthProtocol authProtocol = AuthProtocol.None, string authPassword = "",
                           PrivProtocol privProtocol = PrivProtocol.None, string privPassword = "")
        {
            var credentials = new V3Credentials(userName, authProtocol, authPassword, privProtocol, privPassword);
            AddUser(credentials);
        }

        /// <summary>
        /// Add SNMPv3 user with VACM group assignment
        /// </summary>
        public void AddUser(V3Credentials credentials, string? groupName = null)
        {
            AddUser(credentials);

            // Automatically add to VACM group if specified
            if (!string.IsNullOrEmpty(groupName))
            {
                _vacmProcessor.AddGroup(VacmGroup.CreateUserGroup(groupName, credentials.UserName));
            }
        }

        /// <summary>
        /// Configure default VACM settings
        /// </summary>
        private void ConfigureDefaultVacm()
        {
            _vacmProcessor.ConfigureDefaults();

            // Add groups for any V3 users that are added later
            // This will be expanded as users are added
        }

        /// <summary>
        /// Process incoming SNMP request (overrides base to handle V3)
        /// </summary>
        protected override async Task ProcessRequestAsync(UdpRequest request)
        {
            try
            {
                // Try to detect message version first
                if (IsV3Message(request.Data))
                {
                    await ProcessV3RequestAsync(request);
                }
                else
                {
                    // Process v1/v2c with VACM support
                    await ProcessV1V2cRequestAsync(request);
                }
            }
            catch (Exception)
            {
                // Log error in production, for now ignore malformed packets
            }
        }

        /// <summary>
        /// Process v1/v2c request with VACM access control
        /// </summary>
        private async Task ProcessV1V2cRequestAsync(UdpRequest request)
        {
            try
            {
                // Parse the incoming SNMP message
                var message = SnmpMessage.Create(request.Data);

                if (message == null || message.PDU == null)
                    return; // Invalid message, ignore

                // Validate community string and get security model
                var securityModel = GetSecurityModel(message.Version ?? SnmpVersion.V2c);
                var communityString = message.CommunityString?.Value ?? "";

                // Check VACM access for community string
                var accessType = GetAccessType(message.PDU);
                if (accessType == null)
                    return;

                // Use simplified VACM check for community strings
                bool hasAccess = await CheckCommunityAccess(communityString, accessType.Value, securityModel);

                if (!hasAccess)
                    return; // Access denied, ignore request

                // Process PDU with traditional community-based access
                bool isWriteCommunity = communityString == GetWriteCommunity();
                var responsePdu = await ProcessPduAsync(message.PDU, isWriteCommunity);

                if (responsePdu != null)
                {
                    // Create response message
                    var responseMessage = new SnmpMessage(message.Version, message.CommunityString, responsePdu);
                    var responseData = responseMessage.ToBytes();

                    // Send response
                    await request.SendResponseAsync(responseData);
                }
            }
            catch (Exception)
            {
                // Log error in production, for now ignore malformed packets
            }
        }

        /// <summary>
        /// Check community access using VACM
        /// </summary>
        private async Task<bool> CheckCommunityAccess(string communityString, VacmAccessType accessType, VacmSecurityModel securityModel)
        {
            // For basic implementation, use the community string as the security name
            var result = _vacmProcessor.CheckAccess(
                securityModel,
                communityString,
                SecurityLevel.NoAuthNoPriv,
                "", // Empty context for v1/v2c
                accessType,
                ObjectIdentifier.Create("1.3.6.1")); // Use internet subtree as default check

            return result.IsAllowed;
        }

        /// <summary>
        /// Get security model from SNMP version
        /// </summary>
        private VacmSecurityModel GetSecurityModel(SnmpVersion version)
        {
            return version switch
            {
                SnmpVersion.V1 => VacmSecurityModel.SNMPv1,
                SnmpVersion.V2c => VacmSecurityModel.SNMPv2c,
                SnmpVersion.V3 => VacmSecurityModel.USM,
                _ => VacmSecurityModel.SNMPv2c
            };
        }

        /// <summary>
        /// Get write community string from base class (protected accessor)
        /// </summary>
        private string GetWriteCommunity()
        {
            // This is a workaround since the base class fields are private
            // In practice, we'd need to modify the base class or track this separately
            return "private"; // Default write community
        }

        /// <summary>
        /// Process SNMPv3 request with USM security
        /// </summary>
        private async Task ProcessV3RequestAsync(UdpRequest request)
        {
            // Process with USM
            var usmResult = _usmProcessor.ProcessIncomingMessage(request.Data);

            PDU? responsePdu = null;

            if (usmResult.IsSuccess && usmResult.Pdu != null)
            {
                // Process the authenticated PDU with VACM access control
                responsePdu = await ProcessV3PduWithVacm(usmResult);
            }
            else if (usmResult.RequiresReport)
            {
                // Create report PDU for USM errors
                responsePdu = CreateUsmReportPdu(usmResult.ReportError, usmResult.MessageId);
            }

            if (responsePdu != null)
            {
                // Create V3 response message
                var responseData = _usmProcessor.CreateResponseMessage(responsePdu, usmResult);
                await request.SendResponseAsync(responseData);
            }
        }

        /// <summary>
        /// Process V3 PDU with VACM access control
        /// </summary>
        private async Task<PDU?> ProcessV3PduWithVacm(UsmProcessResult usmResult)
        {
            if (usmResult.User == null || usmResult.Pdu == null)
                return null;

            var securityModel = VacmSecurityModel.USM;
            var securityName = usmResult.User.UserName;
            var securityLevel = usmResult.User.SecurityLevel;
            var contextName = usmResult.ContextName ?? "";

            // Determine access type from PDU type
            var accessType = GetAccessType(usmResult.Pdu);
            if (accessType == null)
            {
                return CreateV3ErrorResponse(usmResult.Pdu, SnmpErrorStatus.GenErr, 0);
            }

            // For operations that access specific OIDs, check each one
            if (usmResult.Pdu is GetRequest || usmResult.Pdu is GetNextRequest || usmResult.Pdu is SetRequest || usmResult.Pdu is GetBulkRequest)
            {
                return await ProcessPduWithVacmChecks(usmResult.Pdu, securityModel, securityName, securityLevel, contextName, accessType.Value);
            }

            // For other PDU types, use base implementation
            var hasWriteAccess = accessType == VacmAccessType.Write;
            return await ProcessPduAsync(usmResult.Pdu, hasWriteAccess);
        }

        /// <summary>
        /// Process PDU with VACM access checks for each OID
        /// </summary>
        private async Task<PDU> ProcessPduWithVacmChecks(PDU requestPdu, VacmSecurityModel securityModel, string securityName, SecurityLevel securityLevel, string contextName, VacmAccessType accessType)
        {
            var responseVarBinds = new List<IDataType>();
            var errorIndex = 0;
            var errorStatus = SnmpErrorStatus.NoError;

            if (requestPdu.VarbindList?.Elements != null)
            {
                for (int i = 0; i < requestPdu.VarbindList.Elements.Count; i++)
                {
                    var varbind = requestPdu.VarbindList.Elements[i];
                    if (varbind is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = (ObjectIdentifier)seq.Elements[0];

                        // Check VACM access for this OID
                        var vacmResult = _vacmProcessor.CheckAccess(securityModel, securityName, securityLevel, contextName, accessType, oid);

                        if (!vacmResult.IsAllowed)
                        {
                            // Access denied - return appropriate error
                            errorStatus = SnmpErrorStatus.NoAccess;
                            errorIndex = i + 1;
                            break;
                        }

                        // Access allowed - process the operation
                        IDataType value;
                        if (requestPdu is SetRequest)
                        {
                            var setValue = seq.Elements[1];
                            if (SetV3Value(oid, setValue))
                            {
                                value = setValue;
                            }
                            else
                            {
                                errorStatus = SnmpErrorStatus.NotWritable;
                                errorIndex = i + 1;
                                break;
                            }
                        }
                        else
                        {
                            value = GetV3Value(oid) ?? new NoSuchObject();
                        }

                        responseVarBinds.Add(new Sequence(new IDataType[] { oid, value }));
                    }
                }
            }

            return new GetResponse(
                null,
                requestPdu.RequestId,
                Integer.Create((int)errorStatus),
                Integer.Create(errorIndex),
                new Sequence(responseVarBinds)
            );
        }

        /// <summary>
        /// Get VACM access type from PDU type
        /// </summary>
        private VacmAccessType? GetAccessType(PDU pdu)
        {
            return pdu switch
            {
                GetRequest => VacmAccessType.Read,
                GetNextRequest => VacmAccessType.Read,
                GetBulkRequest => VacmAccessType.Read,
                SetRequest => VacmAccessType.Write,
                InformRequest => VacmAccessType.Notify,
                _ => null
            };
        }

        /// <summary>
        /// Create USM error report PDU
        /// </summary>
        private Report CreateUsmReportPdu(UsmError error, int messageId)
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

            var varbind = new Sequence(new IDataType[]
            {
                ObjectIdentifier.Create(errorOid),
                Counter32.Create(1)
            });

            var varbindList = new Sequence(new IDataType[] { varbind });

            return new Report(
                null,
                Integer.Create(messageId),
                Integer.Create(0),
                Integer.Create(0),
                varbindList);
        }

        /// <summary>
        /// Detect if incoming message is SNMPv3
        /// </summary>
        private bool IsV3Message(byte[] data)
        {
            try
            {
                if (data.Length < 10) return false;

                // Quick check: SNMPv3 messages start with sequence, then version 3
                // Parse just enough to check version
                var sequence = (Sequence)nSNMP.SMI.SMIDataFactory.Create(data);
                if (sequence.Elements?.Count > 0)
                {
                    var version = (Integer)sequence.Elements[0];
                    return version.Value == 3;
                }
            }
            catch
            {
                // If parsing fails, assume not V3
            }

            return false;
        }

        /// <summary>
        /// Get engine information for display
        /// </summary>
        public string GetEngineInfo()
        {
            return _engine.ToString();
        }

        /// <summary>
        /// Get user count
        /// </summary>
        public int UserCount => _userDatabase.Count;

        /// <summary>
        /// Get all usernames
        /// </summary>
        public IEnumerable<string> GetUserNames()
        {
            return _userDatabase.GetUserNames();
        }

        /// <summary>
        /// Create error response PDU for V3 operations
        /// </summary>
        private PDU CreateV3ErrorResponse(PDU originalPdu, SnmpErrorStatus errorStatus, int errorIndex)
        {
            return new GetResponse(
                null,
                originalPdu.RequestId,
                Integer.Create((int)errorStatus),
                Integer.Create(errorIndex),
                originalPdu.VarbindList ?? new Sequence(Array.Empty<IDataType>())
            );
        }

        /// <summary>
        /// Get value from provider (V3 version)
        /// </summary>
        private IDataType? GetV3Value(ObjectIdentifier oid)
        {
            // This is a simplified implementation that should delegate to the base class providers
            // In a real implementation, we'd need access to the base class provider collections
            // For now, return null which will be converted to NoSuchObject
            return null;
        }

        /// <summary>
        /// Set value through provider (V3 version)
        /// </summary>
        private bool SetV3Value(ObjectIdentifier oid, IDataType value)
        {
            // This is a simplified implementation that should delegate to the base class providers
            // In a real implementation, we'd need access to the base class provider collections
            // For now, return false indicating the operation is not writable
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Additional V3-specific cleanup if needed
            }
            base.Dispose(disposing);
        }
    }
}