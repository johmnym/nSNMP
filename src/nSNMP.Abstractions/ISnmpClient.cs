using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Contract for SNMP client operations
    /// </summary>
    public interface ISnmpClient : IDisposable
    {
        /// <summary>
        /// Gets the target endpoint for SNMP operations
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Gets the SNMP version being used
        /// </summary>
        SnmpVersion Version { get; }

        /// <summary>
        /// Performs a synchronous SNMP GET operation
        /// </summary>
        /// <param name="oids">Object identifiers to retrieve</param>
        /// <returns>Variable bindings with retrieved values</returns>
        Task<IReadOnlyList<IVarBind>> GetAsync(params string[] oids);

        /// <summary>
        /// Performs a synchronous SNMP GET operation with cancellation
        /// </summary>
        /// <param name="oids">Object identifiers to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Variable bindings with retrieved values</returns>
        Task<IReadOnlyList<IVarBind>> GetAsync(IEnumerable<string> oids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a synchronous SNMP GET-NEXT operation
        /// </summary>
        /// <param name="oids">Object identifiers to get next values for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Variable bindings with next values</returns>
        Task<IReadOnlyList<IVarBind>> GetNextAsync(IEnumerable<string> oids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a synchronous SNMP GET-BULK operation
        /// </summary>
        /// <param name="nonRepeaters">Number of non-repeating variables</param>
        /// <param name="maxRepetitions">Maximum number of repetitions</param>
        /// <param name="oids">Object identifiers to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Variable bindings with bulk retrieved values</returns>
        Task<IReadOnlyList<IVarBind>> GetBulkAsync(int nonRepeaters, int maxRepetitions, IEnumerable<string> oids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a synchronous SNMP SET operation
        /// </summary>
        /// <param name="varBinds">Variable bindings to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Variable bindings with set results</returns>
        Task<IReadOnlyList<IVarBind>> SetAsync(IEnumerable<IVarBind> varBinds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs an SNMP table walk operation
        /// </summary>
        /// <param name="tableOid">Base OID of the table to walk</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All variable bindings in the table</returns>
        IAsyncEnumerable<IVarBind> WalkAsync(string tableOid, CancellationToken cancellationToken = default);
    }
}