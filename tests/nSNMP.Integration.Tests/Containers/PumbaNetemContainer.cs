using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace nSNMP.Integration.Tests.Containers;

public class PumbaNetemContainer : IAsyncDisposable
{
    private readonly IContainer _container;
    private readonly ILogger<PumbaNetemContainer> _logger;
    private readonly NetworkImpairmentConfig _config;
    private readonly string _targetContainerName;

    public PumbaNetemContainer(
        string targetContainerName,
        NetworkImpairmentConfig config,
        INetwork? network = null,
        ILogger<PumbaNetemContainer>? logger = null)
    {
        _targetContainerName = targetContainerName;
        _config = config;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PumbaNetemContainer>.Instance;

        var command = BuildNetemCommand(targetContainerName, config);

        var builder = new ContainerBuilder()
            .WithImage("gaiaadm/pumba")
            .WithName($"pumba-{config.Name}")
            .WithCommand(command)
            .WithPrivileged(true)
            .WithVolumeMount("/var/run/docker.sock", "/var/run/docker.sock")
            .WithCleanUp(true);

        if (network != null)
        {
            builder = builder.WithNetwork(network);
        }

        _container = builder.Build();
    }

    private string[] BuildNetemCommand(string targetContainer, NetworkImpairmentConfig config)
    {
        var args = new List<string> { "netem" };

        // Add delay if specified
        if (!string.IsNullOrEmpty(config.Delay))
        {
            args.Add("--duration");
            args.Add("60s"); // Run for 60 seconds
            args.Add("delay");
            args.Add(config.Delay);

            if (!string.IsNullOrEmpty(config.Jitter))
            {
                args.Add(config.Jitter);
            }
        }

        // Add packet loss if specified
        if (!string.IsNullOrEmpty(config.PacketLoss))
        {
            if (args.Contains("delay"))
            {
                // Combined delay and loss
                args.Add("loss");
                args.Add(config.PacketLoss);
            }
            else
            {
                // Loss only
                args.Add("--duration");
                args.Add("60s");
                args.Add("loss");
                args.Add(config.PacketLoss);
            }
        }

        // Target container pattern
        args.Add($"--target-pattern={targetContainer}");

        return args.ToArray();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Pumba network impairment for target: {Target} with config: {Config}",
            _targetContainerName, _config);

        await _container.StartAsync(cancellationToken);

        _logger.LogInformation("Pumba network impairment started for target: {Target}", _targetContainerName);

        // Give Pumba a moment to apply network impairment
        await Task.Delay(2000, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Pumba network impairment for target: {Target}", _targetContainerName);
        await _container.StopAsync(cancellationToken);

        // Give a moment for network conditions to restore
        await Task.Delay(1000, cancellationToken);
    }

    public async Task<string> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        var (stdout, stderr) = await _container.GetLogsAsync();

        var logs = new StringBuilder();
        logs.AppendLine($"=== Pumba ({_config.Name}) STDOUT ===");
        logs.AppendLine(stdout);
        logs.AppendLine($"=== Pumba ({_config.Name}) STDERR ===");
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

    public NetworkImpairmentConfig GetConfig() => _config;

    public string GetTargetContainer() => _targetContainerName;

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Pumba container for target: {Target}", _targetContainerName);
        }
    }
}

public record NetworkImpairmentConfig(
    string Name,
    string? Delay = null,
    string? Jitter = null,
    string? PacketLoss = null,
    string? Description = null)
{
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Delay))
        {
            parts.Add($"delay:{Delay}");
            if (!string.IsNullOrEmpty(Jitter))
                parts.Add($"jitter:{Jitter}");
        }

        if (!string.IsNullOrEmpty(PacketLoss))
            parts.Add($"loss:{PacketLoss}");

        return string.Join(", ", parts);
    }
};