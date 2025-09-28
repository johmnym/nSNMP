using System.Net;
using System.Runtime.CompilerServices;
using nSNMP.Core;
using nSNMP.Message;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;

namespace nSNMP.Agent
{
    /// <summary>
    /// SNMP Trap receiver for listening to trap and notification messages
    /// </summary>
    public class TrapReceiver : IAsyncDisposable, IDisposable
    {
        private readonly IUdpListener _listener;
        private readonly List<ITrapHandler> _handlers;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        public TrapReceiver(IUdpListener? listener = null)
        {
            _listener = listener ?? new UdpListener();
            _handlers = new List<ITrapHandler>();
        }

        /// <summary>
        /// Register a trap handler
        /// </summary>
        public void RegisterHandler(ITrapHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.Add(handler);
        }

        /// <summary>
        /// Start listening for traps on the specified port (default 162)
        /// </summary>
        public async Task StartAsync(int port = 162, CancellationToken cancellationToken = default)
        {
            if (_listenerTask != null)
                throw new InvalidOperationException("Trap receiver is already running");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenerTask = ListenAsync(port, _cancellationTokenSource.Token);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stop listening for traps
        /// </summary>
        public async Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();
            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when canceling
                }
            }
            _listenerTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Listen for traps asynchronously
        /// </summary>
        public async IAsyncEnumerable<TrapInfo> ListenTrapsAsync(
            int port = 162,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var request in _listener.ListenAsync(port, cancellationToken))
            {
                TrapInfo? trapInfo = null;

                trapInfo = ParseTrap(request);
                if (trapInfo != null)
                {
                    // Process through handlers
                    foreach (var handler in _handlers)
                    {
                        try
                        {
                            await handler.HandleTrapAsync(trapInfo);
                        }
                        catch
                        {
                            // Handler errors shouldn't stop processing
                        }
                    }

                    // If this is an INFORM, send response
                    if (trapInfo.Type == TrapType.InformRequest)
                    {
                        await SendInformResponseAsync(request, trapInfo);
                    }

                    yield return trapInfo;
                }
            }
        }

        /// <summary>
        /// Internal listener loop
        /// </summary>
        private async Task ListenAsync(int port, CancellationToken cancellationToken)
        {
            await foreach (var trapInfo in ListenTrapsAsync(port, cancellationToken))
            {
                // This loop runs the enumerable to process traps through handlers
                // The actual trap data is yielded to any external consumers
            }
        }

        /// <summary>
        /// Parse incoming trap message
        /// </summary>
        private TrapInfo? ParseTrap(UdpRequest request)
        {
            try
            {
                var message = SnmpMessage.Create(request.Data);
                if (message == null)
                    return null;

                var source = request.RemoteEndPoint;
                var version = message.Version ?? SnmpVersion.V2c;
                var community = message.CommunityString?.Value ?? "";

                // Handle different trap types
                // For V1, the message contains a TrapV1 as the third element, not a PDU
                // We need to check the raw data or the version
                if (version == SnmpVersion.V1)
                {
                    // Re-parse as V1 trap
                    return ParseV1TrapMessage(request.Data, source);
                }
                else if (message.PDU is TrapV2 trapV2)
                {
                    return ParseV2Trap(trapV2, source, community, version);
                }
                else if (message.PDU is InformRequest inform)
                {
                    return ParseInform(inform, source, community, version);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse SNMPv1 trap message
        /// </summary>
        private TrapInfo? ParseV1TrapMessage(byte[] data, IPEndPoint source)
        {
            try
            {
                var sequence = (Sequence)nSNMP.SMI.SMIDataFactory.Create(data);
                if (sequence.Elements?.Count >= 3)
                {
                    var version = (Integer)sequence.Elements[0];
                    var community = (OctetString)sequence.Elements[1];
                    var trap = sequence.Elements[2] as TrapV1;

                    if (trap != null && version.Value == 0) // Version 0 is SNMPv1
                    {
                        return ParseV1Trap(trap, source, community.Value);
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return null;
        }

        /// <summary>
        /// Parse SNMPv1 trap
        /// </summary>
        private TrapInfo ParseV1Trap(TrapV1 trap, IPEndPoint source, string community)
        {
            var varbinds = ParseVarbinds(trap.VarbindList);
            var uptime = trap.TimeStamp != null ? TimeSpan.FromMilliseconds(trap.TimeStamp.Value * 10) : TimeSpan.Zero;

            return new TrapInfo(
                TrapType.TrapV1,
                source,
                SnmpVersion.V1,
                community,
                trap.Enterprise,
                trap.AgentAddr?.ToString(),
                trap.GenericTrap?.Value ?? 0,
                trap.SpecificTrap?.Value ?? 0,
                null, // No trap OID in v1
                uptime,
                varbinds,
                0 // V1 traps don't have request ID
            );
        }

        /// <summary>
        /// Parse SNMPv2c/v3 trap
        /// </summary>
        private TrapInfo ParseV2Trap(TrapV2 trap, IPEndPoint source, string community, SnmpVersion version)
        {
            var varbinds = ParseVarbinds(trap.VarbindList);

            // Extract standard trap varbinds
            TimeSpan uptime = TimeSpan.Zero;
            ObjectIdentifier? trapOid = null;

            // Look for sysUpTime and snmpTrapOID
            foreach (var vb in varbinds)
            {
                var oidStr = vb.Oid.ToString();
                if (oidStr == ".1.3.6.1.2.1.1.3.0" && vb.Value is TimeTicks ticks)
                {
                    uptime = TimeSpan.FromMilliseconds(ticks.Value * 10);
                }
                else if (oidStr == ".1.3.6.1.6.3.1.1.4.1.0" && vb.Value is ObjectIdentifier oid)
                {
                    trapOid = oid;
                }
            }

            return new TrapInfo(
                TrapType.TrapV2,
                source,
                version,
                community,
                null, // No enterprise in v2
                null, // No agent address in v2
                0, // No generic trap in v2
                0, // No specific trap in v2
                trapOid,
                uptime,
                varbinds,
                trap.RequestId?.Value ?? 0
            );
        }

        /// <summary>
        /// Parse INFORM request
        /// </summary>
        private TrapInfo ParseInform(InformRequest inform, IPEndPoint source, string community, SnmpVersion version)
        {
            var varbinds = ParseVarbinds(inform.VarbindList);

            // Extract standard trap varbinds (same as v2 trap)
            TimeSpan uptime = TimeSpan.Zero;
            ObjectIdentifier? trapOid = null;

            foreach (var vb in varbinds)
            {
                var oidStr = vb.Oid.ToString();
                if (oidStr == ".1.3.6.1.2.1.1.3.0" && vb.Value is TimeTicks ticks)
                {
                    uptime = TimeSpan.FromMilliseconds(ticks.Value * 10);
                }
                else if (oidStr == ".1.3.6.1.6.3.1.1.4.1.0" && vb.Value is ObjectIdentifier oid)
                {
                    trapOid = oid;
                }
            }

            return new TrapInfo(
                TrapType.InformRequest,
                source,
                version,
                community,
                null, // No enterprise in inform
                null, // No agent address in inform
                0, // No generic trap in inform
                0, // No specific trap in inform
                trapOid,
                uptime,
                varbinds,
                inform.RequestId?.Value ?? 0
            );
        }

        /// <summary>
        /// Parse varbind list from trap
        /// </summary>
        private List<VarBind> ParseVarbinds(Sequence? varbindList)
        {
            var varbinds = new List<VarBind>();

            if (varbindList?.Elements != null)
            {
                foreach (var element in varbindList.Elements)
                {
                    if (element is Sequence seq && seq.Elements?.Count >= 2)
                    {
                        var oid = seq.Elements[0] as ObjectIdentifier;
                        var value = seq.Elements[1];
                        if (oid != null && value != null)
                        {
                            varbinds.Add(new VarBind(oid, value));
                        }
                    }
                }
            }

            return varbinds;
        }

        /// <summary>
        /// Send response to INFORM request
        /// </summary>
        private async Task SendInformResponseAsync(UdpRequest request, TrapInfo trapInfo)
        {
            // Create response PDU
            var response = new GetResponse(
                null,
                Integer.Create(trapInfo.RequestId),
                Integer.Create(0), // No error
                Integer.Create(0), // No error index
                new Sequence(Array.Empty<IDataType>()) // Empty varbind list
            );

            // Create response message
            var message = new SnmpMessage(
                trapInfo.Version,
                OctetString.Create(trapInfo.Community),
                response
            );

            var responseData = message.ToBytes();
            await request.SendResponseAsync(responseData);
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
            if (_disposed)
                return;

            // For synchronous dispose, cancel and dispose without waiting
            _cancellationTokenSource?.Cancel();
            _listener?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Information about a received trap
    /// </summary>
    public record TrapInfo(
        TrapType Type,
        IPEndPoint Source,
        SnmpVersion Version,
        string Community,
        ObjectIdentifier? Enterprise,
        string? AgentAddress,
        int GenericTrap,
        int SpecificTrap,
        ObjectIdentifier? TrapOid,
        TimeSpan Uptime,
        List<VarBind> VarBinds,
        int RequestId
    )
    {
        /// <summary>
        /// Get a user-friendly description of the trap
        /// </summary>
        public string GetDescription()
        {
            return Type switch
            {
                TrapType.TrapV1 => $"SNMPv1 Trap - Generic: {GenericTrap}, Specific: {SpecificTrap}",
                TrapType.TrapV2 => $"SNMPv2 Trap - OID: {TrapOid?.ToString() ?? "unknown"}",
                TrapType.InformRequest => $"INFORM Request - OID: {TrapOid?.ToString() ?? "unknown"}",
                _ => "Unknown trap type"
            };
        }
    }

    /// <summary>
    /// Trap message types
    /// </summary>
    public enum TrapType
    {
        TrapV1,
        TrapV2,
        InformRequest
    }

    /// <summary>
    /// Interface for trap handlers
    /// </summary>
    public interface ITrapHandler
    {
        Task HandleTrapAsync(TrapInfo trapInfo);
    }

    /// <summary>
    /// Simple trap handler that logs trap information
    /// </summary>
    public class LoggingTrapHandler : ITrapHandler
    {
        private readonly Action<string> _logAction;

        public LoggingTrapHandler(Action<string>? logAction = null)
        {
            _logAction = logAction ?? Console.WriteLine;
        }

        public Task HandleTrapAsync(TrapInfo trapInfo)
        {
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {trapInfo.GetDescription()} from {trapInfo.Source}";
            _logAction(message);

            foreach (var vb in trapInfo.VarBinds)
            {
                _logAction($"  {vb.Oid.ToString()} = {vb.Value}");
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Trap handler that filters by OID
    /// </summary>
    public class FilteringTrapHandler : ITrapHandler
    {
        private readonly string _oidPrefix;
        private readonly ITrapHandler _innerHandler;

        public FilteringTrapHandler(string oidPrefix, ITrapHandler innerHandler)
        {
            _oidPrefix = oidPrefix ?? throw new ArgumentNullException(nameof(oidPrefix));
            _innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
        }

        public async Task HandleTrapAsync(TrapInfo trapInfo)
        {
            // Check if trap OID matches the filter
            if (trapInfo.TrapOid != null && trapInfo.TrapOid.ToString().StartsWith(_oidPrefix))
            {
                await _innerHandler.HandleTrapAsync(trapInfo);
            }
            // For v1 traps, check enterprise OID
            else if (trapInfo.Enterprise != null && trapInfo.Enterprise.ToString().StartsWith(_oidPrefix))
            {
                await _innerHandler.HandleTrapAsync(trapInfo);
            }
        }
    }
}