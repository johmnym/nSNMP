using System.Collections.Concurrent;
using nSNMP.SMI.Configuration;

namespace nSNMP.SMI.Optimization
{
    /// <summary>
    /// Provides string interning for commonly used OID strings to reduce memory usage
    /// </summary>
    public static class OidInterning
    {
        private static readonly ConcurrentDictionary<string, string> _internedOids = new();

        // Pre-populate with standard MIB-2 OIDs that are used frequently
        private static readonly HashSet<string> _commonOids = new()
        {
            // System group (1.3.6.1.2.1.1)
            "1.3.6.1.2.1.1.1.0",    // sysDescr
            "1.3.6.1.2.1.1.2.0",    // sysObjectID
            "1.3.6.1.2.1.1.3.0",    // sysUpTime
            "1.3.6.1.2.1.1.4.0",    // sysContact
            "1.3.6.1.2.1.1.5.0",    // sysName
            "1.3.6.1.2.1.1.6.0",    // sysLocation
            "1.3.6.1.2.1.1.7.0",    // sysServices

            // Interfaces group (1.3.6.1.2.1.2)
            "1.3.6.1.2.1.2.1.0",    // ifNumber
            "1.3.6.1.2.1.2.2.1.1",  // ifIndex
            "1.3.6.1.2.1.2.2.1.2",  // ifDescr
            "1.3.6.1.2.1.2.2.1.3",  // ifType
            "1.3.6.1.2.1.2.2.1.4",  // ifMtu
            "1.3.6.1.2.1.2.2.1.5",  // ifSpeed
            "1.3.6.1.2.1.2.2.1.6",  // ifPhysAddress
            "1.3.6.1.2.1.2.2.1.7",  // ifAdminStatus
            "1.3.6.1.2.1.2.2.1.8",  // ifOperStatus
            "1.3.6.1.2.1.2.2.1.9",  // ifLastChange
            "1.3.6.1.2.1.2.2.1.10", // ifInOctets
            "1.3.6.1.2.1.2.2.1.11", // ifInUcastPkts
            "1.3.6.1.2.1.2.2.1.12", // ifInNUcastPkts
            "1.3.6.1.2.1.2.2.1.13", // ifInDiscards
            "1.3.6.1.2.1.2.2.1.14", // ifInErrors
            "1.3.6.1.2.1.2.2.1.15", // ifInUnknownProtos
            "1.3.6.1.2.1.2.2.1.16", // ifOutOctets
            "1.3.6.1.2.1.2.2.1.17", // ifOutUcastPkts
            "1.3.6.1.2.1.2.2.1.18", // ifOutNUcastPkts
            "1.3.6.1.2.1.2.2.1.19", // ifOutDiscards
            "1.3.6.1.2.1.2.2.1.20", // ifOutErrors
            "1.3.6.1.2.1.2.2.1.21", // ifOutQLen
            "1.3.6.1.2.1.2.2.1.22", // ifSpecific

            // Address Translation group (1.3.6.1.2.1.3) - deprecated
            "1.3.6.1.2.1.3.1.1.1",  // atIfIndex
            "1.3.6.1.2.1.3.1.1.2",  // atPhysAddress
            "1.3.6.1.2.1.3.1.1.3",  // atNetAddress

            // IP group (1.3.6.1.2.1.4)
            "1.3.6.1.2.1.4.1.0",    // ipForwarding
            "1.3.6.1.2.1.4.2.0",    // ipDefaultTTL
            "1.3.6.1.2.1.4.3.0",    // ipInReceives
            "1.3.6.1.2.1.4.4.0",    // ipInHdrErrors
            "1.3.6.1.2.1.4.5.0",    // ipInAddrErrors
            "1.3.6.1.2.1.4.6.0",    // ipForwDatagrams
            "1.3.6.1.2.1.4.7.0",    // ipInUnknownProtos
            "1.3.6.1.2.1.4.8.0",    // ipInDiscards
            "1.3.6.1.2.1.4.9.0",    // ipInDelivers
            "1.3.6.1.2.1.4.10.0",   // ipOutRequests
            "1.3.6.1.2.1.4.11.0",   // ipOutDiscards
            "1.3.6.1.2.1.4.12.0",   // ipOutNoRoutes

            // ICMP group (1.3.6.1.2.1.5)
            "1.3.6.1.2.1.5.1.0",    // icmpInMsgs
            "1.3.6.1.2.1.5.2.0",    // icmpInErrors
            "1.3.6.1.2.1.5.3.0",    // icmpInDestUnreachs
            "1.3.6.1.2.1.5.4.0",    // icmpInTimeExcds
            "1.3.6.1.2.1.5.5.0",    // icmpInParmProbs
            "1.3.6.1.2.1.5.6.0",    // icmpInSrcQuenchs
            "1.3.6.1.2.1.5.7.0",    // icmpInRedirects
            "1.3.6.1.2.1.5.8.0",    // icmpInEchos
            "1.3.6.1.2.1.5.9.0",    // icmpInEchoReps
            "1.3.6.1.2.1.5.10.0",   // icmpInTimestamps

            // TCP group (1.3.6.1.2.1.6)
            "1.3.6.1.2.1.6.1.0",    // tcpRtoAlgorithm
            "1.3.6.1.2.1.6.2.0",    // tcpRtoMin
            "1.3.6.1.2.1.6.3.0",    // tcpRtoMax
            "1.3.6.1.2.1.6.4.0",    // tcpMaxConn
            "1.3.6.1.2.1.6.5.0",    // tcpActiveOpens
            "1.3.6.1.2.1.6.6.0",    // tcpPassiveOpens
            "1.3.6.1.2.1.6.7.0",    // tcpAttemptFails
            "1.3.6.1.2.1.6.8.0",    // tcpEstabResets
            "1.3.6.1.2.1.6.9.0",    // tcpCurrEstab
            "1.3.6.1.2.1.6.10.0",   // tcpInSegs
            "1.3.6.1.2.1.6.11.0",   // tcpOutSegs
            "1.3.6.1.2.1.6.12.0",   // tcpRetransSegs
            "1.3.6.1.2.1.6.14.0",   // tcpInErrs
            "1.3.6.1.2.1.6.15.0",   // tcpOutRsts

            // UDP group (1.3.6.1.2.1.7)
            "1.3.6.1.2.1.7.1.0",    // udpInDatagrams
            "1.3.6.1.2.1.7.2.0",    // udpNoPorts
            "1.3.6.1.2.1.7.3.0",    // udpInErrors
            "1.3.6.1.2.1.7.4.0",    // udpOutDatagrams

            // SNMP group (1.3.6.1.2.1.11)
            "1.3.6.1.2.1.11.1.0",   // snmpInPkts
            "1.3.6.1.2.1.11.2.0",   // snmpOutPkts
            "1.3.6.1.2.1.11.3.0",   // snmpInBadVersions
            "1.3.6.1.2.1.11.4.0",   // snmpInBadCommunityNames
            "1.3.6.1.2.1.11.5.0",   // snmpInBadCommunityUses
            "1.3.6.1.2.1.11.6.0",   // snmpInASNParseErrs
            "1.3.6.1.2.1.11.8.0",   // snmpInTooBigs
            "1.3.6.1.2.1.11.9.0",   // snmpInNoSuchNames
            "1.3.6.1.2.1.11.10.0",  // snmpInBadValues
            "1.3.6.1.2.1.11.11.0",  // snmpInReadOnlys
            "1.3.6.1.2.1.11.12.0",  // snmpInGenErrs
            "1.3.6.1.2.1.11.13.0",  // snmpInTotalReqVars
            "1.3.6.1.2.1.11.14.0",  // snmpInTotalSetVars
            "1.3.6.1.2.1.11.15.0",  // snmpInGetRequests
            "1.3.6.1.2.1.11.16.0",  // snmpInGetNexts
            "1.3.6.1.2.1.11.17.0",  // snmpInSetRequests
            "1.3.6.1.2.1.11.18.0",  // snmpInGetResponses
            "1.3.6.1.2.1.11.19.0",  // snmpInTraps
            "1.3.6.1.2.1.11.20.0",  // snmpOutTooBigs
            "1.3.6.1.2.1.11.21.0",  // snmpOutNoSuchNames
            "1.3.6.1.2.1.11.22.0",  // snmpOutBadValues
            "1.3.6.1.2.1.11.24.0",  // snmpOutGenErrs
            "1.3.6.1.2.1.11.25.0",  // snmpOutGetRequests
            "1.3.6.1.2.1.11.26.0",  // snmpOutGetNexts
            "1.3.6.1.2.1.11.27.0",  // snmpOutSetRequests
            "1.3.6.1.2.1.11.28.0",  // snmpOutGetResponses
            "1.3.6.1.2.1.11.29.0",  // snmpOutTraps
            "1.3.6.1.2.1.11.30.0",  // snmpEnableAuthenTraps

            // Common trap OIDs
            "1.3.6.1.6.3.1.1.4.1.0",   // snmpTrapOID
            "1.3.6.1.2.1.1.3.0",       // sysUpTimeInstance (for traps)
            "1.3.6.1.6.3.1.1.5.1",     // coldStart
            "1.3.6.1.6.3.1.1.5.2",     // warmStart
            "1.3.6.1.6.3.1.1.5.3",     // linkDown
            "1.3.6.1.6.3.1.1.5.4",     // linkUp
            "1.3.6.1.6.3.1.1.5.5",     // authenticationFailure

            // OID tree structure
            "1.3.6.1",                 // internet
            "1.3.6.1.2",               // mgmt
            "1.3.6.1.2.1",             // mib-2
            "1.3.6.1.3",               // experimental
            "1.3.6.1.4",               // private
            "1.3.6.1.4.1",             // enterprises
            "1.3.6.1.6",               // snmpV2
            "1.3.6.1.6.3",             // snmpModules
        };

        static OidInterning()
        {
            // Pre-intern common OIDs for immediate availability
            foreach (var oid in _commonOids)
            {
                _internedOids[oid] = string.Intern(oid);
            }
        }

        /// <summary>
        /// Interns an OID string, returning the interned version
        /// Returns the original string if interning is disabled
        /// </summary>
        public static string Intern(string oidString)
        {
            if (!MemoryOptimizationSettings.UseStringInterning || string.IsNullOrEmpty(oidString))
            {
                return oidString;
            }

            // Check if already interned
            if (_internedOids.TryGetValue(oidString, out var interned))
            {
                return interned;
            }

            // For performance, only intern if it's commonly used or already in common list
            if (_commonOids.Contains(oidString) || ShouldIntern(oidString))
            {
                var internedString = string.Intern(oidString);
                _internedOids[oidString] = internedString;
                return internedString;
            }

            return oidString;
        }

        /// <summary>
        /// Determines if an OID string should be interned based on heuristics
        /// </summary>
        private static bool ShouldIntern(string oidString)
        {
            // Intern if it's a standard MIB-2 OID (starts with 1.3.6.1.2.1)
            if (oidString.StartsWith("1.3.6.1.2.1"))
                return true;

            // Intern if it's a common SNMP OID (starts with 1.3.6.1.6.3)
            if (oidString.StartsWith("1.3.6.1.6.3"))
                return true;

            // Intern short OIDs (likely to be reused)
            if (oidString.Length <= 20)
                return true;

            // Don't intern very long OIDs (likely unique)
            if (oidString.Length > 100)
                return false;

            // Intern based on cache hit patterns
            return _internedOids.Count < 10000; // Limit cache size
        }

        /// <summary>
        /// Adds a custom OID to the interning set
        /// Useful for applications that frequently use specific enterprise OIDs
        /// </summary>
        public static void AddCustomOid(string oidString)
        {
            if (!MemoryOptimizationSettings.UseStringInterning || string.IsNullOrEmpty(oidString))
                return;

            var internedString = string.Intern(oidString);
            _internedOids[oidString] = internedString;
        }

        /// <summary>
        /// Adds multiple custom OIDs to the interning set
        /// </summary>
        public static void AddCustomOids(IEnumerable<string> oidStrings)
        {
            if (!MemoryOptimizationSettings.UseStringInterning)
                return;

            foreach (var oid in oidStrings)
            {
                if (!string.IsNullOrEmpty(oid))
                {
                    var internedString = string.Intern(oid);
                    _internedOids[oid] = internedString;
                }
            }
        }

        /// <summary>
        /// Gets statistics about the interning cache
        /// </summary>
        public static InterningStats GetStats()
        {
            return new InterningStats
            {
                InternedCount = _internedOids.Count,
                CommonOidsCount = _commonOids.Count,
                IsEnabled = MemoryOptimizationSettings.UseStringInterning
            };
        }

        /// <summary>
        /// Clears the interning cache (keeps common OIDs)
        /// </summary>
        public static void ClearCache()
        {
            _internedOids.Clear();

            // Re-add common OIDs
            foreach (var oid in _commonOids)
            {
                _internedOids[oid] = string.Intern(oid);
            }
        }
    }

    /// <summary>
    /// Statistics about OID string interning
    /// </summary>
    public record InterningStats
    {
        public int InternedCount { get; init; }
        public int CommonOidsCount { get; init; }
        public bool IsEnabled { get; init; }

        public override string ToString()
        {
            return $"OID Interning: {InternedCount} cached, {CommonOidsCount} common, Enabled: {IsEnabled}";
        }
    }
}