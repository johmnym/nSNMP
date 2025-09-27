using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SnmpTrapReceiver;

class Program
{
    static async Task Main(string[] args)
    {
        var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 162;

        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Register services
        builder.Services.AddSingleton<TrapReceiverService>();
        builder.Services.AddHostedService<TrapReceiverService>(provider =>
            provider.GetRequiredService<TrapReceiverService>());

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting SNMP Trap Receiver on port {Port}", port);
        logger.LogInformation("Press Ctrl+C to stop the receiver");

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application terminated unexpectedly");
            Environment.Exit(1);
        }
    }
}