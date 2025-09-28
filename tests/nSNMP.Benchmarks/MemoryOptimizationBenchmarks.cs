using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using nSNMP.Core;
using nSNMP.SMI.Configuration;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.Manager;

namespace nSNMP.Benchmarks
{
    /// <summary>
    /// Benchmarks comparing original vs ArrayPool-optimized memory allocation
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class MemoryOptimizationBenchmarks
    {
        private VarBind[] _testVarBinds = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Create test data - 100 varbinds for bulk operations
            _testVarBinds = Enumerable.Range(0, 100)
                .Select(i => new VarBind(
                    ObjectIdentifier.Create($"1.3.6.1.2.1.1.{i}.0"),
                    Integer.Create(i)))
                .ToArray();
        }

        [Benchmark]
        public byte[] OriginalBEREncoding()
        {
            // Disable array pooling to use original implementation
            MemoryOptimizationSettings.UseArrayPooling = false;

            try
            {
                return EncodeBulkVarBinds();
            }
            finally
            {
                MemoryOptimizationSettings.UseArrayPooling = true; // Reset
            }
        }

        [Benchmark]
        public byte[] PooledBEREncoding()
        {
            // Ensure array pooling is enabled
            MemoryOptimizationSettings.UseArrayPooling = true;

            return EncodeBulkVarBinds();
        }

        [Benchmark]
        public ObjectIdentifier OriginalOidCreation()
        {
            MemoryOptimizationSettings.UseArrayPooling = false;

            try
            {
                return ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            }
            finally
            {
                MemoryOptimizationSettings.UseArrayPooling = true;
            }
        }

        [Benchmark]
        public ObjectIdentifier PooledOidCreation()
        {
            MemoryOptimizationSettings.UseArrayPooling = true;

            return ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
        }

        [Benchmark]
        public byte[] OriginalSequenceEncoding()
        {
            MemoryOptimizationSettings.UseArrayPooling = false;

            try
            {
                var sequence = CreateTestSequence();
                return sequence.ToBytes();
            }
            finally
            {
                MemoryOptimizationSettings.UseArrayPooling = true;
            }
        }

        [Benchmark]
        public byte[] PooledSequenceEncoding()
        {
            MemoryOptimizationSettings.UseArrayPooling = true;

            var sequence = CreateTestSequence();
            return sequence.ToBytes();
        }

        /// <summary>
        /// Memory stress test: Create and encode many objects to measure GC pressure
        /// </summary>
        [Benchmark]
        [Arguments(false)] // Original implementation
        [Arguments(true)]  // Pooled implementation
        public void MemoryStressTest(bool usePooling)
        {
            MemoryOptimizationSettings.UseArrayPooling = usePooling;

            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    var oid = ObjectIdentifier.Create($"1.3.6.1.2.1.1.{i}.0");
                    var value = Integer.Create(i);
                    var vb = new VarBind(oid, value);

                    // Force encoding to trigger memory allocations
                    var encoded = oid.ToBytes();
                    var valueEncoded = value.ToBytes();
                }
            }
            finally
            {
                MemoryOptimizationSettings.UseArrayPooling = true; // Reset to default
            }
        }

        /// <summary>
        /// Compare allocation patterns for different OID lengths
        /// </summary>
        [Benchmark]
        [Arguments("1.1", false)]
        [Arguments("1.1", true)]
        [Arguments("1.3.6.1.2.1.1.1.0", false)]
        [Arguments("1.3.6.1.2.1.1.1.0", true)]
        [Arguments("1.3.6.1.4.1.9.9.109.1.1.1.1.3.2", false)]
        [Arguments("1.3.6.1.4.1.9.9.109.1.1.1.1.3.2", true)]
        public byte[] OidEncodingByLength(string oidString, bool usePooling)
        {
            MemoryOptimizationSettings.UseArrayPooling = usePooling;

            try
            {
                var oid = ObjectIdentifier.Create(oidString);
                return oid.ToBytes();
            }
            finally
            {
                MemoryOptimizationSettings.UseArrayPooling = true; // Reset to default
            }
        }

        private byte[] EncodeBulkVarBinds()
        {
            var varbindList = new List<nSNMP.SMI.DataTypes.IDataType>();
            foreach (var vb in _testVarBinds)
            {
                varbindList.Add(new Sequence(new nSNMP.SMI.DataTypes.IDataType[] { vb.Oid, vb.Value }));
            }
            var sequence = new Sequence(varbindList.ToArray());
            return sequence.ToBytes();
        }

        /// <summary>
        /// Compare memory usage with and without string interning
        /// </summary>
        [Benchmark]
        [Arguments(false)] // No string interning
        [Arguments(true)]  // String interning enabled
        public void StringInterningTest(bool useInterning)
        {
            MemoryOptimizationSettings.UseStringInterning = useInterning;

            try
            {
                // Create the same OID strings multiple times to measure interning benefits
                var commonOids = new[]
                {
                    "1.3.6.1.2.1.1.1.0",
                    "1.3.6.1.2.1.1.2.0",
                    "1.3.6.1.2.1.1.3.0",
                    "1.3.6.1.2.1.2.1.0",
                    "1.3.6.1.2.1.2.2.1.1",
                    "1.3.6.1.2.1.2.2.1.2"
                };

                var oids = new List<ObjectIdentifier>();

                // Create each OID multiple times to simulate real usage
                for (int iteration = 0; iteration < 100; iteration++)
                {
                    foreach (var oidString in commonOids)
                    {
                        var oid = ObjectIdentifier.Create(oidString);
                        oids.Add(oid);

                        // Also call ToString to trigger string creation
                        var _ = oid.ToString();
                    }
                }
            }
            finally
            {
                MemoryOptimizationSettings.UseStringInterning = true; // Reset to default
            }
        }

        /// <summary>
        /// Benchmark OID creation with different interning settings
        /// </summary>
        [Benchmark]
        public string OidToStringWithInterning()
        {
            MemoryOptimizationSettings.UseStringInterning = true;
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            return oid.ToString();
        }

        [Benchmark]
        public string OidToStringWithoutInterning()
        {
            MemoryOptimizationSettings.UseStringInterning = false;

            try
            {
                var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
                return oid.ToString();
            }
            finally
            {
                MemoryOptimizationSettings.UseStringInterning = true; // Reset
            }
        }

        private Sequence CreateTestSequence()
        {
            var varbindList = new List<nSNMP.SMI.DataTypes.IDataType>();
            foreach (var vb in _testVarBinds.Take(10))
            {
                varbindList.Add(new Sequence(new nSNMP.SMI.DataTypes.IDataType[] { vb.Oid, vb.Value }));
            }
            return new Sequence(varbindList.ToArray());
        }
    }
}