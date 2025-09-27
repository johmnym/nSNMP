Network Scout — UI Design (v2)

A compact .NET MAUI app that combines LAN Healthboard + Interface Watch for fast network assessments using SNMP (v1/v2c/v3). This revision adds concrete bindings, UI states, math, and performance budgets so implementation is straightforward.

⸻

1) Scope & Goals
	•	Two-screen core: Healthboard (triage) + Interface Watch (proof). Auxiliary: Discovery, Alerts, Settings, Logs (optional).
	•	Fast signal: high-density lists/cards with sortable metrics; one-tap drill-down.
	•	Correct math: COUNTER64-first, discontinuity-safe, rate windows (5m/60m).
	•	Portable: MAUI Shell + MVVM; works on mobile/desktop; foreground-friendly.
	•	Offline: Capture/Replay for demos & unit tests.

⸻

2) Information Architecture & Navigation

Navigation model: MAUI Shell; bottom tabs (mobile) / left flyout (desktop).

Shell routes

/health
/health/device/{deviceId}
/health/device/{deviceId}/if/{ifId}      // Interface Watch
/discovery
/alerts
/settings
/logs (optional)

Primary tabs: Healthboard · Discovery · Alerts · Settings.

⸻

3) Design Tokens (Theme)
	•	Spacing: 8px base scale.
	•	Typography: System font; Title 20/24, Body 14/16, Mono 13 (OIDs).
	•	Colors: (dark theme first)
	•	Bg0 #111A22, Bg1 #192633, Card #233648, Stroke #324D67.
	•	TextPrimary #FFFFFF, TextSecondary #92ADC9.
	•	Chips: OK neutral, Warn #FFB020, Crit #E53935.
	•	Elevation: soft shadow on cards; 12–16px radius.
	•	Status chips: rounded, medium density; icons for link up/down, shield for v3.

⸻

4) Healthboard (Triage)

Purpose: see inventory + current hotspots and jump to watch view.

4.1 Layout
	•	Header: Search (name/IP/ifAlias), Filters (Location, Vendor), Refresh.
	•	Section header: Top Busy Interfaces (5m) · updated {Freshness}.
	•	Controls: Sort: Util | Errors | Flaps · Scope: All | Location · Security: Any | v3 authPriv.
	•	Content (responsive):
	•	Mobile: card list (one per row).
	•	Desktop: table view (virtualized) + optional per-row sparkline.

4.2 Card spec (mobile)
	•	Top line (metrics): 85% · in 740 Mbps / out 120 Mbps · 0.02‰ err · 0 flaps (5m)
	•	Title: Router-1 · eth0 (WAN)
	•	Link line: Up · 1 Gbps · MTU 1500
	•	Chips: v3 authPriv · 64-bit
	•	Sparkline: tiny dual-series (in/out bps), 60 points (5m @ 5s cadence)
	•	Action: whole card is tappable → Interface Watch; keep a small Watch button for affordance.

4.3 Table spec (desktop)

Columns: Device | IfName/Alias | Admin/Oper | Speed | Util% | In Err‰ | Out Err‰ | Flaps(60m) | Last change | Status.

4.4 Sorting & filtering rules
	•	Sort by numeric util (max of in/out), then err‰, then flaps.
	•	Filters persist (Preferences) and survive app restarts.

4.5 Empty/Loading/Error states
	•	Empty: CTA → Run Discovery; toggle Replay if capture available.
	•	Loading: 5 skeleton rows/cards.
	•	Error: inline row banner (auth failed, timeout) with Retry.

⸻

5) Interface Watch (Proof)

Purpose: live, discontinuity-safe rates and events for a chosen interface.

5.1 Header
	•	Device + interface pickers (preselect from navigation).
	•	Status pills: Up/Down, Flapping (if ≥2 toggles in window), v3 authPriv/v2c, 64-bit/32-bit.
	•	Facts: Speed, MTU, ifIndex, ifAlias.

5.2 Live chart
	•	Dual line series (in/out bps), 1–5s polling; 10–60 min ring buffer (configurable).
	•	Event rail overlaid: OperStatus flips, threshold alerts.
	•	Stats strip: Avg · 95th · Max · Err‰ · Disc‰ · Flaps (window).

5.3 Actions
	•	Copy OID set, Export CSV (window), Export PNG, Add Alert Rule.

5.4 Math & safety
	•	Prefer ifHCInOctets/ifHCOutOctets. Fall back to 32-bit only if HC missing and show chip.
	•	Use ifCounterDiscontinuityTime (1.3.6.1.2.1.31.1.1.1.19); drop sample if discontinuity increased.
	•	Wrap check: if counter decreased → drop sample.
	•	Utilization:

bps_in  = (Δoctets_in  * 8) / Δt
bps_out = (Δoctets_out * 8) / Δt
capacity_bps = ifHighSpeed * 1_000_000 (fallback to ifSpeed when null)
util = max(bps_in, bps_out) / capacity_bps


	•	Error/Discard rate (per ‰) when packet counters are available; else show “—”.

⸻

6) Discovery (Inventory)
	•	Inputs: CIDR (default current subnet), credential profile.
	•	Pipeline: ICMP ping → UDP/161 probe → SNMP sysName/sysObjectID → ifTable summary.
	•	Result grid: IP | Name | Vendor | Reachable | SNMP (v2c/v3) | Profile | Add/Ignore.
	•	Concurrency: capped (e.g., 64) with jitter; progress bar and ETA.
	•	Persist added devices with location and credential binding.

⸻

7) Alerts
	•	Default rules: util ≥ 80% (Warn), ≥ 90% (Crit); err‰ ≥ 0.1 (Warn), ≥ 1 (Crit); flap count ≥ 2 in 15m (Warn), ≥ 5 (Crit).
	•	List: time · device · interface · rule · value vs threshold · Ack.
	•	Actions: Ack, Deep link → Interface Watch.
	•	Notifications: Local (Plugin.LocalNotification); debounce to avoid storms.

⸻

8) Settings
	•	Credentials: v1/v2c strings; v3 USM (user, auth algo+key, priv algo+key). SecureStorage.
	•	Polling: per-device/interface intervals; jitter ±10%; global cap for concurrent polls.
	•	Thresholds: util %, error/discard ‰, flap window.
	•	Storage: DB path, rotation (max size, sample retention);
	•	Capture/Replay: enable, choose capture set.
	•	Webhook (optional): POST JSON on alerts; retry with backoff.

⸻

9) ViewModels & Contracts (bindings)

public sealed class HealthboardViewModel : ObservableObject {
    public ObservableCollection<InterfaceCardVM> TopInterfaces { get; }
    public string Query { get; set; }
    public string? SelectedLocation { get; set; }
    public string? SelectedVendor { get; set; }
    public SortKind SortBy { get; set; } // Util | Errors | Flaps
    public ScopeKind Scope { get; set; } // All | Location
    public string Freshness { get; }
    public IAsyncRelayCommand Refresh { get; }
    public IRelayCommand<InterfaceCardVM> OpenWatch { get; }
}

public sealed record InterfaceCardVM(
    Guid DeviceId, int IfIndex,
    string MetricsLine,   // "85% · in 740 Mbps / out 120 Mbps · 0.02‰ err · 0 flaps (5m)"
    string Title,         // "Router-1 · eth0 (WAN)"
    string LinkLine,      // "Up · 1 Gbps · MTU 1500"
    string SecurityChip,  // "v3 authPriv" | "v2c"
    string CounterChip,   // "64-bit" | "32-bit"
    IReadOnlyList<double> InBps,
    IReadOnlyList<double> OutBps,
    double UtilPercent    // numeric for sort
);

public sealed class InterfaceWatchViewModel : ObservableObject {
    public ObservableCollection<InterfaceOption> Interfaces { get; }
    public InterfaceOption? Selected { get; set; }
    public ISeries[] ChartSeries { get; }
    public Axis[] TimeAxis { get; }
    public StatsStripVM Stats { get; }
    public ObservableCollection<EventVM> Events { get; }
    public IRelayCommand ExportCsv { get; }
    public IRelayCommand ExportPng { get; }
    public IRelayCommand CopyOids { get; }
}


⸻

10) Data & Persistence (SQLite + Dapper or EF Core)

Tables

Device(id TEXT PK, ip TEXT NOT NULL, name TEXT, vendor TEXT, model TEXT,
       last_seen_utc TEXT, is_online INTEGER)
NetIf(id TEXT PK, device_id TEXT NOT NULL, if_index INTEGER,
      name TEXT, alias TEXT, speed_bps INTEGER, highspeed_mbps INTEGER,
      admin_status INTEGER, oper_status INTEGER, last_change_ticks INTEGER,
      FOREIGN KEY(device_id) REFERENCES Device(id) ON DELETE CASCADE)
Sample(id INTEGER PK AUTOINCREMENT, if_id TEXT NOT NULL, at_utc TEXT NOT NULL,
       in_bps REAL, out_bps REAL, err_in_ppm REAL, err_out_ppm REAL,
       disc_in_ppm REAL, disc_out_ppm REAL,
       FOREIGN KEY(if_id) REFERENCES NetIf(id) ON DELETE CASCADE)
Alert(id TEXT PK, device_id TEXT, if_id TEXT, kind TEXT, value REAL,
      threshold REAL, raised_at_utc TEXT, ack INTEGER)

Indexes: IX_Interface_DeviceIndex(device_id, if_index) · IX_Sample_If_Time(if_id, at_utc DESC).

Retention: keep raw Samples for 24–72h; aggregate older (5m buckets) or purge.

⸻

11) SNMP Adapter Contract

public interface ISnmpClient {
  Task<IReadOnlyList<VarBind>> GetAsync(string ip, Credential cred, params string[] oids);
  IAsyncEnumerable<IReadOnlyList<VarBind>> WalkAsync(string ip, Credential cred, string rootOid, int maxRepetitions = 10, CancellationToken ct = default);
}

Core OIDs (Healthboard/Watch)
	•	Identity: 1.3.6.1.2.1.1.{1,2,3,5}.0 → sysDescr, sysObjectID, sysUpTime, sysName.
	•	IF-MIB: ifIndex, ifDescr, ifName, ifAlias, ifType, ifSpeed, ifHighSpeed, ifMtu, ifAdminStatus, ifOperStatus, ifLastChange.
	•	IF-MIB HC: ifHCInOctets, ifHCOutOctets, (optionally) ifHC{In,Out}{Ucast,Multicast,Broadcast}Pkts.
	•	IFX: ifCounterDiscontinuityTime.

⸻

12) Performance & Concurrency Budgets
	•	Poll cadence: 1–5s for watched interfaces; 10–30s for healthboard summary.
	•	Max concurrent polls: 8–16 (configurable) with SemaphoreSlim.
	•	UI updates: coalesce to 1 Hz for lists; charts update at poll cadence.
	•	Virtualization: CollectionView everywhere; max 60 sparkline points.
	•	DB writes: batch inserts per tick; WAL mode for SQLite.

⸻

13) Accessibility & Input
	•	Focus order: search → filters → list.
	•	Minimum target size: 44×44pt; visible focus states.
	•	Screen reader labels on chips (e.g., “Security: v3 authPriv”).

⸻

14) Permissions & Platform Notes
	•	iOS: NSLocalNetworkUsageDescription; for mDNS later, add NSBonjourServices. Foreground-only sockets for UDP listeners.
	•	Android: INTERNET, ACCESS_NETWORK_STATE, ACCESS_WIFI_STATE (+ multicast for mDNS later).
	•	Desktop: ensure UDP allowed by firewall.

⸻

15) Testing Checklist (UI + Math)
	•	v2c/v3 (authPriv) against 2+ vendors; wrong creds → clean errors.
	•	32-bit fallback flagged; discontinuity (reboot) → samples dropped.
	•	Wrap tests on counters; long-Δt sample ignored.
	•	Sorting stable and correct under live updates.
	•	Replay mode renders consistent charts.

⸻

16) Definition of Done (UI)
	•	Healthboard shows ≥20 devices responsively; 60 FPS scroll.
	•	Interface Watch runs 5s cadence for 30 min with no UI jank.
	•	Dark/light mode parity; localization-ready numbers.
	•	All actions navigable by keyboard and tap.

⸻

17) Appendix A — Rate Formatting Helper

static string BpsToHuman(double bps){
    if (bps >= 1_000_000_000) return $"{bps/1_000_000_000:0.#} Gbps";
    if (bps >= 1_000_000)     return $"{bps/1_000_000:0.#} Mbps";
    if (bps >= 1_000)         return $"{bps/1_000:0.#} Kbps";
    return $"{bps:0} bps";
}
static string MetricsLine(double utilPct,double inBps,double outBps,double errPerThousand,int flaps,TimeSpan window)
    => $"{utilPct:0}% · in {BpsToHuman(inBps)} / out {BpsToHuman(outBps)} · {errPerThousand:0.##}‰ err · {flaps} flaps ({window.TotalMinutes:0}m)";


⸻

18) Appendix B — Page & DI Skeleton

public static class MauiProgram {
  public static MauiApp CreateMauiApp(){
    var b = MauiApp.CreateBuilder();
    b.UseMauiApp<App>();
    // Charts
    b.UseMauiCharts();
    // DI
    b.Services.AddSingleton<ISnmpClient, NsnpClientAdapter>();
    b.Services.AddSingleton<IRateCalculator, RateCalculator>();
    b.Services.AddSingleton<IPollingService, PollingService>();
    b.Services.AddSingleton<IDeviceRepo, SqliteDeviceRepo>();
    b.Services.AddSingleton<IInterfaceRepo, SqliteInterfaceRepo>();
    b.Services.AddSingleton<ISampleRepo, SqliteSampleRepo>();
    b.Services.AddTransient<HealthboardViewModel>();
    b.Services.AddTransient<HealthboardPage>();
    b.Services.AddTransient<InterfaceWatchViewModel>();
    b.Services.AddTransient<InterfaceWatchPage>();
    return b.Build();
  }
}


⸻

Notes
	•	Replace any “hero image” blocks from earlier mocks with inline sparklines on each interface card.
	•	Persist user choices (sort/scope) via Preferences.
	•	Keep the core minimal: Healthboard + Watch shippable first; Discovery/Alerts/Settings can follow in slices.