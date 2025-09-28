nSNMP for .NET 9 — Architecture & Implementation Design

Status: Draft v0.1 • Target: C# / .NET 9 • Scope: Manager (v1/v2c/v3), Agent (v1/v2c/v3), Traps/Notifications, MIB parsing (SMIv2 subset), high‑performance BER codec

⸻

1) Goals & Non‑Goals

Goals
	•	Standards‑compliant SNMP v1, v2c, and v3 implementation for .NET 9.
	•	High performance & low allocations using Span<T>, Memory<T>, IBufferWriter<byte>, and Pipelines where appropriate.
	•	Ergonomic API for both Manager and Agent roles.
	•	SNMPv3 USM (auth/noAuth + priv/noPriv), with AES‑128 and SHA‑1/SHA‑2 families. DES/MD5 available for interop but discouraged by default.
	•	Trap/Inform send/receive.
	•	Walk / BulkWalk helpers.
	•	MIB subsystem: OID tree, runtime MIB loading (SMIv2 subset), scalar & table providers for Agent.
	•	Observability: structured logging (ILogger), OpenTelemetry traces/metrics.
	•	Resilience: timeouts, retries, jittered backoff, request correlation, duplicate/out‑of‑order handling.
	•	Cross‑platform (Windows, Linux, macOS), IPv4/IPv6, UDP primary; optional TCP.

Non‑Goals (initial)
	•	Full SMIv2 compiler with all macros; we support a pragmatic, frequently used subset first.
	•	Transport Security Model (TLS/DTLS/SSH). We design extension points so it can be added later.

⸻

2) Project Layout (proposed)

nSNMP.sln
  ├─ src/
  │   ├─ nSNMP.Abstractions/          # OIDs, SNMP data types, PDUs, common options & errors
  │   ├─ nSNMP.Codec/                 # BER/DER codec, OID encoder/decoder
  │   ├─ nSNMP.Transport/             # UDP (Sockets/Pipelines), TCP optional
  │   ├─ nSNMP.Manager/               # Client for Get/Set/GetBulk/Walk, Trap/Inform sender/receiver
  │   ├─ nSNMP.Agent/                 # Agent engine + providers, VACM (optional), USM
  │   ├─ nSNMP.Mib/                   # MIB loader (SMIv2 subset), OID tree, type mapping
  │   ├─ nSNMP.Instrumentation/       # OpenTelemetry exporters & meters
  │   └─ nSNMP.Tooling/               # MIB precompiler (optional), sample CLI
  ├─ tests/
  │   ├─ nSNMP.Tests/                 # Unit tests (codec, USM vectors, interop fixtures)
  │   ├─ nSNMP.IntegrationTests/      # Net‑SNMP/snmpsimd interop via Testcontainers
  │   └─ nSNMP.Fuzz/                  # SharpFuzz harness for codec
  └─ benchmarks/
      └─ nSNMP.Benchmarks/            # BenchmarkDotNet for codec & client


⸻

3) Protocol Overview & Scope
	•	PDUs: GetRequest, GetNextRequest, GetResponse, SetRequest, GetBulkRequest, InformRequest, SNMPv2‑Trap, Report (v3), plus Trap v1.
	•	Data types: INTEGER, Gauge32, Counter32/64, TimeTicks, IpAddress, Opaque, OctetString, Null, ObjectIdentifier, Sequence.
	•	Transports: UDP/161 for requests, UDP/162 for traps/notifications (default).
	•	SNMPv3 specifics: engine discovery, timeliness (engineBoots/engineTime), USM (auth/priv), Report PDUs.

⸻

4) Core Building Blocks

4.1 BER Codec (nSNMP.Codec)
	•	BerReader using ReadOnlySpan<byte> + ref struct; forward‑only, non‑allocating.
	•	BerWriter on IBufferWriter<byte> / ArrayBufferWriter<byte> or pooled buffers.
	•	Features: definite length only (indefinite can be rejected in SNMP context), primitive/constructed handling, universal/application/context tags.
	•	OID encoding: first two nodes 40*X + Y, base‑128 varints with continuation bit.
	•	Integer: two’s complement; 32/64 for counters; big‑endian.

4.2 SNMP Data Model (nSNMP.Abstractions)
	•	readonly record struct Oid(ReadOnlyMemory<uint> SubIds) with fast parse/format.
	•	abstract class SnmpData { public abstract SnmpSyntax Syntax {get;} }
	•	SnmpInteger, SnmpOctetString, SnmpNull, SnmpObjectId, SnmpIpAddress, SnmpCounter32, SnmpGauge32, SnmpTimeTicks, SnmpCounter64, SnmpOpaque.
	•	record VarBind(Oid Oid, SnmpData Value); VarBindList : IReadOnlyList<VarBind>.
	•	enum PduType { Get, GetNext, Response, Set, GetBulk, Inform, TrapV2, Report, TrapV1 }.
	•	record SnmpPdu(PduType Type, int RequestId, int ErrorStatus, int ErrorIndex, VarBindList VarBinds, BulkOptions? Bulk = null).
	•	Messages:
	•	v1/v2c: record SnmpMessageV2c(string Community, SnmpPdu Pdu).
	•	v3: record SnmpMessageV3(Header hdr, SecurityParameters sec, ScopedPdu scoped).

4.3 Transport (nSNMP.Transport)
	•	UDP with Socket/SocketAsyncEventArgs or System.IO.Pipelines for high‑throughput.
	•	IUdpChannel abstraction; pluggable for tests.
	•	Request multiplexer: match (remoteEndPoint, msgId, securityModel) to TaskCompletionSource.
	•	Retries & timeouts via policy (Polly recommended at higher layer).

4.4 SNMPv3 USM & Security (nSNMP.Agent + Manager)
	•	UserSecurityParameters: username, auth algo (SHA‑1/224/256/384/512), priv algo (AES‑128; DES optional), authKey/privKey localized per engine.
	•	Discovery: DiscoverAsync() to learn engineID, engineBoots, engineTime from authoritative engine via Report.
	•	Timeliness: window validation, auto resync on Report.
	•	HMAC using .NET HMACSHA256/HMACSHA1 etc.
	•	AES‑CFB128 for privacy (default). DES‑CFB64 behind a compat flag.

4.5 Agent Engine (nSNMP.Agent)
	•	Provider model: register scalars and tables under OIDs.
	•	IScalarProvider → returns SnmpData for exact OID.
	•	ITableProvider → supports lexicographic next OID and row retrieval.
	•	VACM (optional): views, groups, access rules (v3). Start with coarse allow/deny per community/user & context.
	•	Traps/Notifications: send v2c/v3 notifications; receive & dispatch to handlers.

4.6 MIB Subsystem (nSNMP.Mib)
	•	Lightweight SMIv2 parser (MODULE‑IDENTITY, OBJECT‑TYPE (scalar/table/row), TEXTUAL‑CONVENTION basic mapping).
	•	Build OID tree with metadata for display, type hints, and agent binding.
	•	Optional precompiler tool generating C# classes for hot paths.

4.7 Observability & Resilience
	•	ILogger<T> hooks everywhere (user can plug Serilog).
	•	ActivitySource & Meter for OpenTelemetry.
	•	Retry policies, jittered backoff at Manager API level; idempotent GET/GETBULK only.

⸻

5) Public API (ergonomic)

5.1 Manager

public sealed class SnmpClient : IAsyncDisposable
{
    public SnmpClient(SnmpClientOptions options, ILogger<SnmpClient>? logger = null);

    // v1/v2c quick ctor
    public static SnmpClient CreateCommunity(EndPoint target, string community = "public", Version version = Version.V2C, SnmpClientOptions? options = null);

    // v3 quick ctor
    public static SnmpClient CreateV3(EndPoint target, V3Credentials creds, SnmpClientOptions? options = null);

    ValueTask<SnmpResponse> GetAsync(params Oid[] oids);
    IAsyncEnumerable<VarBind> WalkAsync(Oid root, WalkOptions? options = null, CancellationToken ct = default);
    ValueTask<SnmpResponse> GetNextAsync(params Oid[] oids);
    ValueTask<SnmpResponse> GetBulkAsync(int nonRepeaters, int maxRepetitions, params Oid[] oids);
    ValueTask<SnmpResponse> SetAsync(params VarBind[] writes);

    // Traps
    Task SendTrapAsync(TrapBuilder trap, CancellationToken ct = default);

    // Trap receiver (port 162)
    IAsyncDisposable ListenTraps(Func<TrapMessage, Task> onTrap, TrapListenerOptions? opts = null);
}

Response model

public sealed record SnmpResponse(int RequestId, VarBindList VarBinds, int ErrorStatus = 0, int ErrorIndex = 0);

V3 credentials

public sealed record V3Credentials(
    string Username,
    AuthAlgorithm Auth = AuthAlgorithm.None,
    ReadOnlyMemory<byte> AuthKey = default, // raw master key or password -> localized internally
    PrivAlgorithm Priv = PrivAlgorithm.None,
    ReadOnlyMemory<byte> PrivKey = default,
    string ContextName = "",
    ReadOnlyMemory<byte> ContextEngineId = default);

5.2 Agent

public sealed class SnmpAgentHost : IAsyncDisposable
{
    public SnmpAgentHost(SnmpAgentOptions opts, ILogger<SnmpAgentHost>? logger = null);

    // Register scalar and table providers
    public void MapScalar(Oid oid, Func<SnmpData> getter, Action<SnmpData>? setter = null);
    public void MapTable(ITableProvider tableProvider);

    // Start/Stop listen
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}

Table provider contract

public interface ITableProvider
{
    Oid TableRoot { get; }
    bool TryGetExact(Oid oid, out VarBind vb);
    bool TryGetNext(Oid oid, out VarBind next);
    bool TrySet(VarBind write, out int errorStatus, out int errorIndex);
}


⸻

6) Encoding/Decoding (details)

6.1 BerReader / BerWriter
	•	BerReader: ReadTag(), ReadLength(), ReadInteger32(), ReadInteger64(), ReadOctetString(), ReadObjectId(), ReadSequence(in out BerReader child).
	•	BerWriter: WriteTag(), WriteLength(), WriteInteger32/64(), WriteOctetString(), WriteObjectId(), BeginSequence(out Token), EndSequence(Token) computing length backwards or using temp length slots.
	•	Prefer two‑pass for constructed types: write content to an inner ArrayBufferWriter<byte>, then outer sequence with definite length.

6.2 OID Encoding
	•	Combine first two sub‑ids: value = 40 * first + second.
	•	Each sub‑id encoded as base‑128 with MSB continuation bit.
	•	Keep a small stackalloc buffer for per‑subid encoding to minimize heap traffic.

6.3 SNMPv3 USM Notes
	•	Key localization: Kul = H( Ku || engineID || Ku ) for the chosen hash H.
	•	Auth: compute HMAC over the whole message with the authenticationParameters field zeroed, then insert the auth digest.
	•	Priv: encrypt scopedPDU with AES‑CFB128 using privacy parameters (salt/IV). Maintain per‑message salt monotonicity.
	•	Timeliness: cache engine boots/time; re‑discover when Report indicates notInTimeWindow.

⸻

7) Concurrency, Matching & Timeouts
	•	Each request assigns a unique request-id (int32) and stores a TaskCompletionSource<SnmpResponse> in a ConcurrentDictionary keyed by (remoteEP, msgFlags/securityModel, requestId).
	•	Datagram receiver loop demultiplexes to the matching TCS; duplicates complete the existing one once.
	•	Timeouts managed per request with CancellationTokenSource + cleanup.
	•	Optional retry policy only for idempotent operations (GET/GETNEXT/GETBULK). Never auto‑retry SET.

⸻

8) Error Handling Model
	•	Transport errors → SnmpTransportException.
	•	Decode/encode issues → SnmpCodecException.
	•	SNMP error‑status → surface on response (non‑throwing), unless ClientOptions.ThrowOnSnmpError.
	•	v3 Reports (e.g., usmStatsNotInTimeWindows, unknownEngineIDs) → handled internally with optional callback for telemetry.

⸻

9) Performance Plan
	•	Minimize allocations: pooled buffers, ArrayPool<byte>, ref structs.
	•	Avoid string conversions on the hot path; OIDs kept as numeric uint arrays.
	•	Batch varbinds for GetBulk/Walk.
	•	Benchmarks:
	•	Encode/decode 100k varbinds/s target on commodity hardware.
	•	P95 request RTT overhead < 10µs codec time at 1KB payloads (local loopback).

⸻

10) Testing Strategy
	•	Golden vectors: hand‑crafted packets for all PDUs & types.
	•	USM vectors: known inputs/outputs for SHA‑1/2 and AES privacy.
	•	Property‑based tests: round‑trip (encode ∘ decode) == id for all types.
	•	Interop tests: talk to net-snmp / snmpsimd via Testcontainers; run Walk/BulkWalk/Set.
	•	Fuzzing: SharpFuzz targets BerReader and v3 decoder.

⸻

11) Security Considerations
	•	Default to v3 authPriv; require explicit opt‑in for MD5/DES.
	•	Zero sensitive key material from buffers after use where feasible.
	•	Constant‑time compare for auth digests.
	•	Validate lengths/indices defensively in codec.

⸻

12) Roadmap & Milestones
	1.	M1 — BER & Core Types: Codec + OID; v2c message round‑trip; unit tests.
	2.	M2 — Manager v1/v2c: Get/GetNext/Set/GetBulk; Walk/BulkWalk; retries; basic trap sender/receiver.
	3.	M3 — USM (v3): discovery, auth (SHA‑1/2), priv (AES‑128), Report handling; v3 Manager.
	4.	M4 — Agent v1/v2c: scalar & table providers; Set handling.
	5.	M5 — Agent v3: USM server side; minimal VACM.
	6.	M6 — MIB loader: SMIv2 subset, OID tree, display hints; optional codegen tool.
	7.	M7 — Hardening: fuzzing, interop matrices, benchmarks, docs & samples.

⸻

13) Example Code Sketches

These are illustrative, trimmed for brevity; real code should include validations and error handling.

13.1 BER Writer/Reader (sketch)

public ref struct BerReader
{
    private ReadOnlySpan<byte> _buf;
    private int _pos;
    public BerReader(ReadOnlySpan<byte> buffer) { _buf = buffer; _pos = 0; }
    public bool TryReadTag(out byte tag) { if (_pos >= _buf.Length) { tag = 0; return false; } tag = _buf[_pos++]; return true; }
    public bool TryReadLength(out int length) { /* decode definite length */ }
    public ReadOnlySpan<byte> ReadSlice(int len) { var s = _buf.Slice(_pos, len); _pos += len; return s; }
    public int ReadInt32() { /* two's complement */ }
    public long ReadInt64() { /* two's complement */ }
    public Oid ReadOid() { /* base‑128 */ }
    public ReadOnlySpan<byte> ReadOctets() { /* ... */ }
}

public ref struct BerWriter
{
    private IBufferWriter<byte> _writer;
    public BerWriter(IBufferWriter<byte> writer) => _writer = writer;
    public void WriteTag(byte tag) { var s = _writer.GetSpan(1); s[0] = tag; _writer.Advance(1); }
    public void WriteLength(int len) { /* definite length */ }
    public void WriteInt32(int value) { /* big‑endian minimal */ }
    public void WriteOid(in Oid oid) { /* base‑128 */ }
    public void WriteOctets(ReadOnlySpan<byte> data) { /* ... */ }
}

13.2 Manager usage

var client = SnmpClient.CreateCommunity(
    new IPEndPoint(IPAddress.Parse("192.168.1.10"), 161),
    community: "public",
    version: Version.V2C);

var sysDescr = new Oid("1.3.6.1.2.1.1.1.0");
var resp = await client.GetAsync(sysDescr);
Console.WriteLine(((SnmpOctetString)resp.VarBinds[0].Value).ToUtf8String());

await foreach (var vb in client.WalkAsync(new Oid("1.3.6.1.2.1.2")))
    Console.WriteLine($"{vb.Oid} = {vb.Value}");

13.3 v3 credentials & discovery

var v3 = SnmpClient.CreateV3(
    new IPEndPoint(IPAddress.Parse("10.0.0.5"), 161),
    new V3Credentials(
        Username: "monitor",
        Auth: AuthAlgorithm.Sha256,
        AuthKey: MemoryMarshal.AsMemory("p@ssw0rd"u8.ToArray()),
        Priv: PrivAlgorithm.Aes128,
        PrivKey: MemoryMarshal.AsMemory("anotherSecret"u8.ToArray())));

await v3.DiscoverAsync();
var resp = await v3.GetAsync(new Oid("1.3.6.1.2.1.1.5.0"));

13.4 Agent host (scalar)

var agent = new SnmpAgentHost(new SnmpAgentOptions { Listen = new IPEndPoint(IPAddress.Any, 161) });
agent.MapScalar(new Oid("1.3.6.1.2.1.1.1.0"), () => new SnmpOctetString("nSNMP agent"));
await agent.StartAsync();


⸻

14) Extensibility Points
	•	Transports: IUdpChannel, ITcpChannel interfaces allow TLS/DTLS later (TSM).
	•	Crypto: IAuthProvider, IPrivProvider for adding new algorithms without touching core.
	•	MIB: loader accepts external repositories; codegen plugs via Roslyn source generators.

⸻

15) Design Choices & Rationale
	•	Own BER codec for control & performance; ASN.1 libraries are heavy for BER nuances used in SNMP.
	•	Request multiplexer keeps a single socket per target by default, reducing ports/FDs.
	•	No implicit retries for SET to avoid side effects.
	•	SHA‑2/AES by default reflects current operational best practice.

⸻

16) Interop & Compatibility
	•	Tested against: net‑snmp (snmpd, snmpsimd), common network gear (Cisco/Juniper/HPE), Windows SNMP service where applicable.
	•	Ensure correct v3 engine discovery and Report handling for diverse vendors.

⸻

17) Deliverables
	•	NuGet packages (nSNMP.*).
	•	Samples: Manager (walk, bulkwalk, set), Agent (scalars, table), Trap listener.
	•	Docs: API reference (DocFX), How‑Tos, MIB loader guide.
	•	Benchmarks & interop matrix.

⸻

18) Open Questions (to refine later)
	•	How much of VACM to implement initially?
	•	Should we include TCP by default or as an optional transport package?
	•	Scope of DES/MD5: build optional package nSNMP.LegacyCrypto?

⸻

19) Appendix — Minimal Type Definitions

public enum Version { V1 = 0, V2C = 1, V3 = 3 }

public enum AuthAlgorithm { None, Md5, Sha1, Sha224, Sha256, Sha384, Sha512 }
public enum PrivAlgorithm { None, Des, Aes128 }

public sealed class SnmpClientOptions
{
    public EndPoint Target { get; init; } = default!;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);
    public int MaxRetries { get; init; } = 1; // idempotent ops only
    public bool ThrowOnSnmpError { get; init; } = false;
    public int ReceiveBufferSize { get; init; } = 64 * 1024;
}

public sealed class TrapListenerOptions
{
    public EndPoint Listen { get; init; } = new IPEndPoint(IPAddress.Any, 162);
    public bool AllowV1 { get; init; } = false;
    public bool AllowV2c { get; init; } = true;
    public bool AllowV3 { get; init; } = true;
}


⸻

20) Next Steps
	•	Validate API surface with 2–3 real scenarios (monitoring, config push, trap receiver).
	•	Start with M1 (codec + round‑trip tests), then iterate per roadmap.
	•	Decide on package/namespace branding (nSNMP.*).