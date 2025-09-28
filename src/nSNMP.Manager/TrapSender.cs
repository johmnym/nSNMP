using System.Net;
using nSNMP.Core;
using nSNMP.Message;
using nSNMP.Security;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;

namespace nSNMP.Manager
{
    /// <summary>
    /// SNMP Trap sender for sending trap and notification messages
    /// </summary>
    public class TrapSender : IDisposable
    {
        private readonly IUdpChannel _channel;
        private readonly SnmpVersion _version;
        private readonly string _community;
        private V3Credentials? _credentials;
        private EngineParameters? _engineParameters;
        private bool _disposed;

        /// <summary>
        /// Create a trap sender for v1/v2c
        /// </summary>
        public TrapSender(SnmpVersion version = SnmpVersion.V2c, string community = "public", IUdpChannel? channel = null)
        {
            if (version == SnmpVersion.V3)
                throw new ArgumentException("Use the V3 constructor for SNMPv3", nameof(version));

            _version = version;
            _community = community ?? throw new ArgumentNullException(nameof(community));
            _channel = channel ?? new UdpChannel();
        }

        /// <summary>
        /// Create a trap sender for v3
        /// </summary>
        public TrapSender(V3Credentials credentials, EngineParameters? engineParameters = null, IUdpChannel? channel = null)
        {
            _version = SnmpVersion.V3;
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _engineParameters = engineParameters;
            _community = "";
            _channel = channel ?? new UdpChannel();
        }

        /// <summary>
        /// Send SNMPv1 trap
        /// </summary>
        public async Task SendTrapV1Async(
            IPEndPoint destination,
            ObjectIdentifier enterprise,
            IPAddress agentAddress,
            GenericTrap genericTrap,
            int specificTrap,
            TimeSpan uptime,
            params VarBind[] varbinds)
        {
            if (_version != SnmpVersion.V1)
                throw new InvalidOperationException("This method requires SNMPv1");

            var varbindList = CreateVarbindList(varbinds);
            var trap = new TrapV1(
                null,
                enterprise,
                new IpAddress(agentAddress.GetAddressBytes()),
                Integer.Create((int)genericTrap),
                Integer.Create(specificTrap),
                TimeTicks.Create((uint)(uptime.TotalMilliseconds / 10)), // Convert to 1/100th seconds
                varbindList
            );

            // For V1 traps, we need to encode them as a complete SNMP message
            // Create a sequence with version, community, and trap as the PDU
            var elements = new List<IDataType>
            {
                Integer.Create(0), // Version 0 for SNMPv1
                OctetString.Create(_community),
                trap
            };
            var messageSequence = new Sequence(elements.ToArray());
            var trapBytes = messageSequence.ToBytes();

            await _channel.SendAsync(trapBytes, destination, CancellationToken.None);
        }

        /// <summary>
        /// Send SNMPv2c trap (notification)
        /// </summary>
        public async Task SendTrapV2cAsync(
            IPEndPoint destination,
            ObjectIdentifier trapOid,
            TimeSpan uptime,
            params VarBind[] varbinds)
        {
            if (_version != SnmpVersion.V2c)
                throw new InvalidOperationException("This method requires SNMPv2c");

            // Create standard varbinds for v2c trap
            var allVarbinds = new List<VarBind>
            {
                new VarBind("1.3.6.1.2.1.1.3.0", TimeTicks.Create((uint)(uptime.TotalMilliseconds / 10))), // sysUpTime
                new VarBind("1.3.6.1.6.3.1.1.4.1.0", trapOid) // snmpTrapOID
            };
            allVarbinds.AddRange(varbinds);

            var varbindList = CreateVarbindList(allVarbinds.ToArray());
            var trap = new TrapV2(
                null,
                Integer.Create(0), // Request ID (0 for traps)
                Integer.Create(0), // Error status
                Integer.Create(0), // Error index
                varbindList
            );

            var message = new SnmpMessage(SnmpVersion.V2c, OctetString.Create(_community), trap);
            await SendMessageAsync(destination, message);
        }

        /// <summary>
        /// Send SNMPv3 trap (notification)
        /// </summary>
        public Task SendTrapV3Async(
            IPEndPoint destination,
            ObjectIdentifier trapOid,
            TimeSpan uptime,
            params VarBind[] varbinds)
        {
            if (_version != SnmpVersion.V3 || _credentials == null)
                throw new InvalidOperationException("This method requires SNMPv3 with credentials");

            // For V3 traps, we need engine discovery if not provided
            if (_engineParameters == null)
            {
                // In a real implementation, we might want to discover the engine
                // For now, use default values
                _engineParameters = new EngineParameters(
                    Array.Empty<byte>(),
                    0,
                    0
                );
            }

            // Create standard varbinds for v3 trap
            var allVarbinds = new List<VarBind>
            {
                new VarBind("1.3.6.1.2.1.1.3.0", TimeTicks.Create((uint)(uptime.TotalMilliseconds / 10))), // sysUpTime
                new VarBind("1.3.6.1.6.3.1.1.4.1.0", trapOid) // snmpTrapOID
            };
            allVarbinds.AddRange(varbinds);

            var varbindList = CreateVarbindList(allVarbinds.ToArray());
            var trap = new TrapV2(
                null,
                Integer.Create(0), // Request ID (0 for traps)
                Integer.Create(0), // Error status
                Integer.Create(0), // Error index
                varbindList
            );

            return SendV3TrapAsync(destination, trap);
        }

        /// <summary>
        /// Send INFORM request (acknowledged notification)
        /// </summary>
        public async Task<bool> SendInformAsync(
            IPEndPoint destination,
            ObjectIdentifier trapOid,
            TimeSpan uptime,
            params VarBind[] varbinds)
        {
            if (_version == SnmpVersion.V1)
                throw new InvalidOperationException("INFORM is not supported in SNMPv1");

            // Create standard varbinds for inform
            var allVarbinds = new List<VarBind>
            {
                new VarBind("1.3.6.1.2.1.1.3.0", TimeTicks.Create((uint)(uptime.TotalMilliseconds / 10))), // sysUpTime
                new VarBind("1.3.6.1.6.3.1.1.4.1.0", trapOid) // snmpTrapOID
            };
            allVarbinds.AddRange(varbinds);

            var varbindList = CreateVarbindList(allVarbinds.ToArray());
            var requestId = new Random().Next(1, int.MaxValue);
            var inform = new InformRequest(
                null,
                Integer.Create(requestId),
                Integer.Create(0), // Error status
                Integer.Create(0), // Error index
                varbindList
            );

            var message = new SnmpMessage(_version, OctetString.Create(_community), inform);

            // INFORM expects a response, unlike traps
            try
            {
                var response = await SendAndReceiveAsync(destination, message, TimeSpan.FromSeconds(5));
                return response != null;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Helper method to create varbind list
        /// </summary>
        private Sequence CreateVarbindList(VarBind[] varbinds)
        {
            var varbindData = new List<IDataType>();
            foreach (var vb in varbinds)
            {
                varbindData.Add(new Sequence(new IDataType[] { vb.Oid, vb.Value }));
            }
            return new Sequence(varbindData.ToArray());
        }

        /// <summary>
        /// Send message without expecting response
        /// </summary>
        private async Task SendMessageAsync(IPEndPoint destination, SnmpMessage message)
        {
            var data = message.ToBytes();
            await _channel.SendAsync(data, destination, CancellationToken.None);
        }

        /// <summary>
        /// Send message and wait for response (for INFORM)
        /// </summary>
        private async Task<byte[]?> SendAndReceiveAsync(IPEndPoint destination, SnmpMessage message, TimeSpan timeout)
        {
            var data = message.ToBytes();
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                // Use RequestAsync method from IUdpChannel
                return await _channel.SendReceiveAsync(data, destination, timeout, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"No response received within {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Send SNMPv3 trap with USM security
        /// </summary>
        private async Task SendV3TrapAsync(IPEndPoint destination, TrapV2 trap)
        {
            if (_credentials == null || _engineParameters == null)
                throw new InvalidOperationException("V3 credentials and engine parameters are required");

            // Create scoped PDU - traps don't need context typically
            var scopedPdu = ScopedPdu.Create(trap, contextEngineId: "", contextName: "");
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

            // Create message flags
            byte flags = 0x00; // Traps are not reportable by default
            if (_credentials.AuthProtocol != AuthProtocol.None) flags |= 0x01; // Auth
            if (_credentials.PrivProtocol != PrivProtocol.None) flags |= 0x02; // Priv

            // Create SNMPv3 message with a simple message ID (traps don't need correlation)
            var v3Message = new SnmpMessageV3(
                Integer.Create(new Random().Next(1, int.MaxValue)),
                Integer.Create(65507),
                new OctetString(new byte[] { flags }),
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

            // Send trap (fire-and-forget for traps)
            await _channel.SendAsync(messageData, destination, CancellationToken.None);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Generic trap types for SNMPv1
    /// </summary>
    public enum GenericTrap
    {
        ColdStart = 0,
        WarmStart = 1,
        LinkDown = 2,
        LinkUp = 3,
        AuthenticationFailure = 4,
        EgpNeighborLoss = 5,
        EnterpriseSpecific = 6
    }
}