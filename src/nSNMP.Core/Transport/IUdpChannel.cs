using System.Net;

namespace nSNMP.Transport
{
    /// <summary>
    /// Abstraction for UDP communication to enable testability
    /// </summary>
    public interface IUdpChannel : IDisposable
    {
        /// <summary>
        /// Send data to the specified endpoint and wait for a response
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="endpoint">Target endpoint</param>
        /// <param name="timeout">Operation timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        Task<byte[]> SendReceiveAsync(byte[] data, IPEndPoint endpoint, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send data without waiting for a response (for traps/informs)
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="endpoint">Target endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SendAsync(byte[] data, IPEndPoint endpoint, CancellationToken cancellationToken = default);
    }
}