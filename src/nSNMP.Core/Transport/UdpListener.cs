using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace nSNMP.Transport
{
    /// <summary>
    /// UDP listener implementation for SNMP agent
    /// </summary>
    public class UdpListener : IUdpListener
    {
        private UdpClient? _udpClient;
        private bool _disposed;

        public bool IsListening => _udpClient != null && !_disposed;

        public async IAsyncEnumerable<UdpRequest> ListenAsync(int port, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UdpListener));

            _udpClient = new UdpClient(port);

            try
            {
                while (!cancellationToken.IsCancellationRequested && !_disposed)
                {
                    UdpReceiveResult result;
                    try
                    {
                        result = await _udpClient.ReceiveAsync(cancellationToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Normal shutdown
                        break;
                    }
                    catch (SocketException)
                    {
                        // Network error, continue listening
                        continue;
                    }

                    // Create response function that captures the client and remote endpoint
                    var sendResponse = async (byte[] responseData) =>
                    {
                        try
                        {
                            if (_udpClient != null && !_disposed)
                            {
                                await _udpClient.SendAsync(responseData, result.RemoteEndPoint);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Client was disposed, ignore
                        }
                        catch (SocketException)
                        {
                            // Network error sending response, ignore
                        }
                    };

                    yield return new UdpRequest(result.Buffer, result.RemoteEndPoint, sendResponse);
                }
            }
            finally
            {
                _udpClient?.Close();
                _udpClient = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }
}