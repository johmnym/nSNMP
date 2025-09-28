using nSNMP.Manager;
using nSNMP.SMI.DataTypes.V1.Primitive;
using SnmpScout.Models;
using Spectre.Console;
using System.Net;
using System.Net.NetworkInformation;
using SystemNetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace SnmpScout.Services;

public class NetworkScanner
{
    private readonly string[] _commonCommunities = { "public", "private", "admin", "manager", "snmp" };
    private readonly TimeSpan _snmpTimeout = TimeSpan.FromSeconds(2);

    public async Task<IEnumerable<NetworkDevice>> ScanNetworkAsync(string networkRange)
    {
        var ipRange = ParseNetworkRange(networkRange);
        var devices = new List<NetworkDevice>();

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var scanTask = ctx.AddTask($"🔍 Scanning {ipRange.Count} addresses", maxValue: ipRange.Count);

                var semaphore = new SemaphoreSlim(20); // Limit concurrent scans
                var tasks = ipRange.Select(async ip =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var device = await ScanSingleDeviceAsync(ip.ToString());
                        if (device != null)
                        {
                            devices.Add(device);
                            AnsiConsole.MarkupLine($"✅ [green]Found: {device.DisplayName} ({ip})[/]");
                        }
                        scanTask.Increment(1);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            });

        return devices.OrderBy(d => d.IpAddress.ToString());
    }

    public async Task<NetworkDevice?> ScanSingleDeviceAsync(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return null;

        // First, check if device is reachable via ping
        var ping = new Ping();
        try
        {
            var pingReply = await ping.SendPingAsync(ip, 1000);
            if (pingReply.Status != IPStatus.Success)
                return null;
        }
        catch
        {
            return null;
        }

        // Try SNMP discovery
        var device = new NetworkDevice
        {
            IpAddress = ip,
            Status = DeviceStatus.Online
        };

        // Try to resolve hostname
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ip);
            device.HostName = hostEntry.HostName;
        }
        catch
        {
            // Ignore DNS resolution failures
        }

        // Try SNMP with different communities and versions
        if (await TrySnmpDiscovery(device))
        {
            await EnrichDeviceInfo(device);
            return device;
        }

        return null;
    }

    private async Task<bool> TrySnmpDiscovery(NetworkDevice device)
    {
        // Try SNMPv2c first, then v1
        var versions = new[] { nSNMP.Message.SnmpVersion.V2c, nSNMP.Message.SnmpVersion.V1 };

        foreach (var version in versions)
        {
            foreach (var community in _commonCommunities)
            {
                try
                {
                    var endpoint = new IPEndPoint(device.IpAddress, 161);
                    using var client = new SnmpClient(endpoint, version, community, _snmpTimeout);

                    // Try to get system description (1.3.6.1.2.1.1.1.0)
                    var result = await client.GetAsync("1.3.6.1.2.1.1.1.0");
                    if (result != null && result.Length > 0)
                    {
                        device.SnmpVersion = version == nSNMP.Message.SnmpVersion.V2c ?
                            SnmpVersion.V2c : SnmpVersion.V1;
                        device.Community = community;
                        return true;
                    }
                }
                catch
                {
                    // Continue trying other combinations
                }
            }
        }

        return false;
    }

    private async Task EnrichDeviceInfo(NetworkDevice device)
    {
        try
        {
            var endpoint = new IPEndPoint(device.IpAddress, 161);
            var version = device.SnmpVersion == SnmpVersion.V2c ?
                nSNMP.Message.SnmpVersion.V2c : nSNMP.Message.SnmpVersion.V1;

            using var client = new SnmpClient(endpoint, version, device.Community!, _snmpTimeout);

            // Get system information
            var systemOids = new Dictionary<string, string>
            {
                ["1.3.6.1.2.1.1.1.0"] = "sysDescr",     // System Description
                ["1.3.6.1.2.1.1.5.0"] = "sysName",      // System Name
                ["1.3.6.1.2.1.1.6.0"] = "sysLocation",  // System Location
                ["1.3.6.1.2.1.1.4.0"] = "sysContact",   // System Contact
                ["1.3.6.1.2.1.1.3.0"] = "sysUpTime"     // System Uptime
            };

            foreach (var kvp in systemOids)
            {
                try
                {
                    var result = await client.GetAsync(kvp.Key);
                    if (result != null && result.Length > 0)
                    {
                        var value = result[0].Value.ToString();
                        switch (kvp.Value)
                        {
                            case "sysDescr":
                                device.SystemDescription = value;
                                device.DeviceType = DetermineDeviceType(value ?? "");
                                break;
                            case "sysName":
                                device.SystemName = value;
                                break;
                            case "sysLocation":
                                device.SystemLocation = value;
                                break;
                            case "sysContact":
                                device.SystemContact = value;
                                break;
                            case "sysUpTime":
                                if (uint.TryParse(value, out var ticks))
                                {
                                    device.SystemUptime = TimeSpan.FromMilliseconds(ticks * 10);
                                }
                                break;
                        }
                    }
                }
                catch
                {
                    // Continue if individual OID fails
                }
            }

            // Get interface information
            await GetInterfaceInfo(client, device);
        }
        catch
        {
            // If enrichment fails, device is still valid with basic info
        }
    }

    private async Task GetInterfaceInfo(SnmpClient client, NetworkDevice device)
    {
        try
        {
            // Get interface count
            var ifNumberResult = await client.GetAsync("1.3.6.1.2.1.2.1.0");
            if (ifNumberResult == null || ifNumberResult.Length == 0) return;

            var interfaceCount = int.Parse(ifNumberResult[0].Value.ToString() ?? "0");
            if (interfaceCount == 0) return;

            // Walk interface table (limited to first 10 interfaces for performance)
            var maxInterfaces = Math.Min(interfaceCount, 10);
            for (int i = 1; i <= maxInterfaces; i++)
            {
                var netInterface = new Models.NetworkInterface { Index = i };

                try
                {
                    // Interface description
                    var descResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.2.{i}");
                    if (descResult?.Length > 0)
                        netInterface.Description = descResult[0].Value.ToString();

                    // Interface status
                    var statusResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.8.{i}");
                    if (statusResult?.Length > 0 && int.TryParse(statusResult[0].Value.ToString(), out var status))
                        netInterface.Status = (InterfaceStatus)status;

                    device.Interfaces.Add(netInterface);
                }
                catch
                {
                    // Skip this interface if it fails
                }
            }
        }
        catch
        {
            // Interface discovery is optional
        }
    }

    private static DeviceType DetermineDeviceType(string sysDescr)
    {
        var description = sysDescr.ToLowerInvariant();

        return description switch
        {
            var d when d.Contains("router") => DeviceType.Router,
            var d when d.Contains("switch") => DeviceType.Switch,
            var d when d.Contains("access point") || d.Contains("wireless") => DeviceType.AccessPoint,
            var d when d.Contains("printer") => DeviceType.Printer,
            var d when d.Contains("server") => DeviceType.Server,
            var d when d.Contains("ups") || d.Contains("power") => DeviceType.UPS,
            var d when d.Contains("camera") => DeviceType.Camera,
            var d when d.Contains("phone") => DeviceType.Phone,
            var d when d.Contains("storage") || d.Contains("nas") => DeviceType.Storage,
            var d when d.Contains("firewall") => DeviceType.Firewall,
            var d when d.Contains("load balancer") => DeviceType.LoadBalancer,
            _ => DeviceType.Unknown
        };
    }

    private static List<IPAddress> ParseNetworkRange(string networkRange)
    {
        var addresses = new List<IPAddress>();

        if (networkRange.Contains('/'))
        {
            // CIDR notation (e.g., 192.168.1.0/24)
            var parts = networkRange.Split('/');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var network) &&
                int.TryParse(parts[1], out var prefixLength))
            {
                addresses.AddRange(GetCidrRange(network, prefixLength));
            }
        }
        else if (networkRange.Contains('-'))
        {
            // Range notation (e.g., 192.168.1.1-192.168.1.100)
            var parts = networkRange.Split('-');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var start) &&
                IPAddress.TryParse(parts[1], out var end))
            {
                addresses.AddRange(GetIpRange(start, end));
            }
        }
        else if (IPAddress.TryParse(networkRange, out var singleIp))
        {
            // Single IP address
            addresses.Add(singleIp);
        }

        return addresses;
    }

    private static IEnumerable<IPAddress> GetCidrRange(IPAddress network, int prefixLength)
    {
        var addresses = new List<IPAddress>();
        var networkBytes = network.GetAddressBytes();
        var hostBits = 32 - prefixLength;
        var hostCount = (1 << hostBits) - 2; // Exclude network and broadcast

        for (int i = 1; i <= hostCount && i <= 254; i++) // Limit to 254 for performance
        {
            var hostBytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(hostBytes);

            var addressBytes = new byte[4];
            for (int j = 0; j < 4; j++)
            {
                addressBytes[j] = (byte)(networkBytes[j] | hostBytes[j]);
            }

            addresses.Add(new IPAddress(addressBytes));
        }

        return addresses;
    }

    private static IEnumerable<IPAddress> GetIpRange(IPAddress start, IPAddress end)
    {
        var addresses = new List<IPAddress>();
        var startBytes = start.GetAddressBytes();
        var endBytes = end.GetAddressBytes();

        var startInt = BitConverter.ToUInt32(startBytes.Reverse().ToArray(), 0);
        var endInt = BitConverter.ToUInt32(endBytes.Reverse().ToArray(), 0);

        for (uint i = startInt; i <= endInt && addresses.Count < 254; i++)
        {
            var bytes = BitConverter.GetBytes(i).Reverse().ToArray();
            addresses.Add(new IPAddress(bytes));
        }

        return addresses;
    }
}