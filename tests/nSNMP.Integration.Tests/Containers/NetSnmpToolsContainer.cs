using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace nSNMP.Integration.Tests.Containers;

public class NetSnmpToolsContainer : IAsyncDisposable
{
    private readonly IContainer _container;
    private readonly ILogger<NetSnmpToolsContainer> _logger;
    private readonly INetwork? _network;

    public NetSnmpToolsContainer(INetwork? network = null, ILogger<NetSnmpToolsContainer>? logger = null)
    {
        _network = network;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NetSnmpToolsContainer>.Instance;

        var builder = new ContainerBuilder()
            .WithImage("net-snmp-tools")
            .WithName("net-snmp-tools")
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null") // Keep container running
            .WithCleanUp(true);

        if (network != null)
        {
            builder = builder.WithNetwork(network);
        }

        _container = builder.Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting NET-SNMP tools container");
        await _container.StartAsync(cancellationToken);
        _logger.LogInformation("NET-SNMP tools container started");

        // Give the container a moment to fully initialize
        await Task.Delay(1000, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping NET-SNMP tools container");
        await _container.StopAsync(cancellationToken);
    }

    public async Task<TrapResult> SendV2cTrapAsync(
        string targetHost,
        int targetPort,
        string community,
        string uptime,
        string trapOid,
        string[] varbinds,
        CancellationToken cancellationToken = default)
    {
        var command = BuildV2cTrapCommand(targetHost, targetPort, community, uptime, trapOid, varbinds);

        _logger.LogDebug("Sending SNMPv2c trap: {Command}", string.Join(" ", command));

        var result = await _container.ExecAsync(command, cancellationToken);

        return new TrapResult(
            Success: result.ExitCode == 0,
            ExitCode: result.ExitCode,
            Stdout: result.Stdout,
            Stderr: result.Stderr,
            Command: string.Join(" ", command)
        );
    }

    public async Task<TrapResult> SendV3InformAsync(
        string targetHost,
        int targetPort,
        string username,
        string authProtocol,
        string authKey,
        string privProtocol,
        string privKey,
        string engineId,
        string uptime,
        string trapOid,
        string[] varbinds,
        CancellationToken cancellationToken = default)
    {
        var command = BuildV3InformCommand(
            targetHost, targetPort, username, authProtocol, authKey,
            privProtocol, privKey, engineId, uptime, trapOid, varbinds);

        _logger.LogDebug("Sending SNMPv3 INFORM: {Command}", string.Join(" ", command));

        var result = await _container.ExecAsync(command, cancellationToken);

        return new TrapResult(
            Success: result.ExitCode == 0,
            ExitCode: result.ExitCode,
            Stdout: result.Stdout,
            Stderr: result.Stderr,
            Command: string.Join(" ", command)
        );
    }

    private string[] BuildV2cTrapCommand(
        string targetHost,
        int targetPort,
        string community,
        string uptime,
        string trapOid,
        string[] varbinds)
    {
        var command = new List<string>
        {
            "snmptrap",
            "-v2c",
            "-c", community,
            $"{targetHost}:{targetPort}",
            uptime,
            trapOid
        };

        command.AddRange(varbinds);
        return command.ToArray();
    }

    private string[] BuildV3InformCommand(
        string targetHost,
        int targetPort,
        string username,
        string authProtocol,
        string authKey,
        string privProtocol,
        string privKey,
        string engineId,
        string uptime,
        string trapOid,
        string[] varbinds)
    {
        var command = new List<string>
        {
            "snmpinform",
            "-v3",
            "-u", username,
            "-l", "authPriv",
            "-a", MapAuthProtocol(authProtocol),
            "-A", authKey,
            "-x", MapPrivProtocol(privProtocol),
            "-X", privKey,
            "-e", engineId,
            $"{targetHost}:{targetPort}",
            uptime,
            trapOid
        };

        command.AddRange(varbinds);
        return command.ToArray();
    }

    private static string MapAuthProtocol(string protocol) => protocol.ToUpperInvariant() switch
    {
        "SHA256" => "SHA",
        "SHA" => "SHA",
        "MD5" => "MD5",
        _ => "SHA"
    };

    private static string MapPrivProtocol(string protocol) => protocol.ToUpperInvariant() switch
    {
        "AES128" => "AES",
        "AES" => "AES",
        "DES" => "DES",
        _ => "AES"
    };

    public async Task<string> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        var (stdout, stderr) = await _container.GetLogsAsync();

        var logs = new StringBuilder();
        logs.AppendLine("=== NET-SNMP Tools STDOUT ===");
        logs.AppendLine(stdout);
        logs.AppendLine("=== NET-SNMP Tools STDERR ===");
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
            _logger.LogError(ex, "Error disposing NET-SNMP tools container");
        }
    }
}

public record TrapResult(
    bool Success,
    long ExitCode,
    string Stdout,
    string Stderr,
    string Command);