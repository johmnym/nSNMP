using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using nSNMP.Integration.Tests.Configuration;
using nSNMP.Integration.Tests.Containers;
using System.Collections.Concurrent;

namespace nSNMP.Integration.Tests.Infrastructure;

public class IntegrationTestFixture : IAsyncDisposable
{
    private readonly ILogger<IntegrationTestFixture> _logger;
    private readonly ScenarioLoader _scenarioLoader;
    private readonly string _scenarioDirectory;

    private INetwork? _testNetwork;
    private ScenarioConfig? _scenarios;
    private V3UsersConfig? _v3Users;

    private readonly ConcurrentDictionary<string, SnmpSimContainer> _snmpContainers = new();
    private NetSnmpToolsContainer? _netSnmpContainer;
    private readonly ConcurrentDictionary<string, PumbaNetemContainer> _pumbaContainers = new();

    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public IntegrationTestFixture(string? scenarioDirectory = null, ILogger<IntegrationTestFixture>? logger = null)
    {
        _scenarioDirectory = scenarioDirectory ?? Path.Combine(AppContext.BaseDirectory, "Scenarios");
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<IntegrationTestFixture>.Instance;
        _scenarioLoader = new ScenarioLoader();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            _logger.LogInformation("Initializing integration test fixture");

            // Create test network
            _testNetwork = new NetworkBuilder()
                .WithName("nsnmp-integration-test-net")
                .WithCleanUp(true)
                .Build();

            await _testNetwork.CreateAsync(cancellationToken);
            _logger.LogInformation("Created test network: {NetworkName}", _testNetwork.Name);

            // Load configurations
            await LoadConfigurationsAsync(cancellationToken);

            // Initialize core containers
            await InitializeCoreContainersAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Integration test fixture initialized successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task LoadConfigurationsAsync(CancellationToken cancellationToken)
    {
        var scenarioPath = Path.Combine(_scenarioDirectory, "scenarios.yml");
        var usersPath = Path.Combine(_scenarioDirectory, "v3-users.yml");

        _scenarios = await _scenarioLoader.LoadScenariosAsync(scenarioPath, cancellationToken);
        _v3Users = await _scenarioLoader.LoadV3UsersAsync(usersPath, cancellationToken);
    }

    private async Task InitializeCoreContainersAsync(CancellationToken cancellationToken)
    {
        // Initialize NET-SNMP tools container for trap testing
        _netSnmpContainer = new NetSnmpToolsContainer(_testNetwork);
        await _netSnmpContainer.StartAsync(cancellationToken);
    }

    public async Task<SnmpSimContainer> GetOrCreateDeviceAsync(
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        if (_snmpContainers.TryGetValue(deviceName, out var existingContainer))
            return existingContainer;

        var deviceConfig = _scenarios!.Devices.FirstOrDefault(d => d.Name == deviceName)
            ?? throw new ArgumentException($"Device not found: {deviceName}");

        var dataFilePath = _scenarioLoader.GetScenarioDataPath(_scenarioDirectory, deviceConfig.Data);
        if (!File.Exists(dataFilePath))
            throw new FileNotFoundException($"Data file not found: {dataFilePath}");

        SnmpV3Config? v3Config = null;
        string? community = null;

        if (deviceConfig.V2c != null)
        {
            community = deviceConfig.V2c.Community;
        }
        else if (deviceConfig.V3 != null)
        {
            v3Config = new SnmpV3Config(
                deviceConfig.V3.Username,
                deviceConfig.V3.AuthKey,
                deviceConfig.V3.Auth,
                deviceConfig.V3.PrivKey,
                deviceConfig.V3.Priv,
                deviceConfig.V3.EngineId);
        }

        var container = new SnmpSimContainer(
            deviceName,
            deviceConfig.UdpPort,
            deviceConfig.Data,
            community,
            v3Config,
            _testNetwork);

        await container.StartAsync(cancellationToken);

        if (!_snmpContainers.TryAdd(deviceName, container))
        {
            // Another thread created the container first
            await container.DisposeAsync();
            return _snmpContainers[deviceName];
        }

        _logger.LogInformation("Created and started SNMP container for device: {DeviceName}", deviceName);
        return container;
    }

    public async Task<PumbaNetemContainer> CreateNetworkImpairmentAsync(
        string deviceName,
        string impairmentScenarioName,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            await InitializeAsync(cancellationToken);

        var impairmentConfig = _scenarios!.NetworkImpairment.FirstOrDefault(n => n.Name == impairmentScenarioName)
            ?? throw new ArgumentException($"Network impairment scenario not found: {impairmentScenarioName}");

        var targetContainer = await GetOrCreateDeviceAsync(deviceName, cancellationToken);
        var targetContainerName = targetContainer.GetContainerName();

        var networkConfig = new NetworkImpairmentConfig(
            impairmentConfig.Name,
            impairmentConfig.Delay,
            impairmentConfig.Jitter,
            impairmentConfig.Loss,
            impairmentConfig.Description);

        var pumbaContainer = new PumbaNetemContainer(
            targetContainerName,
            networkConfig,
            _testNetwork);

        await pumbaContainer.StartAsync(cancellationToken);

        var key = $"{deviceName}-{impairmentScenarioName}";
        _pumbaContainers.TryAdd(key, pumbaContainer);

        _logger.LogInformation("Created network impairment for device: {DeviceName} using scenario: {ScenarioName}",
            deviceName, impairmentScenarioName);

        return pumbaContainer;
    }

    public NetSnmpToolsContainer GetTrapSender()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync() first.");

        return _netSnmpContainer ?? throw new InvalidOperationException("NET-SNMP tools container not available");
    }

    public ScenarioConfig GetScenarios()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync() first.");

        return _scenarios ?? throw new InvalidOperationException("Scenarios not loaded");
    }

    public V3UsersConfig GetV3Users()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync() first.");

        return _v3Users ?? throw new InvalidOperationException("V3 users not loaded");
    }

    public IEnumerable<SnmpSimContainer> GetActiveDevices() => _snmpContainers.Values;

    public IEnumerable<PumbaNetemContainer> GetActiveImpairments() => _pumbaContainers.Values;

    public async Task StopNetworkImpairmentAsync(string deviceName, string impairmentScenarioName)
    {
        var key = $"{deviceName}-{impairmentScenarioName}";
        if (_pumbaContainers.TryRemove(key, out var pumbaContainer))
        {
            await pumbaContainer.StopAsync();
            await pumbaContainer.DisposeAsync();
            _logger.LogInformation("Stopped network impairment for device: {DeviceName} scenario: {ScenarioName}",
                deviceName, impairmentScenarioName);
        }
    }

    public async Task<string> CollectAllLogsAsync(CancellationToken cancellationToken = default)
    {
        var logs = new List<string>();

        // Collect SNMP container logs
        foreach (var kvp in _snmpContainers)
        {
            try
            {
                var containerLogs = await kvp.Value.GetLogsAsync(cancellationToken);
                logs.Add($"=== Device {kvp.Key} Logs ===\n{containerLogs}");
            }
            catch (Exception ex)
            {
                logs.Add($"=== Device {kvp.Key} Logs (Error) ===\nFailed to retrieve logs: {ex.Message}");
            }
        }

        // Collect NET-SNMP tools logs
        if (_netSnmpContainer != null)
        {
            try
            {
                var toolsLogs = await _netSnmpContainer.GetLogsAsync(cancellationToken);
                logs.Add($"=== NET-SNMP Tools Logs ===\n{toolsLogs}");
            }
            catch (Exception ex)
            {
                logs.Add($"=== NET-SNMP Tools Logs (Error) ===\nFailed to retrieve logs: {ex.Message}");
            }
        }

        // Collect Pumba logs
        foreach (var kvp in _pumbaContainers)
        {
            try
            {
                var pumbaLogs = await kvp.Value.GetLogsAsync(cancellationToken);
                logs.Add($"=== Pumba {kvp.Key} Logs ===\n{pumbaLogs}");
            }
            catch (Exception ex)
            {
                logs.Add($"=== Pumba {kvp.Key} Logs (Error) ===\nFailed to retrieve logs: {ex.Message}");
            }
        }

        return string.Join("\n\n", logs);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing integration test fixture");

        // Dispose Pumba containers
        var pumbaDisposeTasks = _pumbaContainers.Values.Select(c => c.DisposeAsync().AsTask());
        await Task.WhenAll(pumbaDisposeTasks);
        _pumbaContainers.Clear();

        // Dispose SNMP containers
        var snmpDisposeTasks = _snmpContainers.Values.Select(c => c.DisposeAsync().AsTask());
        await Task.WhenAll(snmpDisposeTasks);
        _snmpContainers.Clear();

        // Dispose NET-SNMP tools container
        if (_netSnmpContainer != null)
        {
            await _netSnmpContainer.DisposeAsync();
        }

        // Dispose test network
        if (_testNetwork != null)
        {
            await _testNetwork.DisposeAsync();
        }

        _initLock.Dispose();

        _logger.LogInformation("Integration test fixture disposed");
    }
}