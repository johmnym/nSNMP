using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using nSNMP.Extensions;
using nSNMP.Manager;

namespace SimpleSnmpGet;

class Program
{
    static async Task Main(string[] args)
    {
        // Parse command line arguments
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: SimpleSnmpGet <host> [community] [oid1] [oid2] ...");
            Console.WriteLine("Example: SimpleSnmpGet 192.168.1.1 public 1.3.6.1.2.1.1.1.0");
            return;
        }

        var host = args[0];
        var community = args.Length > 1 ? args[1] : "public";
        var oids = args.Length > 2 ? args[2..] : new[] { "1.3.6.1.2.1.1.1.0", "1.3.6.1.2.1.1.5.0" };

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Starting Simple SNMP GET example");
        logger.LogInformation("Target: {Host}, Community: {Community}", host, community);
        logger.LogInformation("OIDs to retrieve: {Oids}", string.Join(", ", oids));

        try
        {
            // Create SNMP client
            using var client = nSNMP.Manager.SnmpClient.CreateCommunity(host);

            logger.LogInformation("Performing SNMP GET operation...");

            // Perform GET operation
            var results = await client.GetAsync(oids);

            // Display results
            Console.WriteLine();
            Console.WriteLine("SNMP GET Results:");
            Console.WriteLine("================");

            foreach (var varBind in results)
            {
                Console.WriteLine($"OID: {varBind.Oid}");
                Console.WriteLine($"Value: {varBind.Value}");
                Console.WriteLine($"Type: {varBind.Value.GetType().Name}");
                Console.WriteLine();
            }

            logger.LogInformation("SNMP GET operation completed successfully. Retrieved {Count} values.", results.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SNMP GET operation failed");
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}