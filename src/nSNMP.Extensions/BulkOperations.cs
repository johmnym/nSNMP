using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;

namespace nSNMP.Extensions
{
    /// <summary>
    /// Extension methods for bulk SNMP operations
    /// </summary>
    public static class BulkOperations
    {
        /// <summary>
        /// Performs a bulk GET operation on multiple OIDs with batching
        /// </summary>
        /// <param name="client">SNMP client</param>
        /// <param name="oids">OIDs to retrieve</param>
        /// <param name="batchSize">Number of OIDs per batch</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All variable bindings</returns>
        public static async Task<IReadOnlyList<IVarBind>> GetBatchAsync(
            this ISnmpClient client,
            IEnumerable<string> oids,
            int batchSize = 20,
            CancellationToken cancellationToken = default)
        {
            var oidList = oids.ToList();
            var results = new List<IVarBind>();

            for (int i = 0; i < oidList.Count; i += batchSize)
            {
                var batch = oidList.Skip(i).Take(batchSize);
                var batchResults = await client.GetAsync(batch, cancellationToken);
                results.AddRange(batchResults);
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Performs a parallel bulk GET operation on multiple OIDs
        /// </summary>
        /// <param name="client">SNMP client</param>
        /// <param name="oids">OIDs to retrieve</param>
        /// <param name="maxConcurrency">Maximum number of concurrent operations</param>
        /// <param name="batchSize">Number of OIDs per batch</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All variable bindings</returns>
        public static async Task<IReadOnlyList<IVarBind>> GetParallelAsync(
            this ISnmpClient client,
            IEnumerable<string> oids,
            int maxConcurrency = 5,
            int batchSize = 10,
            CancellationToken cancellationToken = default)
        {
            var oidList = oids.ToList();
            var batches = new List<List<string>>();

            for (int i = 0; i < oidList.Count; i += batchSize)
            {
                batches.Add(oidList.Skip(i).Take(batchSize).ToList());
            }

            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await client.GetAsync(batch, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList().AsReadOnly();
        }

        /// <summary>
        /// Walks multiple tables in parallel
        /// </summary>
        /// <param name="client">SNMP client</param>
        /// <param name="tableOids">Base OIDs of tables to walk</param>
        /// <param name="maxConcurrency">Maximum number of concurrent walks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All variable bindings from all tables</returns>
        public static async Task<IReadOnlyList<IVarBind>> WalkTablesAsync(
            this ISnmpClient client,
            IEnumerable<string> tableOids,
            int maxConcurrency = 3,
            CancellationToken cancellationToken = default)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = tableOids.Select(async tableOid =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var results = new List<IVarBind>();
                    await foreach (var varBind in client.WalkAsync(tableOid, cancellationToken))
                    {
                        results.Add(varBind);
                    }
                    return results;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList().AsReadOnly();
        }

        /// <summary>
        /// Performs a chunked table walk with specified chunk size
        /// </summary>
        /// <param name="client">SNMP client</param>
        /// <param name="tableOid">Base OID of the table</param>
        /// <param name="chunkSize">Number of entries to retrieve per chunk</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Variable bindings in chunks</returns>
        public static async IAsyncEnumerable<IReadOnlyList<IVarBind>> WalkChunkedAsync(
            this ISnmpClient client,
            string tableOid,
            int chunkSize = 50,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var chunk = new List<IVarBind>();

            await foreach (var varBind in client.WalkAsync(tableOid, cancellationToken))
            {
                chunk.Add(varBind);

                if (chunk.Count >= chunkSize)
                {
                    yield return chunk.AsReadOnly();
                    chunk.Clear();
                }
            }

            if (chunk.Count > 0)
            {
                yield return chunk.AsReadOnly();
            }
        }
    }
}