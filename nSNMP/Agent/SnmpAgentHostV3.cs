using System.Collections.Concurrent;
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

        public SnmpAgentHostV3(
            string readCommunity = "public",
            string writeCommunity = "private",
            IUdpListener? listener = null,
            byte[]? engineId = null)
            : base(readCommunity, writeCommunity, listener)
        {
            _engine = new SnmpEngine(engineId);
            _userDatabase = new V3UserDatabase(_engine);
            _usmProcessor = new UsmProcessor(_engine, _userDatabase);
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
                    // Fall back to base v1/v2c processing
                    await base.ProcessRequestAsync(request);
                }
            }
            catch (Exception)
            {
                // Log error in production, for now ignore malformed packets
            }
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
                // Process the authenticated PDU
                var hasWriteAccess = usmResult.User?.SecurityLevel == SecurityLevel.AuthPriv ||
                                   usmResult.User?.SecurityLevel == SecurityLevel.AuthNoPriv;

                responsePdu = await ProcessPduAsync(usmResult.Pdu, hasWriteAccess);
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