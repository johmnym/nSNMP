using nSNMP.Manager;
using nSNMP.Message;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;
using System.Net;

namespace nSNMP.Security
{
    /// <summary>
    /// SNMPv3 engine discovery implementation (RFC 3414)
    /// </summary>
    public class EngineDiscovery
    {
        private readonly IUdpChannel _transport;
        private readonly IPEndPoint _endpoint;
        private int _messageId = 1;

        public EngineDiscovery(IPEndPoint endpoint, IUdpChannel? transport = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _transport = transport ?? new UdpChannel();
        }

        /// <summary>
        /// Discover engine parameters from SNMPv3 agent
        /// </summary>
        public async Task<EngineParameters> DiscoverAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var discoveryTimeout = timeout ?? TimeSpan.FromSeconds(5);

            // Create discovery request with empty engine parameters
            var discoveryRequest = CreateDiscoveryRequest();
            var requestData = discoveryRequest.ToBytes();

            try
            {
                // Send discovery request and wait for report
                var responseData = await _transport.SendReceiveAsync(requestData, _endpoint, discoveryTimeout, cancellationToken);

                // Parse response
                var response = SnmpMessageV3.Parse(responseData);

                // Extract engine parameters from the response
                return ExtractEngineParameters(response);
            }
            catch (TimeoutException)
            {
                throw new SnmpTimeoutException(discoveryTimeout);
            }
            catch (Exception ex)
            {
                throw new SnmpException("Engine discovery failed", ex);
            }
        }

        /// <summary>
        /// Create SNMPv3 discovery request message
        /// </summary>
        private SnmpMessageV3 CreateDiscoveryRequest()
        {
            var messageId = Interlocked.Increment(ref _messageId);

            // Empty USM parameters for discovery
            var usmParams = UsmSecurityParameters.CreateDiscovery();

            // Empty scoped PDU with GetRequest for discovery
            var varbindList = new Sequence(Array.Empty<nSNMP.SMI.DataTypes.IDataType>());
            var getRequest = new GetRequest(null, Integer.Create(messageId), Integer.Create(0), Integer.Create(0), varbindList);
            var scopedPdu = ScopedPdu.Create(getRequest);

            // Create SNMPv3 message with reportable flag
            return new SnmpMessageV3(
                Integer.Create(messageId),
                Integer.Create(65507), // maxSize
                new OctetString(new byte[] { 0x04 }), // reportable flag only
                Integer.Create(3), // USM security model
                new OctetString(usmParams.ToBytes()),
                scopedPdu
            );
        }

        /// <summary>
        /// Extract engine parameters from discovery response
        /// </summary>
        private EngineParameters ExtractEngineParameters(SnmpMessageV3 response)
        {
            // Discovery response should contain a Report PDU
            if (response.ScopedPdu.Pdu is not Report report)
            {
                throw new SnmpException($"Expected Report PDU in discovery response, got {response.ScopedPdu.Pdu?.GetType().Name}");
            }

            // Parse USM security parameters from response
            var usmParams = UsmSecurityParameters.Parse(response.SecurityParameters.Data ?? Array.Empty<byte>());

            // Check for discovery error (unknown engine ID)
            if (report.Error?.Value == 1) // Unknown engine ID error is expected in discovery
            {
                return new EngineParameters(
                    usmParams.AuthoritativeEngineId.Data ?? Array.Empty<byte>(),
                    usmParams.AuthoritativeEngineBoots.Value,
                    usmParams.AuthoritativeEngineTime.Value
                );
            }

            // Other errors indicate discovery failure
            if (report.Error?.Value != 0)
            {
                throw SnmpErrorException.FromErrorStatus(report.Error?.Value ?? 0, report.ErrorIndex?.Value ?? 0);
            }

            return new EngineParameters(
                usmParams.AuthoritativeEngineId.Data ?? Array.Empty<byte>(),
                usmParams.AuthoritativeEngineBoots.Value,
                usmParams.AuthoritativeEngineTime.Value
            );
        }
    }

    /// <summary>
    /// Engine parameters discovered from SNMPv3 agent
    /// </summary>
    public record EngineParameters(byte[] EngineId, int EngineBoots, int EngineTime)
    {
        /// <summary>
        /// Engine ID as hex string for display
        /// </summary>
        public string EngineIdHex => Convert.ToHexString(EngineId);

        /// <summary>
        /// Check if engine time appears valid (not zero for most implementations)
        /// </summary>
        public bool IsTimeValid => EngineTime > 0;

        /// <summary>
        /// Calculate time window validity
        /// </summary>
        public bool IsTimeWithinWindow(int currentTime, int windowSize = 150)
        {
            return Math.Abs(currentTime - EngineTime) <= windowSize;
        }

        /// <summary>
        /// Create updated engine parameters with new time
        /// </summary>
        public EngineParameters WithTime(int newTime) => this with { EngineTime = newTime };

        /// <summary>
        /// Create updated engine parameters with incremented boots
        /// </summary>
        public EngineParameters WithIncrementedBoots() => this with { EngineBoots = EngineBoots + 1, EngineTime = 0 };

        public override string ToString()
        {
            return $"Engine ID: {EngineIdHex}, Boots: {EngineBoots}, Time: {EngineTime}";
        }
    }
}