using SnmpScout.Models;
using System.Text.Json;

namespace SnmpScout.Services;

public class DeviceManager
{
    private readonly Dictionary<string, NetworkDevice> _devices = new();

    public void AddDevice(NetworkDevice device)
    {
        var key = device.IpAddress.ToString();
        _devices[key] = device;
    }

    public void AddDevices(IEnumerable<NetworkDevice> devices)
    {
        foreach (var device in devices)
        {
            AddDevice(device);
        }
    }

    public IEnumerable<NetworkDevice> GetAllDevices()
    {
        return _devices.Values.OrderBy(d => d.IpAddress.ToString());
    }

    public NetworkDevice? GetDevice(string ipAddress)
    {
        return _devices.TryGetValue(ipAddress, out var device) ? device : null;
    }

    public IEnumerable<NetworkDevice> GetDevicesByType(DeviceType type)
    {
        return _devices.Values.Where(d => d.DeviceType == type);
    }

    public IEnumerable<NetworkDevice> GetOnlineDevices()
    {
        return _devices.Values.Where(d => d.Status == DeviceStatus.Online);
    }

    public async Task ExportDevicesAsync(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();

        switch (extension)
        {
            case ".json":
                await ExportToJsonAsync(filename);
                break;
            case ".csv":
                await ExportToCsvAsync(filename);
                break;
            case ".txt":
                await ExportToTextAsync(filename);
                break;
            default:
                throw new ArgumentException($"Unsupported file format: {extension}");
        }
    }

    private async Task ExportToJsonAsync(string filename)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(_devices.Values, options);
        await File.WriteAllTextAsync(filename, json);
    }

    private async Task ExportToCsvAsync(string filename)
    {
        var lines = new List<string>
        {
            "IP Address,Hostname,System Name,Device Type,Status,SNMP Version,Community,System Description,Location,Contact,Uptime"
        };

        foreach (var device in _devices.Values)
        {
            var line = string.Join(",",
                EscapeCsv(device.IpAddress.ToString()),
                EscapeCsv(device.HostName ?? ""),
                EscapeCsv(device.SystemName ?? ""),
                EscapeCsv(device.DeviceType.ToString()),
                EscapeCsv(device.Status.ToString()),
                EscapeCsv(device.SnmpVersion.ToString()),
                EscapeCsv(device.Community ?? ""),
                EscapeCsv(device.SystemDescription ?? ""),
                EscapeCsv(device.SystemLocation ?? ""),
                EscapeCsv(device.SystemContact ?? ""),
                EscapeCsv(device.SystemUptime?.ToString() ?? "")
            );
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filename, lines);
    }

    private async Task ExportToTextAsync(string filename)
    {
        var lines = new List<string>
        {
            "SNMP Scout Network Discovery Report",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Total Devices: {_devices.Count}",
            new string('=', 50),
            ""
        };

        foreach (var device in _devices.Values.OrderBy(d => d.IpAddress.ToString()))
        {
            lines.AddRange(new[]
            {
                $"IP Address: {device.IpAddress}",
                $"Display Name: {device.DisplayName}",
                $"Device Type: {device.DeviceType} {device.TypeEmoji}",
                $"Status: {device.Status} {device.StatusEmoji}",
                $"SNMP Version: {device.SnmpVersion}",
                $"Community: {device.Community ?? "N/A"}",
                $"System Description: {device.SystemDescription ?? "N/A"}",
                $"Location: {device.SystemLocation ?? "N/A"}",
                $"Contact: {device.SystemContact ?? "N/A"}",
                $"Uptime: {device.SystemUptime?.ToString(@"dd\.hh\:mm\:ss") ?? "N/A"}",
                $"Interfaces: {device.Interfaces.Count}",
                $"Last Seen: {device.LastSeen:yyyy-MM-dd HH:mm:ss} UTC",
                new string('-', 30),
                ""
            });
        }

        await File.WriteAllLinesAsync(filename, lines);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public void ClearDevices()
    {
        _devices.Clear();
    }

    public int DeviceCount => _devices.Count;

    public Dictionary<DeviceType, int> GetDeviceTypeStats()
    {
        return _devices.Values
            .GroupBy(d => d.DeviceType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<DeviceStatus, int> GetDeviceStatusStats()
    {
        return _devices.Values
            .GroupBy(d => d.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}