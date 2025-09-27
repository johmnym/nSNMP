using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace nSNMP.Transport
{
    /// <summary>
    /// UDP transport implementation for SNMP communication
    /// </summary>
    public class UdpChannel : IUdpChannel
    {
        private readonly UdpClient _udpClient;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _receiveTask;
        private bool _disposed;

        public UdpChannel()
        {
            _udpClient = new UdpClient();
            _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
            _cancellationTokenSource = new CancellationTokenSource();
            _receiveTask = ReceiveLoop();
        }

        public async Task<byte[]> SendReceiveAsync(byte[] data, IPEndPoint endpoint, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UdpChannel));

            // Create correlation key for this request
            var correlationKey = $"{endpoint}:{Environment.TickCount}";
            var tcs = new TaskCompletionSource<byte[]>();

            if (!_pendingRequests.TryAdd(correlationKey, tcs))
                throw new InvalidOperationException("Failed to register request");

            try
            {
                // Send the request
                await _udpClient.SendAsync(data, endpoint, cancellationToken);

                // Wait for response with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                try
                {
                    return await tcs.Task.WaitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    throw new TimeoutException($"SNMP request to {endpoint} timed out after {timeout}");
                }
            }
            finally
            {
                _pendingRequests.TryRemove(correlationKey, out _);
            }
        }

        public async Task SendAsync(byte[] data, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UdpChannel));

            await _udpClient.SendAsync(data, endpoint, cancellationToken);
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync(_cancellationTokenSource.Token);

                    // Find pending request for this endpoint
                    var correlationKey = $"{result.RemoteEndPoint}:";
                    var matchingRequest = _pendingRequests.FirstOrDefault(kvp => kvp.Key.StartsWith(correlationKey));

                    if (matchingRequest.Key != null)
                    {
                        if (_pendingRequests.TryRemove(matchingRequest.Key, out var tcs))
                        {
                            tcs.SetResult(result.Buffer);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.WriteLine($"UDP receive error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cancellationTokenSource.Cancel();

            // Complete all pending requests with cancellation
            foreach (var kvp in _pendingRequests)
            {
                if (_pendingRequests.TryRemove(kvp.Key, out var tcs))
                {
                    tcs.SetCanceled();
                }
            }

            try
            {
                _receiveTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore timeout on shutdown
            }

            _udpClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}