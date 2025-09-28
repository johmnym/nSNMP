using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace nSNMP.Integration.Tests.Configuration;

public class ScenarioLoader
{
    private readonly ILogger<ScenarioLoader> _logger;
    private readonly IDeserializer _deserializer;

    public ScenarioLoader(ILogger<ScenarioLoader>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ScenarioLoader>.Instance;
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<ScenarioConfig> LoadScenariosAsync(string scenarioFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading scenarios from: {FilePath}", scenarioFilePath);

            if (!File.Exists(scenarioFilePath))
            {
                throw new FileNotFoundException($"Scenario file not found: {scenarioFilePath}");
            }

            var yamlContent = await File.ReadAllTextAsync(scenarioFilePath, cancellationToken);
            var scenarios = _deserializer.Deserialize<ScenarioConfig>(yamlContent);

            _logger.LogInformation("Loaded {DeviceCount} devices, {ImpairmentCount} network impairment scenarios",
                scenarios.Devices.Count, scenarios.NetworkImpairment.Count);

            ValidateScenarios(scenarios);
            return scenarios;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scenarios from: {FilePath}", scenarioFilePath);
            throw;
        }
    }

    public async Task<V3UsersConfig> LoadV3UsersAsync(string usersFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading SNMPv3 users from: {FilePath}", usersFilePath);

            if (!File.Exists(usersFilePath))
            {
                throw new FileNotFoundException($"V3 users file not found: {usersFilePath}");
            }

            var yamlContent = await File.ReadAllTextAsync(usersFilePath, cancellationToken);
            var users = _deserializer.Deserialize<V3UsersConfig>(yamlContent);

            _logger.LogInformation("Loaded {UserCount} SNMPv3 users", users.Users.Count);

            ValidateV3Users(users);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SNMPv3 users from: {FilePath}", usersFilePath);
            throw;
        }
    }

    private void ValidateScenarios(ScenarioConfig scenarios)
    {
        // Validate devices
        var deviceNames = new HashSet<string>();
        var udpPorts = new HashSet<int>();

        foreach (var device in scenarios.Devices)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
                throw new InvalidOperationException("Device name cannot be empty");

            if (!deviceNames.Add(device.Name))
                throw new InvalidOperationException($"Duplicate device name: {device.Name}");

            if (!udpPorts.Add(device.UdpPort))
                throw new InvalidOperationException($"Duplicate UDP port: {device.UdpPort}");

            if (device.UdpPort < 1024 || device.UdpPort > 65535)
                throw new InvalidOperationException($"Invalid UDP port for device {device.Name}: {device.UdpPort}");

            if (string.IsNullOrWhiteSpace(device.Data))
                throw new InvalidOperationException($"Device {device.Name} must specify a data file");

            // Validate SNMP configuration
            if (device.V2c == null && device.V3 == null)
                throw new InvalidOperationException($"Device {device.Name} must have either v2c or v3 configuration");

            if (device.V2c != null && device.V3 != null)
                throw new InvalidOperationException($"Device {device.Name} cannot have both v2c and v3 configuration");

            if (device.V2c != null && string.IsNullOrWhiteSpace(device.V2c.Community))
                throw new InvalidOperationException($"Device {device.Name} v2c configuration must specify community");

            if (device.V3 != null)
            {
                ValidateV3Config(device.Name, device.V3);
            }
        }

        // Validate trap endpoints
        if (scenarios.TrapSender != null)
        {
            var endpointNames = new HashSet<string>();
            foreach (var endpoint in scenarios.TrapSender.Endpoints)
            {
                if (string.IsNullOrWhiteSpace(endpoint.Name))
                    throw new InvalidOperationException("Trap endpoint name cannot be empty");

                if (!endpointNames.Add(endpoint.Name))
                    throw new InvalidOperationException($"Duplicate trap endpoint name: {endpoint.Name}");

                if (endpoint.Version.Equals("v2c", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(endpoint.Community))
                        throw new InvalidOperationException($"V2c trap endpoint {endpoint.Name} must specify community");
                }
                else if (endpoint.Version.Equals("v3", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(endpoint.Username))
                        throw new InvalidOperationException($"V3 trap endpoint {endpoint.Name} must specify username");
                }
            }
        }

        // Validate network impairment scenarios
        var impairmentNames = new HashSet<string>();
        foreach (var impairment in scenarios.NetworkImpairment)
        {
            if (string.IsNullOrWhiteSpace(impairment.Name))
                throw new InvalidOperationException("Network impairment scenario name cannot be empty");

            if (!impairmentNames.Add(impairment.Name))
                throw new InvalidOperationException($"Duplicate network impairment scenario name: {impairment.Name}");

            if (string.IsNullOrWhiteSpace(impairment.Delay) && string.IsNullOrWhiteSpace(impairment.Loss))
                throw new InvalidOperationException($"Network impairment scenario {impairment.Name} must specify delay or loss");
        }
    }

    private void ValidateV3Config(string deviceName, V3Config v3)
    {
        if (string.IsNullOrWhiteSpace(v3.Username))
            throw new InvalidOperationException($"Device {deviceName} v3 configuration must specify username");

        if (string.IsNullOrWhiteSpace(v3.Auth))
            throw new InvalidOperationException($"Device {deviceName} v3 configuration must specify auth protocol");

        if (string.IsNullOrWhiteSpace(v3.AuthKey))
            throw new InvalidOperationException($"Device {deviceName} v3 configuration must specify auth key");

        if (string.IsNullOrWhiteSpace(v3.Priv))
            throw new InvalidOperationException($"Device {deviceName} v3 configuration must specify priv protocol");

        if (string.IsNullOrWhiteSpace(v3.PrivKey))
            throw new InvalidOperationException($"Device {deviceName} v3 configuration must specify priv key");
    }

    private void ValidateV3Users(V3UsersConfig users)
    {
        var usernames = new HashSet<string>();
        foreach (var user in users.Users)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new InvalidOperationException("V3 user username cannot be empty");

            if (!usernames.Add(user.Username))
                throw new InvalidOperationException($"Duplicate V3 username: {user.Username}");

            if (string.IsNullOrWhiteSpace(user.AuthProtocol))
                throw new InvalidOperationException($"V3 user {user.Username} must specify auth protocol");

            if (string.IsNullOrWhiteSpace(user.AuthKey))
                throw new InvalidOperationException($"V3 user {user.Username} must specify auth key");

            if (string.IsNullOrWhiteSpace(user.PrivProtocol))
                throw new InvalidOperationException($"V3 user {user.Username} must specify priv protocol");

            if (string.IsNullOrWhiteSpace(user.PrivKey))
                throw new InvalidOperationException($"V3 user {user.Username} must specify priv key");
        }
    }

    public string GetScenarioDataPath(string scenarioDirectory, string dataFile)
    {
        return Path.Combine(scenarioDirectory, dataFile);
    }

    public bool IsDataFileValid(string scenarioDirectory, string dataFile)
    {
        var fullPath = GetScenarioDataPath(scenarioDirectory, dataFile);
        return File.Exists(fullPath);
    }
}