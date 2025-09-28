using nSNMP.Manager;
using SnmpScout.Models;
using Spectre.Console;
using System.Net;

namespace SnmpScout.Services;

public class BandwidthMonitor
{
    private readonly TimeSpan _snmpTimeout = TimeSpan.FromSeconds(2);
    private readonly Dictionary<int, NetworkInterfaceStats> _previousStats = new();

    public async Task<List<NetworkInterfaceStats>> GetInterfaceStatsAsync(NetworkDevice device)
    {
        var stats = new List<NetworkInterfaceStats>();

        try
        {
            var endpoint = new IPEndPoint(device.IpAddress, 161);
            var version = device.SnmpVersion == SnmpVersion.V2c ?
                nSNMP.Message.SnmpVersion.V2c : nSNMP.Message.SnmpVersion.V1;

            using var client = new SnmpClient(endpoint, version, device.Community!, _snmpTimeout);

            // Get interface count first
            var ifNumberResult = await client.GetAsync("1.3.6.1.2.1.2.1.0");
            if (ifNumberResult == null || ifNumberResult.Length == 0)
                return stats;

            var interfaceCount = int.Parse(ifNumberResult[0].Value.ToString() ?? "0");
            if (interfaceCount == 0)
                return stats;

            // Process up to 10 interfaces for performance
            var maxInterfaces = Math.Min(interfaceCount, 10);
            var timestamp = DateTime.UtcNow;

            for (int i = 1; i <= maxInterfaces; i++)
            {
                var interfaceStats = await GetSingleInterfaceStatsAsync(client, i, timestamp);
                if (interfaceStats != null)
                {
                    // Calculate throughput if we have previous measurements
                    if (_previousStats.TryGetValue(i, out var previousStats))
                    {
                        CalculateThroughput(interfaceStats, previousStats);
                    }

                    stats.Add(interfaceStats);
                    _previousStats[i] = interfaceStats;
                }
            }
        }
        catch
        {
            // Ignore errors and return empty stats
        }

        return stats;
    }

    private async Task<NetworkInterfaceStats?> GetSingleInterfaceStatsAsync(SnmpClient client, int ifIndex, DateTime timestamp)
    {
        try
        {
            var stats = new NetworkInterfaceStats { Timestamp = timestamp };

            // Get all interface statistics in parallel
            var tasks = new Dictionary<string, Task<VarBind[]>>
            {
                ["inOctets"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.10.{ifIndex}"),
                ["outOctets"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.16.{ifIndex}"),
                ["inUcastPkts"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.11.{ifIndex}"),
                ["outUcastPkts"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.17.{ifIndex}"),
                ["inNUcastPkts"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.12.{ifIndex}"),
                ["outNUcastPkts"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.18.{ifIndex}"),
                ["inDiscards"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.13.{ifIndex}"),
                ["outDiscards"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.19.{ifIndex}"),
                ["inErrors"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.14.{ifIndex}"),
                ["outErrors"] = client.GetAsync($"1.3.6.1.2.1.2.2.1.20.{ifIndex}")
            };

            await Task.WhenAll(tasks.Values);

            // Parse results
            foreach (var kvp in tasks)
            {
                var result = await kvp.Value;
                if (result?.Length > 0 && long.TryParse(result[0].Value.ToString(), out var value))
                {
                    switch (kvp.Key)
                    {
                        case "inOctets":
                            stats.BytesIn = value;
                            break;
                        case "outOctets":
                            stats.BytesOut = value;
                            break;
                        case "inUcastPkts":
                            stats.PacketsIn += value;
                            break;
                        case "outUcastPkts":
                            stats.PacketsOut += value;
                            break;
                        case "inNUcastPkts":
                            stats.PacketsIn += value; // Add to total packets
                            break;
                        case "outNUcastPkts":
                            stats.PacketsOut += value; // Add to total packets
                            break;
                        case "inDiscards":
                            stats.DiscardsIn = value;
                            break;
                        case "outDiscards":
                            stats.DiscardsOut = value;
                            break;
                        case "inErrors":
                            stats.ErrorsIn = value;
                            break;
                        case "outErrors":
                            stats.ErrorsOut = value;
                            break;
                    }
                }
            }

            return stats;
        }
        catch
        {
            return null;
        }
    }

    private static void CalculateThroughput(NetworkInterfaceStats current, NetworkInterfaceStats previous)
    {
        var timeDelta = current.Timestamp - previous.Timestamp;
        var deltaSeconds = timeDelta.TotalSeconds;

        if (deltaSeconds <= 0) return;

        // Handle counter wrap-around for 32-bit counters
        var deltaInBytes = HandleCounterWrap(current.BytesIn, previous.BytesIn);
        var deltaOutBytes = HandleCounterWrap(current.BytesOut, previous.BytesOut);
        var deltaInPackets = HandleCounterWrap(current.PacketsIn, previous.PacketsIn);
        var deltaOutPackets = HandleCounterWrap(current.PacketsOut, previous.PacketsOut);
        var deltaInErrors = HandleCounterWrap(current.ErrorsIn, previous.ErrorsIn);
        var deltaOutErrors = HandleCounterWrap(current.ErrorsOut, previous.ErrorsOut);

        // Calculate throughput in bits per second
        current.InThroughputBps = (deltaInBytes * 8) / deltaSeconds;
        current.OutThroughputBps = (deltaOutBytes * 8) / deltaSeconds;

        // Calculate packet rates
        current.InPacketsPerSecond = deltaInPackets / deltaSeconds;
        current.OutPacketsPerSecond = deltaOutPackets / deltaSeconds;

        // Calculate error rates
        current.InErrorRate = deltaInErrors / deltaSeconds;
        current.OutErrorRate = deltaOutErrors / deltaSeconds;

        // Calculate utilization (assuming 100 Mbps interface - could be enhanced to get actual speed)
        var interfaceSpeed = 100_000_000.0; // 100 Mbps in bps
        current.InUtilizationPercent = Math.Min((current.InThroughputBps / interfaceSpeed) * 100.0, 100.0);
        current.OutUtilizationPercent = Math.Min((current.OutThroughputBps / interfaceSpeed) * 100.0, 100.0);
        current.TotalUtilizationPercent = Math.Min(((current.InThroughputBps + current.OutThroughputBps) / interfaceSpeed) * 100.0, 100.0);
    }

    private static long HandleCounterWrap(long current, long previous)
    {
        // Handle 32-bit counter wrap-around
        if (current < previous)
        {
            // Counter wrapped - assume 32-bit counter
            return (uint.MaxValue - (uint)previous) + (uint)current + 1;
        }
        return current - previous;
    }

    public static string FormatThroughput(double bitsPerSecond)
    {
        var units = new[] { "bps", "Kbps", "Mbps", "Gbps" };
        var value = bitsPerSecond;
        var unitIndex = 0;

        while (value >= 1000 && unitIndex < units.Length - 1)
        {
            value /= 1000;
            unitIndex++;
        }

        return $"{value:F1} {units[unitIndex]}";
    }

    public void ClearHistory()
    {
        _previousStats.Clear();
    }
}