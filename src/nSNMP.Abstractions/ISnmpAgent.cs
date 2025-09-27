using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Contract for SNMP agent operations
    /// </summary>
    public interface ISnmpAgent : IDisposable
    {
        /// <summary>
        /// Gets the endpoint the agent is listening on
        /// </summary>
        IPEndPoint? Endpoint { get; }

        /// <summary>
        /// Gets whether the agent is currently running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the SNMP agent
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the SNMP agent
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a scalar object handler
        /// </summary>
        /// <param name="oid">Object identifier</param>
        /// <param name="handler">Handler function</param>
        void RegisterScalar(string oid, Func<IDataType> handler);

        /// <summary>
        /// Registers a table provider
        /// </summary>
        /// <param name="baseOid">Base OID of the table</param>
        /// <param name="provider">Table data provider</param>
        void RegisterTable(string baseOid, ITableProvider provider);

        /// <summary>
        /// Unregisters an object handler
        /// </summary>
        /// <param name="oid">Object identifier to unregister</param>
        void Unregister(string oid);

        /// <summary>
        /// Event raised when an SNMP request is received
        /// </summary>
        event EventHandler<SnmpRequestEventArgs>? RequestReceived;

        /// <summary>
        /// Event raised when an SNMP response is sent
        /// </summary>
        event EventHandler<SnmpResponseEventArgs>? ResponseSent;
    }

    /// <summary>
    /// Event arguments for SNMP request events
    /// </summary>
    public class SnmpRequestEventArgs : EventArgs
    {
        public IPEndPoint Source { get; init; } = null!;
        public SnmpVersion Version { get; init; }
        public string Operation { get; init; } = string.Empty;
        public IReadOnlyList<string> Oids { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Event arguments for SNMP response events
    /// </summary>
    public class SnmpResponseEventArgs : EventArgs
    {
        public IPEndPoint Destination { get; init; } = null!;
        public SnmpVersion Version { get; init; }
        public ErrorStatus ErrorStatus { get; init; }
        public IReadOnlyList<IVarBind> VarBinds { get; init; } = Array.Empty<IVarBind>();
    }
}