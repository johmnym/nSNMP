using System.Net;
using nSNMP.Message;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;

namespace nSNMP.Manager
{
    /// <summary>
    /// SNMP client for performing v1/v2c operations against SNMP agents
    /// </summary>
    public class SnmpClient : IDisposable
    {
        private readonly IUdpChannel _transport;
        private readonly IPEndPoint _endpoint;
        private readonly SnmpVersion _version;
        private readonly string _community;
        private readonly TimeSpan _timeout;
        private int _requestId;
        private bool _disposed;

        public SnmpClient(IPEndPoint endpoint, SnmpVersion version = SnmpVersion.V2c, string community = "public", TimeSpan? timeout = null, IUdpChannel? transport = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _version = version;
            _community = community ?? throw new ArgumentNullException(nameof(community));
            _timeout = timeout ?? TimeSpan.FromSeconds(5);
            _transport = transport ?? new UdpChannel();
        }

        /// <summary>
        /// Factory method for creating SNMP v1/v2c clients with community string
        /// </summary>
        public static SnmpClient CreateCommunity(string host, int port = 161, SnmpVersion version = SnmpVersion.V2c, string community = "public", TimeSpan? timeout = null)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            return new SnmpClient(endpoint, version, community, timeout);
        }

        /// <summary>
        /// Performs SNMP GET operation for multiple OIDs
        /// </summary>
        public async Task<VarBind[]> GetAsync(params string[] oids)
        {
            return await GetAsync(oids.Select(ObjectIdentifier.Create).ToArray());
        }

        /// <summary>
        /// Performs SNMP GET operation for multiple OIDs
        /// </summary>
        public async Task<VarBind[]> GetAsync(params ObjectIdentifier[] oids)
        {
            if (oids == null || oids.Length == 0)
                throw new ArgumentException("At least one OID must be specified", nameof(oids));

            var varbinds = oids.Select(oid => new Sequence(new IDataType[] { oid, new Null() })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new GetRequest(null, Integer.Create(GetNextRequestId()), Integer.Create(0), Integer.Create(0), varbindList);
            var response = await SendRequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Performs SNMP GETNEXT operation for multiple OIDs
        /// </summary>
        public async Task<VarBind[]> GetNextAsync(params string[] oids)
        {
            return await GetNextAsync(oids.Select(ObjectIdentifier.Create).ToArray());
        }

        /// <summary>
        /// Performs SNMP GETNEXT operation for multiple OIDs
        /// </summary>
        public async Task<VarBind[]> GetNextAsync(params ObjectIdentifier[] oids)
        {
            if (oids == null || oids.Length == 0)
                throw new ArgumentException("At least one OID must be specified", nameof(oids));

            var varbinds = oids.Select(oid => new Sequence(new IDataType[] { oid, new Null() })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new GetNextRequest(null, Integer.Create(GetNextRequestId()), Integer.Create(0), Integer.Create(0), varbindList);
            var response = await SendRequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Performs SNMP SET operation for multiple variable bindings
        /// </summary>
        public async Task<VarBind[]> SetAsync(params VarBind[] varBinds)
        {
            if (varBinds == null || varBinds.Length == 0)
                throw new ArgumentException("At least one variable binding must be specified", nameof(varBinds));

            var varbinds = varBinds.Select(vb => new Sequence(new IDataType[] { vb.Oid, vb.Value })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new SetRequest(null, Integer.Create(GetNextRequestId()), Integer.Create(0), Integer.Create(0), varbindList);
            var response = await SendRequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Performs SNMP GETBULK operation (v2c/v3 only)
        /// </summary>
        public async Task<VarBind[]> GetBulkAsync(int nonRepeaters, int maxRepetitions, params string[] oids)
        {
            return await GetBulkAsync(nonRepeaters, maxRepetitions, oids.Select(ObjectIdentifier.Create).ToArray());
        }

        /// <summary>
        /// Performs SNMP GETBULK operation (v2c/v3 only)
        /// </summary>
        public async Task<VarBind[]> GetBulkAsync(int nonRepeaters, int maxRepetitions, params ObjectIdentifier[] oids)
        {
            if (_version == SnmpVersion.V1)
                throw new NotSupportedException("GetBulk is not supported in SNMP v1");

            if (oids == null || oids.Length == 0)
                throw new ArgumentException("At least one OID must be specified", nameof(oids));

            var varbinds = oids.Select(oid => new Sequence(new IDataType[] { oid, new Null() })).ToArray();
            var varbindList = new Sequence(varbinds);

            var request = new GetBulkRequest(null, Integer.Create(GetNextRequestId()), Integer.Create(nonRepeaters), Integer.Create(maxRepetitions), varbindList);
            var response = await SendRequestAsync(request);

            return ParseVarBinds(response);
        }

        /// <summary>
        /// Performs SNMP walk starting from the specified OID
        /// </summary>
        public async IAsyncEnumerable<VarBind> WalkAsync(string startOid)
        {
            await foreach (var varbind in WalkAsync(ObjectIdentifier.Create(startOid)))
            {
                yield return varbind;
            }
        }

        /// <summary>
        /// Performs SNMP walk starting from the specified OID
        /// </summary>
        public async IAsyncEnumerable<VarBind> WalkAsync(ObjectIdentifier startOid)
        {
            var currentOid = startOid;

            while (true)
            {
                var results = await GetNextAsync(currentOid);

                if (results.Length == 0)
                    break;

                var result = results[0];

                // Check if we've moved past the original subtree
                if (!result.Oid.StartsWith(startOid))
                    break;

                // Check for end-of-MIB-view
                if (result.IsEndOfMibView)
                    break;

                yield return result;
                currentOid = result.Oid;
            }
        }

        private async Task<GetResponse> SendRequestAsync(PDU request)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SnmpClient));

            // Create SNMP message
            var message = new SnmpMessage(_version, OctetString.Create(_community), request);
            var requestData = message.ToBytes();

            try
            {
                // Send request and receive response
                var responseData = await _transport.SendReceiveAsync(requestData, _endpoint, _timeout);

                // Parse response
                var responseMessage = SnmpMessage.Create(responseData);

                if (responseMessage.PDU is not GetResponse response)
                    throw new SnmpException($"Expected GetResponse, received {responseMessage.PDU?.GetType().Name}");

                // Check for SNMP errors
                if (response.Error?.Value != 0)
                {
                    throw SnmpErrorException.FromErrorStatus(response.Error.Value, response.ErrorIndex?.Value ?? 0);
                }

                return response;
            }
            catch (TimeoutException ex)
            {
                throw new SnmpTimeoutException(_timeout);
            }
        }

        private VarBind[] ParseVarBinds(GetResponse response)
        {
            if (response.VarbindList?.Elements == null)
                return Array.Empty<VarBind>();

            var varBinds = new List<VarBind>();

            foreach (var element in response.VarbindList.Elements)
            {
                if (element is Sequence seq && seq.Elements?.Count >= 2)
                {
                    var oid = (ObjectIdentifier)seq.Elements[0];
                    var value = seq.Elements[1];
                    varBinds.Add(new VarBind(oid, value));
                }
            }

            return varBinds.ToArray();
        }

        private int GetNextRequestId()
        {
            return Interlocked.Increment(ref _requestId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _transport?.Dispose();
        }
    }
}