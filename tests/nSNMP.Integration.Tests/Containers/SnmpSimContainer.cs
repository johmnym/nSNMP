using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace nSNMP.Integration.Tests.Containers;

public class SnmpSimContainer : IAsyncDisposable
{
    private readonly IContainer _container;
    private readonly ILogger<SnmpSimContainer> _logger;
    private readonly string _deviceName;
    private readonly int _udpPort;

    public SnmpSimContainer(
        string deviceName,
        int udpPort,
        string dataFile,
        string? community = null,
        SnmpV3Config? v3Config = null,
        INetwork? network = null,
        ILogger<SnmpSimContainer>? logger = null)
    {
        _deviceName = deviceName;
        _udpPort = udpPort;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SnmpSimContainer>.Instance;

        var builder = new ContainerBuilder()
            .WithImage("snmpsim")
            .WithName($"snmpsim-{deviceName}")
            .WithPortBinding(udpPort, 161)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(161))
            .WithResourceMapping("Scenarios", "/usr/share/snmpsim/data")
            .WithCleanUp(true);

        // Configure command based on SNMP version
        var command = BuildCommand(dataFile, community, v3Config);
        builder = builder.WithCommand(command);

        if (network != null)
        {
            builder = builder.WithNetwork(network);
        }

        _container = builder.Build();
    }

    private string[] BuildCommand(string dataFile, string? community, SnmpV3Config? v3Config)
    {
        var args = new List<string>
        {
            "snmpsim.py",
            "--data-dir=/usr/share/snmpsim/data",
            $"--agent-udpv4-endpoint=0.0.0.0:161",
            "--logging-method=stderr:info"
        };

        if (!string.IsNullOrEmpty(community))
        {
            args.Add($"--v2c-arch");
            args.Add($"--community={community}");
        }

        if (v3Config != null)
        {
            args.Add("--v3-arch");
            args.Add($"--v3-user={v3Config.Username}");
            args.Add($"--v3-auth-key={v3Config.AuthKey}");
            args.Add($"--v3-auth-proto={v3Config.AuthProtocol}");
            args.Add($"--v3-priv-key={v3Config.PrivKey}");
            args.Add($"--v3-priv-proto={v3Config.PrivProtocol}");

            if (!string.IsNullOrEmpty(v3Config.EngineId))
            {
                args.Add($"--v3-engine-id={v3Config.EngineId}");
            }
        }

        args.Add($"--device-dir=/usr/share/snmpsim/data/{dataFile}");

        return args.ToArray();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SNMP simulator container for device: {DeviceName} on UDP port {Port}",
            _deviceName, _udpPort);

        await _container.StartAsync(cancellationToken);

        _logger.LogInformation("SNMP simulator container started for device: {DeviceName}", _deviceName);

        // Give the container a moment to fully initialize
        await Task.Delay(2000, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping SNMP simulator container for device: {DeviceName}", _deviceName);
        await _container.StopAsync(cancellationToken);
    }

    public string GetConnectionEndpoint()
    {
        var hostPort = _container.GetMappedPublicPort(161);
        return $"localhost:{hostPort}";
    }

    public string GetContainerName() => _container.Name;

    public string GetContainerId() => _container.Id;

    public async Task<string> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        var (stdout, stderr) = await _container.GetLogsAsync();

        var logs = new StringBuilder();
        logs.AppendLine("=== STDOUT ===");
        logs.AppendLine(stdout);
        logs.AppendLine("=== STDERR ===");
        logs.AppendLine(stderr);

        return logs.ToString();
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = _container.State;
            return Task.FromResult(state == DotNet.Testcontainers.Containers.TestcontainersStates.Running);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing SNMP simulator container for device: {DeviceName}", _deviceName);
        }
    }
}

public record SnmpV3Config(
    string Username,
    string AuthKey,
    string AuthProtocol,
    string PrivKey,
    string PrivProtocol,
    string? EngineId = null);