using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using nSNMP.Transport;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.DataTypes.V1.Constructed;
using System.Net;

namespace nSNMP.Benchmarks
{
    /// <summary>
    /// Benchmarks for network transport and message processing
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class NetworkBenchmarks
    {
        private byte[] _smallMessage = null!;
        private byte[] _mediumMessage = null!;
        private byte[] _largeMessage = null!;
        private IPEndPoint _endpoint = null!;
        private MockUdpChannel _mockChannel = null!;

        [GlobalSetup]
        public void Setup()
        {
            _endpoint = new IPEndPoint(IPAddress.Loopback, 161);
            _mockChannel = new MockUdpChannel();

            // Create test messages of different sizes
            _smallMessage = CreateTestMessage(1); // ~100 bytes
            _mediumMessage = CreateTestMessage(10); // ~1KB
            _largeMessage = CreateTestMessage(100); // ~10KB
        }

        private byte[] CreateTestMessage(int varbindCount)
        {
            var varbinds = new List<nSNMP.SMI.DataTypes.IDataType>();
            for (int i = 0; i < varbindCount; i++)
            {
                var oid = ObjectIdentifier.Create($"1.3.6.1.2.1.1.{i}.0");
                var value = OctetString.Create($"Test value {i} - some longer text to make the message bigger");
                varbinds.Add(new Sequence(new nSNMP.SMI.DataTypes.IDataType[] { oid, value }));
            }

            var varbindList = new Sequence(varbinds.ToArray());
            var request = new GetRequest(null, Integer.Create(12345), Integer.Create(0), Integer.Create(0), varbindList);
            return request.ToBytes();
        }

        [Benchmark]
        public Task<byte[]> MockNetworkSmallMessage()
        {
            return _mockChannel.SendReceiveAsync(_smallMessage, _endpoint, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        [Benchmark]
        public Task<byte[]> MockNetworkMediumMessage()
        {
            return _mockChannel.SendReceiveAsync(_mediumMessage, _endpoint, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        [Benchmark]
        public Task<byte[]> MockNetworkLargeMessage()
        {
            return _mockChannel.SendReceiveAsync(_largeMessage, _endpoint, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        [Benchmark]
        public int MessageSizeSmall()
        {
            return _smallMessage.Length;
        }

        [Benchmark]
        public int MessageSizeMedium()
        {
            return _mediumMessage.Length;
        }

        [Benchmark]
        public int MessageSizeLarge()
        {
            return _largeMessage.Length;
        }

        /// <summary>
        /// Simulate packet fragmentation scenarios
        /// </summary>
        [Benchmark]
        public byte[][] PacketFragmentation()
        {
            const int maxPacketSize = 1472; // Typical Ethernet MTU - headers
            var message = _largeMessage;
            var fragments = new List<byte[]>();

            for (int i = 0; i < message.Length; i += maxPacketSize)
            {
                var fragmentSize = Math.Min(maxPacketSize, message.Length - i);
                var fragment = new byte[fragmentSize];
                Array.Copy(message, i, fragment, 0, fragmentSize);
                fragments.Add(fragment);
            }

            return fragments.ToArray();
        }

        /// <summary>
        /// Memory copying benchmark for network operations
        /// </summary>
        [Benchmark]
        public byte[] BufferCopyBenchmark()
        {
            var source = _largeMessage;
            var destination = new byte[source.Length];
            Array.Copy(source, destination, source.Length);
            return destination;
        }

        /// <summary>
        /// Span-based memory operations
        /// </summary>
        [Benchmark]
        public byte[] SpanCopyBenchmark()
        {
            var source = _largeMessage.AsSpan();
            var destination = new byte[source.Length];
            source.CopyTo(destination);
            return destination;
        }
    }

    /// <summary>
    /// Mock UDP channel for benchmarking without actual network I/O
    /// </summary>
    public class MockUdpChannel : IUdpChannel
    {
        public Task SendAsync(byte[] data, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<byte[]> SendReceiveAsync(byte[] data, IPEndPoint endpoint, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Simulate network round-trip with immediate response
            return Task.FromResult(data);
        }

        public void Dispose()
        {
            // No cleanup needed for mock
        }
    }
}