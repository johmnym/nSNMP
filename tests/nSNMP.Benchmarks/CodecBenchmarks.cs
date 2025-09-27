using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.PDUs;
using nSNMP.Manager;

namespace nSNMP.Benchmarks
{
    /// <summary>
    /// Benchmarks for core SNMP codec operations targeting 100k varbinds/s encode/decode
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class CodecBenchmarks
    {
        private ObjectIdentifier _testOid = null!;
        private Integer _testInteger = null!;
        private OctetString _testOctetString = null!;
        private VarBind _testVarBind = null!;
        private VarBind[] _testVarBinds = null!;
        private Sequence _testSequence = null!;
        private GetRequest _testGetRequest = null!;
        private GetResponse _testGetResponse = null!;
        private byte[] _encodedVarBind = null!;
        private byte[] _encodedSequence = null!;
        private byte[] _encodedGetRequest = null!;
        private byte[] _encodedGetResponse = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Initialize test data
            _testOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            _testInteger = Integer.Create(42);
            _testOctetString = OctetString.Create("Test system description");
            _testVarBind = new VarBind(_testOid, _testInteger);

            // Create array of varbinds for bulk operations
            _testVarBinds = Enumerable.Range(0, 100)
                .Select(i => new VarBind(
                    ObjectIdentifier.Create($"1.3.6.1.2.1.1.{i}.0"),
                    Integer.Create(i)))
                .ToArray();

            // Create test sequence
            var varbindList = new List<IDataType>();
            foreach (var vb in _testVarBinds.Take(10))
            {
                varbindList.Add(new Sequence(new IDataType[] { vb.Oid, vb.Value }));
            }
            _testSequence = new Sequence(varbindList.ToArray());

            // Create test PDUs
            _testGetRequest = new GetRequest(
                null,
                Integer.Create(12345),
                Integer.Create(0),
                Integer.Create(0),
                _testSequence
            );

            _testGetResponse = new GetResponse(
                null,
                Integer.Create(12345),
                Integer.Create(0),
                Integer.Create(0),
                _testSequence
            );

            // Pre-encode data for decode benchmarks
            _encodedVarBind = _testVarBind.Oid.ToBytes();
            _encodedSequence = _testSequence.ToBytes();
            _encodedGetRequest = _testGetRequest.ToBytes();
            _encodedGetResponse = _testGetResponse.ToBytes();
        }

        [Benchmark]
        public ObjectIdentifier OidParsing()
        {
            return ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
        }

        [Benchmark]
        public byte[] OidEncoding()
        {
            return _testOid.ToBytes();
        }

        [Benchmark]
        public ObjectIdentifier OidDecoding()
        {
            return new ObjectIdentifier(_encodedVarBind);
        }

        [Benchmark]
        public Integer IntegerCreation()
        {
            return Integer.Create(42);
        }

        [Benchmark]
        public byte[] IntegerEncoding()
        {
            return _testInteger.ToBytes();
        }

        [Benchmark]
        public OctetString OctetStringCreation()
        {
            return OctetString.Create("Test system description");
        }

        [Benchmark]
        public byte[] OctetStringEncoding()
        {
            return _testOctetString.ToBytes();
        }

        [Benchmark]
        public VarBind VarBindCreation()
        {
            return new VarBind(_testOid, _testInteger);
        }

        [Benchmark]
        public byte[] SequenceEncoding()
        {
            return _testSequence.ToBytes();
        }

        [Benchmark]
        public byte[] GetRequestEncoding()
        {
            return _testGetRequest.ToBytes();
        }

        [Benchmark]
        public byte[] GetResponseEncoding()
        {
            return _testGetResponse.ToBytes();
        }

        /// <summary>
        /// Critical benchmark: Encode/decode 100 varbinds targeting 100k/s throughput
        /// </summary>
        [Benchmark]
        public byte[] BulkVarBindEncoding()
        {
            var varbindList = new List<IDataType>();
            foreach (var vb in _testVarBinds)
            {
                varbindList.Add(new Sequence(new IDataType[] { vb.Oid, vb.Value }));
            }
            var sequence = new Sequence(varbindList.ToArray());
            return sequence.ToBytes();
        }

        /// <summary>
        /// Round-trip test: encode then decode varbinds
        /// </summary>
        [Benchmark]
        public byte[] RoundTripVarBinds()
        {
            var varbindList = new List<IDataType>();
            foreach (var vb in _testVarBinds.Take(10))
            {
                varbindList.Add(new Sequence(new IDataType[] { vb.Oid, vb.Value }));
            }
            var sequence = new Sequence(varbindList.ToArray());
            return sequence.ToBytes();
        }

        /// <summary>
        /// Memory allocation test: measure GC pressure
        /// </summary>
        [Benchmark]
        public void MemoryAllocationTest()
        {
            for (int i = 0; i < 1000; i++)
            {
                var oid = ObjectIdentifier.Create($"1.3.6.1.2.1.1.{i}.0");
                var value = Integer.Create(i);
                var vb = new VarBind(oid, value);
                var encoded = oid.ToBytes();
            }
        }
    }
}