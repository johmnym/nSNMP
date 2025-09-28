using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Transport abstraction for UDP communication
    /// </summary>
    public interface IUdpChannel : IDisposable
    {
        /// <summary>
        /// Gets the local endpoint the channel is bound to
        /// </summary>
        IPEndPoint? LocalEndpoint { get; }

        /// <summary>
        /// Gets whether the channel is currently open
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Opens the UDP channel
        /// </summary>
        /// <param name="localEndpoint">Local endpoint to bind to, or null for any</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OpenAsync(IPEndPoint? localEndpoint = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the UDP channel
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Sends data to a remote endpoint
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndpoint">Remote endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SendAsync(byte[] data, IPEndPoint remoteEndpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives data from the channel
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Received data and source endpoint</returns>
        Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when data is received
        /// </summary>
        event EventHandler<UdpDataReceivedEventArgs>? DataReceived;
    }

    /// <summary>
    /// Result of a UDP receive operation
    /// </summary>
    public record UdpReceiveResult(byte[] Data, IPEndPoint RemoteEndpoint);

    /// <summary>
    /// Event arguments for UDP data received events
    /// </summary>
    public class UdpDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public IPEndPoint Source { get; init; } = null!;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}