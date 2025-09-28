using System.Net;
using nSNMP.Transport;

namespace nSNMP.Core.Tests
{
    /// <summary>
    /// Mock UDP transport for testing SNMP Manager functionality
    /// </summary>
    public class MockUdpChannel : IUdpChannel
    {
        private readonly Dictionary<byte[], byte[]> _responses = new();
        private readonly List<(byte[] data, IPEndPoint endpoint)> _sentMessages = new();
        private bool _disposed;

        /// <summary>
        /// Configure a response for a specific request
        /// </summary>
        public void SetResponse(byte[] request, byte[] response)
        {
            _responses[request] = response;
        }

        /// <summary>
        /// Get all messages that were sent
        /// </summary>
        public IReadOnlyList<(byte[] data, IPEndPoint endpoint)> SentMessages => _sentMessages.AsReadOnly();

        public async Task<byte[]> SendReceiveAsync(byte[] data, IPEndPoint endpoint, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MockUdpChannel));

            _sentMessages.Add((data, endpoint));

            // Find matching response
            foreach (var kvp in _responses)
            {
                if (kvp.Key.SequenceEqual(data))
                {
                    await Task.Delay(10, cancellationToken); // Simulate network delay
                    return kvp.Value;
                }
            }

            throw new TimeoutException("No response configured for request");
        }

        public async Task SendAsync(byte[] data, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MockUdpChannel));

            _sentMessages.Add((data, endpoint));
            await Task.Delay(10, cancellationToken); // Simulate network delay
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}