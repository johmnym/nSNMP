using System.Net;

namespace nSNMP.Transport
{
    /// <summary>
    /// UDP request received by the agent
    /// </summary>
    public record UdpRequest(byte[] Data, IPEndPoint RemoteEndPoint, Func<byte[], Task> SendResponseAsync);

    /// <summary>
    /// Abstraction for UDP server to enable testability
    /// </summary>
    public interface IUdpListener : IDisposable
    {
        /// <summary>
        /// Start listening for UDP requests on the specified port
        /// </summary>
        /// <param name="port">Port to listen on</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Async enumerable of incoming requests</returns>
        IAsyncEnumerable<UdpRequest> ListenAsync(int port, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the listener is currently active
        /// </summary>
        bool IsListening { get; }
    }
}