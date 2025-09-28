using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;
using nSNMP.Manager;

namespace nSNMP.Extensions
{
    /// <summary>
    /// Fluent API for SNMP client configuration and operations
    /// </summary>
    public class FluentSnmpClient : IDisposable
    {
        private readonly SnmpClientBuilder _builder;
        private ISnmpClient? _client;

        internal FluentSnmpClient(SnmpClientBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Gets the underlying SNMP client
        /// </summary>
        public ISnmpClient Client => _client ?? throw new InvalidOperationException("Client not built yet. Call Build() first.");

        /// <summary>
        /// Builds the SNMP client with the configured settings
        /// </summary>
        public FluentSnmpClient Build()
        {
            _client = _builder.Build();
            return this;
        }

        /// <summary>
        /// Performs a GET operation with fluent syntax
        /// </summary>
        public FluentGetOperation Get(params string[] oids) => new(Client, oids);

        /// <summary>
        /// Performs a GET-NEXT operation with fluent syntax
        /// </summary>
        public FluentGetNextOperation GetNext(params string[] oids) => new(Client, oids);

        /// <summary>
        /// Performs a GET-BULK operation with fluent syntax
        /// </summary>
        public FluentGetBulkOperation GetBulk() => new(Client);

        /// <summary>
        /// Performs a SET operation with fluent syntax
        /// </summary>
        public FluentSetOperation Set() => new(Client);

        /// <summary>
        /// Performs a table walk operation with fluent syntax
        /// </summary>
        public FluentWalkOperation Walk(string tableOid) => new(Client, tableOid);

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Builder for creating SNMP clients with fluent configuration
    /// </summary>
    public class SnmpClientBuilder
    {
        private IPEndPoint? _endpoint;
        private SnmpVersion _version = SnmpVersion.V2c;
        private string _community = "public";
        private ISnmpLogger? _logger;
        private TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private int _retries = 3;

        /// <summary>
        /// Sets the target endpoint
        /// </summary>
        public SnmpClientBuilder Target(string host, int port = 161)
        {
            _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            return this;
        }

        /// <summary>
        /// Sets the target endpoint
        /// </summary>
        public SnmpClientBuilder Target(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
            return this;
        }

        /// <summary>
        /// Sets the SNMP version
        /// </summary>
        public SnmpClientBuilder Version(SnmpVersion version)
        {
            _version = version;
            return this;
        }

        /// <summary>
        /// Sets the community string for v1/v2c
        /// </summary>
        public SnmpClientBuilder Community(string community)
        {
            _community = community;
            return this;
        }

        /// <summary>
        /// Sets the logger
        /// </summary>
        public SnmpClientBuilder WithLogger(ISnmpLogger logger)
        {
            _logger = logger;
            return this;
        }

        /// <summary>
        /// Sets the operation timeout
        /// </summary>
        public SnmpClientBuilder Timeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the number of retries
        /// </summary>
        public SnmpClientBuilder Retries(int retries)
        {
            _retries = retries;
            return this;
        }

        /// <summary>
        /// Builds the SNMP client
        /// </summary>
        internal ISnmpClient Build()
        {
            if (_endpoint == null)
                throw new InvalidOperationException("Target endpoint must be specified");

            // TODO: This will be completed when ISnmpClient implementation is finalized
            // The current SnmpClient class doesn't implement ISnmpClient interface yet
            throw new NotImplementedException("ISnmpClient implementation is not yet available. This will be completed in future iterations of the refactoring.");

            /*
            // Create the actual SNMP client using the Manager.SnmpClient
            var snmpVersion = _version switch
            {
                SnmpVersion.V1 => nSNMP.Message.SnmpVersion.V1,
                SnmpVersion.V2c => nSNMP.Message.SnmpVersion.V2c,
                _ => nSNMP.Message.SnmpVersion.V2c
            };

            return new nSNMP.Manager.SnmpClient(_endpoint, snmpVersion, _community ?? "public", _timeout);
            */
        }
    }

    /// <summary>
    /// Fluent operation for GET requests
    /// </summary>
    public class FluentGetOperation
    {
        private readonly ISnmpClient _client;
        private readonly string[] _oids;
        private CancellationToken _cancellationToken = default;

        internal FluentGetOperation(ISnmpClient client, string[] oids)
        {
            _client = client;
            _oids = oids;
        }

        /// <summary>
        /// Sets the cancellation token
        /// </summary>
        public FluentGetOperation WithCancellation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Executes the GET operation
        /// </summary>
        public Task<IReadOnlyList<IVarBind>> ExecuteAsync() => _client.GetAsync(_oids, _cancellationToken);
    }

    /// <summary>
    /// Fluent operation for GET-NEXT requests
    /// </summary>
    public class FluentGetNextOperation
    {
        private readonly ISnmpClient _client;
        private readonly string[] _oids;
        private CancellationToken _cancellationToken = default;

        internal FluentGetNextOperation(ISnmpClient client, string[] oids)
        {
            _client = client;
            _oids = oids;
        }

        /// <summary>
        /// Sets the cancellation token
        /// </summary>
        public FluentGetNextOperation WithCancellation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Executes the GET-NEXT operation
        /// </summary>
        public Task<IReadOnlyList<IVarBind>> ExecuteAsync() => _client.GetNextAsync(_oids, _cancellationToken);
    }

    /// <summary>
    /// Fluent operation for GET-BULK requests
    /// </summary>
    public class FluentGetBulkOperation
    {
        private readonly ISnmpClient _client;
        private int _nonRepeaters;
        private int _maxRepetitions = 10;
        private string[] _oids = Array.Empty<string>();
        private CancellationToken _cancellationToken = default;

        internal FluentGetBulkOperation(ISnmpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Sets the number of non-repeating variables
        /// </summary>
        public FluentGetBulkOperation NonRepeaters(int nonRepeaters)
        {
            _nonRepeaters = nonRepeaters;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of repetitions
        /// </summary>
        public FluentGetBulkOperation MaxRepetitions(int maxRepetitions)
        {
            _maxRepetitions = maxRepetitions;
            return this;
        }

        /// <summary>
        /// Sets the OIDs to retrieve
        /// </summary>
        public FluentGetBulkOperation Oids(params string[] oids)
        {
            _oids = oids;
            return this;
        }

        /// <summary>
        /// Sets the cancellation token
        /// </summary>
        public FluentGetBulkOperation WithCancellation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Executes the GET-BULK operation
        /// </summary>
        public Task<IReadOnlyList<IVarBind>> ExecuteAsync() =>
            _client.GetBulkAsync(_nonRepeaters, _maxRepetitions, _oids, _cancellationToken);
    }

    /// <summary>
    /// Fluent operation for SET requests
    /// </summary>
    public class FluentSetOperation
    {
        private readonly ISnmpClient _client;
        private readonly List<IVarBind> _varBinds = new();
        private CancellationToken _cancellationToken = default;

        internal FluentSetOperation(ISnmpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Adds a variable binding to set
        /// </summary>
        public FluentSetOperation Add(string oid, IDataType value)
        {
            _varBinds.Add(IVarBind.Create(oid, value));
            return this;
        }

        /// <summary>
        /// Adds a variable binding to set
        /// </summary>
        public FluentSetOperation Add(IVarBind varBind)
        {
            _varBinds.Add(varBind);
            return this;
        }

        /// <summary>
        /// Sets the cancellation token
        /// </summary>
        public FluentSetOperation WithCancellation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Executes the SET operation
        /// </summary>
        public Task<IReadOnlyList<IVarBind>> ExecuteAsync() => _client.SetAsync(_varBinds, _cancellationToken);
    }

    /// <summary>
    /// Fluent operation for table walk requests
    /// </summary>
    public class FluentWalkOperation
    {
        private readonly ISnmpClient _client;
        private readonly string _tableOid;
        private CancellationToken _cancellationToken = default;

        internal FluentWalkOperation(ISnmpClient client, string tableOid)
        {
            _client = client;
            _tableOid = tableOid;
        }

        /// <summary>
        /// Sets the cancellation token
        /// </summary>
        public FluentWalkOperation WithCancellation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        /// <summary>
        /// Executes the walk operation
        /// </summary>
        public IAsyncEnumerable<IVarBind> ExecuteAsync() => _client.WalkAsync(_tableOid, _cancellationToken);

        /// <summary>
        /// Executes the walk operation and returns all results as a list
        /// </summary>
        public async Task<List<IVarBind>> ToListAsync()
        {
            var results = new List<IVarBind>();
            await foreach (var varBind in ExecuteAsync())
            {
                results.Add(varBind);
            }
            return results;
        }
    }

    /// <summary>
    /// Static factory for creating fluent SNMP clients
    /// </summary>
    public static class SnmpClient
    {
        /// <summary>
        /// Creates a new fluent SNMP client builder
        /// </summary>
        public static SnmpClientBuilder Create() => new();

        /// <summary>
        /// Creates a fluent SNMP client with community authentication
        /// </summary>
        public static FluentSnmpClient CreateCommunity(string host, string community = "public", int port = 161)
        {
            return new FluentSnmpClient(
                Create()
                    .Target(host, port)
                    .Version(SnmpVersion.V2c)
                    .Community(community)
            );
        }

        /// <summary>
        /// Creates a fluent SNMP client with community authentication
        /// </summary>
        public static FluentSnmpClient CreateCommunity(IPEndPoint endpoint, string community = "public")
        {
            return new FluentSnmpClient(
                Create()
                    .Target(endpoint)
                    .Version(SnmpVersion.V2c)
                    .Community(community)
            );
        }
    }
}