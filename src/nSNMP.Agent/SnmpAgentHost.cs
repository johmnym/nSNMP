using System.Collections.Concurrent;
using System.Net;
using nSNMP.Logging;
using nSNMP.Message;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;

namespace nSNMP.Agent
{
    /// <summary>
    /// SNMP Agent host that handles incoming requests
    /// </summary>
    public class SnmpAgentHost : IAsyncDisposable, IDisposable
    {
        private readonly IUdpListener _listener;
        protected readonly ConcurrentDictionary<ObjectIdentifier, IScalarProvider> _scalarProviders;
        protected readonly ConcurrentDictionary<ObjectIdentifier, ITableProvider> _tableProviders;
        private readonly string _readCommunity;
        private readonly string _writeCommunity;
        private readonly ISnmpLogger _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        public SnmpAgentHost(string readCommunity = "public", string writeCommunity = "private", IUdpListener? listener = null, ISnmpLogger? logger = null)
        {
            _readCommunity = readCommunity ?? throw new ArgumentNullException(nameof(readCommunity));
            _writeCommunity = writeCommunity ?? throw new ArgumentNullException(nameof(writeCommunity));
            _listener = listener ?? new UdpListener();
            _logger = logger ?? NullSnmpLogger.Instance;
            _scalarProviders = new ConcurrentDictionary<ObjectIdentifier, IScalarProvider>();
            _tableProviders = new ConcurrentDictionary<ObjectIdentifier, ITableProvider>();
        }

        /// <summary>
        /// Register a scalar provider for a specific OID
        /// </summary>
        public void RegisterScalarProvider(ObjectIdentifier oid, IScalarProvider provider)
        {
            _scalarProviders[oid] = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Register a table provider for a specific table OID
        /// </summary>
        public void RegisterTableProvider(ObjectIdentifier tableOid, ITableProvider provider)
        {
            _tableProviders[tableOid] = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Helper method to register a simple scalar value
        /// </summary>
        public void MapScalar(ObjectIdentifier oid, IDataType value, bool readOnly = true)
        {
            var provider = new SimpleScalarProvider(oid, value, readOnly);
            RegisterScalarProvider(oid, provider);
        }

        /// <summary>
        /// Helper method to register a simple scalar value with string OID
        /// </summary>
        public void MapScalar(string oid, IDataType value, bool readOnly = true)
        {
            MapScalar(ObjectIdentifier.Create(oid), value, readOnly);
        }

        /// <summary>
        /// Start the SNMP agent on the specified port
        /// </summary>
        public async Task StartAsync(int port = 161, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SnmpAgentHost));

            if (_listenerTask != null)
                throw new InvalidOperationException("Agent is already running");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _listenerTask = Task.Run(async () =>
            {
                await foreach (var request in _listener.ListenAsync(port, _cancellationTokenSource.Token))
                {
                    // Process request in background to avoid blocking the listener
                    _ = ProcessRequestAsync(request);
                }
            }, _cancellationTokenSource.Token);

            await Task.Delay(100, cancellationToken); // Give listener time to start
        }

        /// <summary>
        /// Stop the SNMP agent
        /// </summary>
        public async Task StopAsync()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _listenerTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Process an incoming SNMP request
        /// </summary>
        protected virtual async Task ProcessRequestAsync(UdpRequest request)
        {
            try
            {
                // Parse the incoming SNMP message
                var message = SnmpMessage.Create(request.Data);

                if (message == null || message.PDU == null)
                    return; // Invalid message, ignore

                // Validate community string
                if (!ValidateCommunity(message, out bool isWrite))
                    return; // Invalid community, ignore

                // Process the PDU and generate response
                var responsePdu = await ProcessPduAsync(message.PDU, isWrite);

                if (responsePdu == null)
                    return; // No response needed

                // Create response message
                var responseMessage = new SnmpMessage(message.Version, message.CommunityString, responsePdu);
                var responseData = responseMessage.ToBytes();

                // Send response
                await request.SendResponseAsync(responseData);
            }
            catch (ArgumentException ex)
            {
                // Expected for malformed packets
                _logger.LogError("ProcessRequest", ex, "Malformed packet received");
            }
            catch (InvalidOperationException ex)
            {
                // Expected for protocol violations
                _logger.LogError("ProcessRequest", ex, "Protocol violation");
            }
            catch (Exception ex)
            {
                // Unexpected errors should be logged for debugging
                _logger.LogError("ProcessRequest", ex, "Unexpected error processing SNMP request");
            }
        }

        /// <summary>
        /// Validate community string and determine access level
        /// </summary>
        private bool ValidateCommunity(SnmpMessage message, out bool isWrite)
        {
            isWrite = false;

            if (message.CommunityString?.Value == _readCommunity)
            {
                return true;
            }

            if (message.CommunityString?.Value == _writeCommunity)
            {
                isWrite = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Process a PDU and generate appropriate response
        /// </summary>
        protected virtual async Task<PDU?> ProcessPduAsync(PDU requestPdu, bool hasWriteAccess)
        {
            return requestPdu switch
            {
                GetRequest getRequest => await ProcessGetRequestAsync(getRequest),
                GetNextRequest getNextRequest => await ProcessGetNextRequestAsync(getNextRequest),
                SetRequest setRequest when hasWriteAccess => await ProcessSetRequestAsync(setRequest),
                SetRequest => CreateErrorResponse(requestPdu, SnmpErrorStatus.NoAccess, 0),
                GetBulkRequest getBulkRequest => await ProcessGetBulkRequestAsync(getBulkRequest),
                _ => null // Unsupported PDU type
            };
        }

        /// <summary>
        /// Process GET request
        /// </summary>
        private Task<PDU> ProcessGetRequestAsync(GetRequest request)
        {
            var responseVarBinds = new List<IDataType>();

            if (request.VarbindList?.Elements != null)
            {
                foreach (var varbind in request.VarbindList.Elements)
                {
                    if (varbind is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = (ObjectIdentifier)seq.Elements[0];
                        var value = GetValue(oid) ?? new NoSuchObject();
                        responseVarBinds.Add(new Sequence(new IDataType[] { oid, value }));
                    }
                }
            }

            return Task.FromResult(new GetResponse(
                null, // Data
                request.RequestId,
                Integer.Create(0), // No error
                Integer.Create(0), // No error index
                new Sequence(responseVarBinds)
            ) as PDU);
        }

        /// <summary>
        /// Process GET-NEXT request
        /// </summary>
        private Task<PDU> ProcessGetNextRequestAsync(GetNextRequest request)
        {
            var responseVarBinds = new List<IDataType>();

            if (request.VarbindList?.Elements != null)
            {
                foreach (var varbind in request.VarbindList.Elements)
                {
                    if (varbind is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = (ObjectIdentifier)seq.Elements[0];
                        var nextOid = GetNextOid(oid);

                        if (nextOid != null)
                        {
                            var value = GetValue(nextOid) ?? new NoSuchObject();
                            responseVarBinds.Add(new Sequence(new IDataType[] { nextOid, value }));
                        }
                        else
                        {
                            responseVarBinds.Add(new Sequence(new IDataType[] { oid, new EndOfMibView() }));
                        }
                    }
                }
            }

            return Task.FromResult(new GetResponse(
                null, // Data
                request.RequestId,
                Integer.Create(0), // No error
                Integer.Create(0), // No error index
                new Sequence(responseVarBinds)
            ) as PDU);
        }

        /// <summary>
        /// Process SET request
        /// </summary>
        private Task<PDU> ProcessSetRequestAsync(SetRequest request)
        {
            var responseVarBinds = new List<IDataType>();
            var errorIndex = 0;
            var errorStatus = SnmpErrorStatus.NoError;

            if (request.VarbindList?.Elements != null)
            {
                for (int i = 0; i < request.VarbindList.Elements.Count; i++)
                {
                    var varbind = request.VarbindList.Elements[i];
                    if (varbind is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = (ObjectIdentifier)seq.Elements[0];
                        var value = seq.Elements[1];

                        if (!SetValue(oid, value))
                        {
                            errorStatus = SnmpErrorStatus.NotWritable;
                            errorIndex = i + 1;
                            break;
                        }

                        responseVarBinds.Add(new Sequence(new IDataType[] { oid, value }));
                    }
                }
            }

            return Task.FromResult(new GetResponse(
                null, // Data
                request.RequestId,
                Integer.Create((int)errorStatus),
                Integer.Create(errorIndex),
                new Sequence(responseVarBinds)
            ) as PDU);
        }

        /// <summary>
        /// Process GET-BULK request (v2c/v3 only)
        /// </summary>
        private Task<PDU> ProcessGetBulkRequestAsync(GetBulkRequest request)
        {
            var responseVarBinds = new List<IDataType>();

            // For now, implement a simple version that treats it like GetNext
            if (request.VarbindList?.Elements != null)
            {
                foreach (var varbind in request.VarbindList.Elements)
                {
                    if (varbind is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = (ObjectIdentifier)seq.Elements[0];
                        var nextOid = GetNextOid(oid);

                        if (nextOid != null)
                        {
                            var value = GetValue(nextOid) ?? new NoSuchObject();
                            responseVarBinds.Add(new Sequence(new IDataType[] { nextOid, value }));
                        }
                        else
                        {
                            responseVarBinds.Add(new Sequence(new IDataType[] { oid, new EndOfMibView() }));
                        }
                    }
                }
            }

            return Task.FromResult(new GetResponse(
                null, // Data
                request.RequestId,
                Integer.Create(0), // No error
                Integer.Create(0), // No error index
                new Sequence(responseVarBinds)
            ) as PDU);
        }

        /// <summary>
        /// Get value for an OID from registered providers
        /// </summary>
        private IDataType? GetValue(ObjectIdentifier oid)
        {
            // Check scalar providers first
            foreach (var provider in _scalarProviders.Values)
            {
                if (provider.CanHandle(oid))
                {
                    return provider.GetValue(oid);
                }
            }

            // Check table providers
            foreach (var provider in _tableProviders.Values)
            {
                if (provider.CanHandle(oid))
                {
                    return provider.GetValue(oid);
                }
            }

            return null;
        }

        /// <summary>
        /// Get next OID in lexicographic order
        /// </summary>
        private ObjectIdentifier? GetNextOid(ObjectIdentifier oid)
        {
            var candidateOids = new List<ObjectIdentifier>();

            // Get candidates from scalar providers
            foreach (var provider in _scalarProviders.Values)
            {
                var nextOid = provider.GetNextOid(oid);
                if (nextOid != null)
                    candidateOids.Add(nextOid);
            }

            // Get candidates from table providers
            foreach (var provider in _tableProviders.Values)
            {
                var nextOid = provider.GetNextOid(oid);
                if (nextOid != null)
                    candidateOids.Add(nextOid);
            }

            // Return the lexicographically smallest candidate
            return candidateOids.OrderBy(o => o).FirstOrDefault();
        }

        /// <summary>
        /// Set value for an OID using registered providers
        /// </summary>
        private bool SetValue(ObjectIdentifier oid, IDataType value)
        {
            // Check scalar providers first
            foreach (var provider in _scalarProviders.Values)
            {
                if (provider.CanHandle(oid))
                {
                    return provider.SetValue(oid, value);
                }
            }

            // Check table providers
            foreach (var provider in _tableProviders.Values)
            {
                if (provider.CanHandle(oid))
                {
                    return provider.SetValue(oid, value);
                }
            }

            return false;
        }

        /// <summary>
        /// Create error response PDU
        /// </summary>
        private PDU CreateErrorResponse(PDU originalPdu, SnmpErrorStatus errorStatus, int errorIndex)
        {
            return new GetResponse(
                null, // Data
                originalPdu.RequestId,
                Integer.Create((int)errorStatus),
                Integer.Create(errorIndex),
                originalPdu.VarbindList ?? new Sequence(Array.Empty<IDataType>())
            );
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors during disposal
            }

            _listener?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // For synchronous dispose, cancel and dispose without waiting
                _cancellationTokenSource?.Cancel();
                _listener?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// SNMP error status codes
    /// </summary>
    public enum SnmpErrorStatus
    {
        NoError = 0,
        TooBig = 1,
        NoSuchName = 2,
        BadValue = 3,
        ReadOnly = 4,
        GenErr = 5,
        NoAccess = 6,
        WrongType = 7,
        WrongLength = 8,
        WrongEncoding = 9,
        WrongValue = 10,
        NoCreation = 11,
        InconsistentValue = 12,
        ResourceUnavailable = 13,
        CommitFailed = 14,
        UndoFailed = 15,
        AuthorizationError = 16,
        NotWritable = 17,
        InconsistentName = 18
    }
}