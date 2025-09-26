using System.Security.Cryptography;
using nSNMP.Security;

namespace nSNMP.Agent
{
    /// <summary>
    /// SNMP Engine management for SNMPv3 agent operations
    /// </summary>
    public class SnmpEngine
    {
        private readonly byte[] _engineId;
        private int _engineBoots;
        private int _engineTime;
        private DateTime _startTime;

        /// <summary>
        /// Engine ID for this SNMP agent
        /// </summary>
        public byte[] EngineId => _engineId;

        /// <summary>
        /// Engine boots counter
        /// </summary>
        public int EngineBoots => _engineBoots;

        /// <summary>
        /// Current engine time in seconds since last boot
        /// </summary>
        public int EngineTime
        {
            get
            {
                var elapsed = DateTime.UtcNow - _startTime;
                return _engineTime + (int)elapsed.TotalSeconds;
            }
        }

        /// <summary>
        /// Create SNMP engine with generated engine ID
        /// </summary>
        public SnmpEngine(byte[]? engineId = null, int engineBoots = 0)
        {
            _engineId = engineId ?? GenerateEngineId();
            _engineBoots = engineBoots;
            _engineTime = 0;
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Generate RFC 3411 compliant engine ID
        /// </summary>
        private static byte[] GenerateEngineId()
        {
            // RFC 3411 format: [enterprise_id(4)] [format(1)] [random_data(N)]
            // Using format 1 (random octets) with 8 random bytes
            var engineId = new byte[13];

            // Enterprise ID for private use (example: 0x00000001)
            engineId[0] = 0x80;
            engineId[1] = 0x00;
            engineId[2] = 0x00;
            engineId[3] = 0x01;

            // Format 1: Random octets
            engineId[4] = 0x01;

            // 8 random bytes
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[8];
            rng.GetBytes(randomBytes);
            Array.Copy(randomBytes, 0, engineId, 5, 8);

            return engineId;
        }

        /// <summary>
        /// Get current engine parameters
        /// </summary>
        public EngineParameters GetParameters()
        {
            return new EngineParameters(EngineId, EngineBoots, EngineTime);
        }

        /// <summary>
        /// Increment engine boots (typically on restart)
        /// </summary>
        public void IncrementBoots()
        {
            _engineBoots++;
            _engineTime = 0;
            _startTime = DateTime.UtcNow; // Reset start time to effectively reset engine time
        }

        /// <summary>
        /// Validate timeliness window for incoming requests
        /// </summary>
        public bool IsTimeValid(int requestBoots, int requestTime, int windowSize = 150)
        {
            // Check boots match
            if (requestBoots != EngineBoots)
                return false;

            // Check time window
            var currentTime = EngineTime;
            return Math.Abs(currentTime - requestTime) <= windowSize;
        }

        /// <summary>
        /// Engine ID as hex string for display
        /// </summary>
        public string EngineIdHex => Convert.ToHexString(EngineId);

        public override string ToString()
        {
            return $"Engine ID: {EngineIdHex}, Boots: {EngineBoots}, Time: {EngineTime}";
        }
    }
}