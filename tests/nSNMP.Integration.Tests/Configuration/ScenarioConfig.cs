using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace nSNMP.Integration.Tests.Configuration;

public class ScenarioConfig
{
    [YamlMember(Alias = "devices")]
    public List<DeviceConfig> Devices { get; set; } = new();

    [YamlMember(Alias = "trap_sender")]
    public TrapSenderConfig? TrapSender { get; set; }

    [YamlMember(Alias = "network_impairment")]
    public List<NetworkImpairmentScenario> NetworkImpairment { get; set; } = new();
}

public class DeviceConfig
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "image")]
    public string Image { get; set; } = string.Empty;

    [YamlMember(Alias = "udpPort")]
    public int UdpPort { get; set; }

    [YamlMember(Alias = "v2c")]
    public V2cConfig? V2c { get; set; }

    [YamlMember(Alias = "v3")]
    public V3Config? V3 { get; set; }

    [YamlMember(Alias = "data")]
    public string Data { get; set; } = string.Empty;

    [YamlMember(Alias = "alerts")]
    public bool Alerts { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}

public class V2cConfig
{
    [YamlMember(Alias = "community")]
    public string Community { get; set; } = string.Empty;
}

public class V3Config
{
    [YamlMember(Alias = "username")]
    public string Username { get; set; } = string.Empty;

    [YamlMember(Alias = "auth")]
    public string Auth { get; set; } = string.Empty;

    [YamlMember(Alias = "authKey")]
    public string AuthKey { get; set; } = string.Empty;

    [YamlMember(Alias = "priv")]
    public string Priv { get; set; } = string.Empty;

    [YamlMember(Alias = "privKey")]
    public string PrivKey { get; set; } = string.Empty;

    [YamlMember(Alias = "engineId")]
    public string EngineId { get; set; } = string.Empty;
}

public class TrapSenderConfig
{
    [YamlMember(Alias = "image")]
    public string Image { get; set; } = string.Empty;

    [YamlMember(Alias = "endpoints")]
    public List<TrapEndpointConfig> Endpoints { get; set; } = new();
}

public class TrapEndpointConfig
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    [YamlMember(Alias = "community")]
    public string? Community { get; set; }

    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    [YamlMember(Alias = "auth")]
    public string? Auth { get; set; }

    [YamlMember(Alias = "authKey")]
    public string? AuthKey { get; set; }

    [YamlMember(Alias = "priv")]
    public string? Priv { get; set; }

    [YamlMember(Alias = "privKey")]
    public string? PrivKey { get; set; }

    [YamlMember(Alias = "engineId")]
    public string? EngineId { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}

public class NetworkImpairmentScenario
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "delay")]
    public string? Delay { get; set; }

    [YamlMember(Alias = "jitter")]
    public string? Jitter { get; set; }

    [YamlMember(Alias = "loss")]
    public string? Loss { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}

public class V3UsersConfig
{
    [YamlMember(Alias = "users")]
    public List<V3UserConfig> Users { get; set; } = new();
}

public class V3UserConfig
{
    [YamlMember(Alias = "username")]
    public string Username { get; set; } = string.Empty;

    [YamlMember(Alias = "auth_protocol")]
    public string AuthProtocol { get; set; } = string.Empty;

    [YamlMember(Alias = "auth_key")]
    public string AuthKey { get; set; } = string.Empty;

    [YamlMember(Alias = "priv_protocol")]
    public string PrivProtocol { get; set; } = string.Empty;

    [YamlMember(Alias = "priv_key")]
    public string PrivKey { get; set; } = string.Empty;

    [YamlMember(Alias = "engine_id")]
    public string EngineId { get; set; } = string.Empty;
}