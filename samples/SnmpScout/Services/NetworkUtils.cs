using System.Net;
using System.Net.NetworkInformation;

namespace SnmpScout.Services;

public static class NetworkUtils
{
    public static string GetLocalNetworkRange()
    {
        try
        {
            var localIp = GetLocalIPAddress();
            if (localIp != null)
            {
                var bytes = localIp.GetAddressBytes();
                // Assume /24 network for simplicity
                return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
            }
        }
        catch
        {
            // Fall back to common default
        }

        return "192.168.1.0/24";
    }

    public static IPAddress? GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(ip) &&
                                     IsPrivateIPAddress(ip));
        }
        catch
        {
            return null;
        }
    }

    public static bool IsPrivateIPAddress(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        // 10.0.0.0 to 10.255.255.255
        if (bytes[0] == 10)
            return true;

        // 172.16.0.0 to 172.31.255.255
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;

        // 192.168.0.0 to 192.168.255.255
        if (bytes[0] == 192 && bytes[1] == 168)
            return true;

        return false;
    }

    public static bool IsValidNetworkRange(string networkRange)
    {
        if (string.IsNullOrWhiteSpace(networkRange))
            return false;

        // Single IP
        if (IPAddress.TryParse(networkRange, out _))
            return true;

        // CIDR notation
        if (networkRange.Contains('/'))
        {
            var parts = networkRange.Split('/');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out _) &&
                int.TryParse(parts[1], out var prefix) &&
                prefix >= 8 && prefix <= 30)
            {
                return true;
            }
        }

        // Range notation
        if (networkRange.Contains('-'))
        {
            var parts = networkRange.Split('-');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out _) &&
                IPAddress.TryParse(parts[1], out _))
            {
                return true;
            }
        }

        return false;
    }

    public static string FormatUptime(TimeSpan? uptime)
    {
        if (!uptime.HasValue)
            return "Unknown";

        var ts = uptime.Value;
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        else if (ts.TotalHours >= 1)
            return $"{ts.Hours}h {ts.Minutes}m";
        else
            return $"{ts.Minutes}m {ts.Seconds}s";
    }

    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static async Task<bool> PingHostAsync(IPAddress address, int timeoutMs = 2000)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(address, timeoutMs);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}