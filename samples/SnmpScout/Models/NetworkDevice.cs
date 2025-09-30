using System.Net;
using SnmpScout.UI;

namespace SnmpScout.Models;

public class NetworkDevice
{
    public required IPAddress IpAddress { get; init; }
    public string? HostName { get; set; }
    public string? SystemName { get; set; }
    public string? SystemDescription { get; set; }
    public string? SystemLocation { get; set; }
    public string? SystemContact { get; set; }
    public string? SystemObjectID { get; set; }
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public TimeSpan? SystemUptime { get; set; }
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
    public SnmpVersion SnmpVersion { get; set; }
    public string? Community { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Unknown;
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public List<NetworkInterface> Interfaces { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();

    public string DisplayName => SystemName ?? HostName ?? IpAddress.ToString();

    public string StatusEmoji => Status switch
    {
        DeviceStatus.Online => EmojiHelper.Online,
        DeviceStatus.Offline => EmojiHelper.Offline,
        DeviceStatus.Warning => EmojiHelper.Warning,
        _ => EmojiHelper.Unknown
    };

    public string TypeEmoji => DeviceType switch
    {
        DeviceType.Router => EmojiHelper.Router,
        DeviceType.Switch => EmojiHelper.Switch,
        DeviceType.AccessPoint => EmojiHelper.AccessPoint,
        DeviceType.Printer => EmojiHelper.Printer,
        DeviceType.Server => EmojiHelper.Server,
        DeviceType.UPS => EmojiHelper.UPS,
        DeviceType.Camera => EmojiHelper.Camera,
        DeviceType.Phone => EmojiHelper.Phone,
        DeviceType.Storage => EmojiHelper.Storage,
        _ => EmojiHelper.QuestionMark
    };
}

public enum DeviceType
{
    Unknown,
    Router,
    Switch,
    AccessPoint,
    Printer,
    Server,
    Workstation,
    UPS,
    Camera,
    Phone,
    Storage,
    Firewall,
    LoadBalancer
}

public enum DeviceStatus
{
    Unknown,
    Online,
    Offline,
    Warning,
    Critical
}

public enum SnmpVersion
{
    V1,
    V2c,
    V3
}

public class NetworkInterface
{
    public int Index { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public InterfaceType Type { get; set; }
    public InterfaceStatus Status { get; set; }
    public long Speed { get; set; }
    public string? MacAddress { get; set; }
    public List<IPAddress> IpAddresses { get; set; } = new();
    public NetworkInterfaceStats? Stats { get; set; }
}

public enum InterfaceType
{
    Unknown,
    Ethernet,
    WiFi,
    Loopback,
    Serial,
    ATM,
    FDDI
}

public enum InterfaceStatus
{
    Unknown,
    Up,
    Down,
    Testing,
    Dormant,
    NotPresent,
    LowerLayerDown
}

public class NetworkInterfaceStats
{
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public long PacketsIn { get; set; }
    public long PacketsOut { get; set; }
    public long ErrorsIn { get; set; }
    public long ErrorsOut { get; set; }
    public long DiscardsIn { get; set; }
    public long DiscardsOut { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Calculated bandwidth metrics
    public double InUtilizationPercent { get; set; }
    public double OutUtilizationPercent { get; set; }
    public double TotalUtilizationPercent { get; set; }
    public double InThroughputBps { get; set; }
    public double OutThroughputBps { get; set; }
    public double InPacketsPerSecond { get; set; }
    public double OutPacketsPerSecond { get; set; }
    public double InErrorRate { get; set; }
    public double OutErrorRate { get; set; }
}