using SnmpScout.Models;
using SnmpScout.Services;
using Spectre.Console;
using System.Net;

namespace SnmpScout.UI;

public class UserInterface
{
    public void ShowWelcomeScreen()
    {
        AnsiConsole.Clear();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(new FigletText("SnmpScout").Centered().Color(Color.Cyan1));
        grid.AddRow(Align.Center(new Panel($"[bold cyan1]{EmojiHelper.Search} Network Discovery & SNMP Management Tool[/]")
            .BorderColor(Color.Cyan1)));

        AnsiConsole.Write(grid);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    public MainMenuChoice ShowMainMenu()
    {
        AnsiConsole.Clear();

        var panel = new Panel(
            $"[bold cyan1]{EmojiHelper.Search} SnmpScout - Main Menu[/]\n\n" +
            "Discover and manage SNMP-enabled devices on your network\n\n" +
            "[dim]Use arrow keys to navigate, Enter to select[/]"
        ).BorderColor(Color.Cyan1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan1]What would you like to do?[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    $"{EmojiHelper.Search} Quick Scan (Local Network)",
                    $"{EmojiHelper.Target} Custom Network Scan",
                    $"{EmojiHelper.Computer}  Single Device Details",
                    $"{EmojiHelper.List} View Discovered Devices",
                    $"{EmojiHelper.Document} Export Results",
                    $"{EmojiHelper.Settings}  Settings",
                    $"{EmojiHelper.Exit} Exit"
                }));

        return choice switch
        {
            var s when s.Contains("Quick Scan") => MainMenuChoice.QuickScan,
            var s when s.Contains("Custom Network Scan") => MainMenuChoice.CustomScan,
            var s when s.Contains("Single Device Details") => MainMenuChoice.SingleDevice,
            var s when s.Contains("View Discovered Devices") => MainMenuChoice.ViewDevices,
            var s when s.Contains("Export Results") => MainMenuChoice.ExportResults,
            var s when s.Contains("Settings") => MainMenuChoice.Settings,
            var s when s.Contains("Exit") => MainMenuChoice.Exit,
            _ => MainMenuChoice.Exit
        };
    }

    public void ShowScanResults(IEnumerable<NetworkDevice> devices)
    {
        var deviceList = devices.ToList();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold green]{EmojiHelper.CheckMark} Scan completed! Found {deviceList.Count} devices[/]");

        if (deviceList.Any())
        {
            ShowDeviceTable(deviceList);
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]{EmojiHelper.ExclamationMark}  No SNMP-enabled devices found.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
        Console.ReadKey();
    }

    public void ShowDeviceTable(IEnumerable<NetworkDevice> devices)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Cyan1);

        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]IP Address[/]");
        table.AddColumn("[bold]Device Name[/]");
        table.AddColumn("[bold]Vendor[/]");
        table.AddColumn("[bold]Model[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Ver[/]");
        table.AddColumn("[bold]Uptime[/]");

        foreach (var device in devices.OrderBy(d => d.IpAddress.ToString()))
        {
            var status = device.Status == DeviceStatus.Online ?
                $"[green]{device.StatusEmoji}[/]" :
                $"[red]{device.StatusEmoji}[/]";

            var deviceType = $"{device.TypeEmoji} {device.DeviceType}";
            var uptime = NetworkUtils.FormatUptime(device.SystemUptime);
            var vendor = !string.IsNullOrEmpty(device.Vendor) ? device.Vendor : "[dim]Unknown[/]";
            var model = !string.IsNullOrEmpty(device.Model) ? device.Model : "[dim]Unknown[/]";

            table.AddRow(
                status,
                $"[bold]{device.IpAddress}[/]",
                $"[yellow]{device.DisplayName}[/]",
                $"[cyan]{vendor}[/]",
                $"[cyan]{model}[/]",
                deviceType,
                $"[dim]{device.SnmpVersion}[/]",
                $"[dim]{uptime}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    public async Task ShowDeviceDetailsAsync(NetworkDevice device)
    {
        AnsiConsole.Clear();

        var devicePanel = new Panel(
            $"[bold cyan1]{device.TypeEmoji} {device.DisplayName}[/]\n" +
            $"[dim]{device.IpAddress} • {device.DeviceType} • {device.StatusEmoji} {device.Status}[/]"
        ).BorderColor(Color.Cyan1);

        AnsiConsole.Write(devicePanel);
        AnsiConsole.WriteLine();

        // System Information
        var systemTable = new Table();
        systemTable.Border(TableBorder.Rounded);
        systemTable.BorderColor(Color.Green);
        systemTable.Title($"[bold green]{EmojiHelper.Chart} System Information[/]");
        systemTable.AddColumn("[bold]Property[/]");
        systemTable.AddColumn("[bold]Value[/]");

        systemTable.AddRow("IP Address", device.IpAddress.ToString());
        systemTable.AddRow("Hostname", device.HostName ?? "[dim]Not available[/]");
        systemTable.AddRow("System Name", device.SystemName ?? "[dim]Not available[/]");
        systemTable.AddRow("Vendor", device.Vendor ?? "[dim]Unknown[/]");
        systemTable.AddRow("Model", device.Model ?? "[dim]Unknown[/]");
        systemTable.AddRow("Device Type", $"{device.TypeEmoji} {device.DeviceType}");
        systemTable.AddRow("Status", $"{device.StatusEmoji} {device.Status}");
        systemTable.AddRow("SNMP Version", device.SnmpVersion.ToString());
        systemTable.AddRow("Community", device.Community ?? "[dim]Not available[/]");
        systemTable.AddRow("System OID", device.SystemObjectID ?? "[dim]Not available[/]");
        systemTable.AddRow("System Uptime", NetworkUtils.FormatUptime(device.SystemUptime));
        systemTable.AddRow("Location", device.SystemLocation ?? "[dim]Not specified[/]");
        systemTable.AddRow("Contact", device.SystemContact ?? "[dim]Not specified[/]");
        systemTable.AddRow("Last Seen", device.LastSeen.ToString("yyyy-MM-dd HH:mm:ss UTC"));

        AnsiConsole.Write(systemTable);
        AnsiConsole.WriteLine();

        // System Description
        if (!string.IsNullOrEmpty(device.SystemDescription))
        {
            var descPanel = new Panel(device.SystemDescription)
                .Header($"[bold yellow]{EmojiHelper.Info} System Description[/]")
                .BorderColor(Color.Yellow);
            AnsiConsole.Write(descPanel);
            AnsiConsole.WriteLine();
        }

        // Network Interfaces
        if (device.Interfaces.Any())
        {
            var interfaceTable = new Table();
            interfaceTable.Border(TableBorder.Rounded);
            interfaceTable.BorderColor(Color.Blue);
            interfaceTable.Title($"[bold blue]{EmojiHelper.Network} Network Interfaces[/]");
            interfaceTable.AddColumn("[bold]Index[/]");
            interfaceTable.AddColumn("[bold]Description[/]");
            interfaceTable.AddColumn("[bold]Status[/]");
            interfaceTable.AddColumn("[bold]Type[/]");

            foreach (var iface in device.Interfaces.Take(10)) // Limit display
            {
                var statusColor = iface.Status == InterfaceStatus.Up ? "green" : "red";
                interfaceTable.AddRow(
                    iface.Index.ToString(),
                    iface.Description ?? "[dim]Unknown[/]",
                    $"[{statusColor}]{iface.Status}[/]",
                    iface.Type.ToString()
                );
            }

            AnsiConsole.Write(interfaceTable);
            AnsiConsole.WriteLine();
        }

        // Options
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan1]What would you like to do?[/]")
                .AddChoices(new[]
                {
                    $"{EmojiHelper.Refresh} Refresh Device Info",
                    $"{EmojiHelper.Chart} Bandwidth Monitor",
                    $"{EmojiHelper.Search} SNMP Walk (Advanced)",
                    $"{EmojiHelper.Back} Back to Device List",
                    $"{EmojiHelper.Home} Main Menu"
                }));

        switch (action)
        {
            case var a when a.Contains("Refresh"):
                await RefreshDeviceAsync(device);
                await ShowDeviceDetailsAsync(device);
                break;
            case var a when a.Contains("Bandwidth"):
                await ShowBandwidthMonitorAsync(device);
                await ShowDeviceDetailsAsync(device);
                break;
            case var a when a.Contains("SNMP Walk"):
                await ShowSnmpWalkAsync(device);
                await ShowDeviceDetailsAsync(device);
                break;
            case var a when a.Contains("Back") || a.Contains("Main Menu"):
                break;
        }
    }

    private async Task RefreshDeviceAsync(NetworkDevice device)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Refreshing {device.DisplayName}...", async ctx =>
            {
                var scanner = new NetworkScanner();
                var refreshed = await scanner.ScanSingleDeviceAsync(device.IpAddress.ToString());

                if (refreshed != null)
                {
                    // Update device properties
                    device.SystemName = refreshed.SystemName;
                    device.SystemDescription = refreshed.SystemDescription;
                    device.SystemLocation = refreshed.SystemLocation;
                    device.SystemContact = refreshed.SystemContact;
                    device.SystemUptime = refreshed.SystemUptime;
                    device.Status = refreshed.Status;
                    device.LastSeen = DateTime.UtcNow;
                    device.Interfaces = refreshed.Interfaces;

                    ctx.Status("Refresh completed!");
                    await Task.Delay(1000);
                }
            });
    }

    private async Task ShowSnmpWalkAsync(NetworkDevice device)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold yellow]{EmojiHelper.Search} SNMP Walk: {device.DisplayName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Leave empty and press Enter to cancel[/]");

        var oid = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter OID to walk (e.g., [green]1.3.6.1.2.1.1[/]):")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(oid))
        {
            return; // User cancelled
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Walking SNMP tree...", async ctx =>
            {
                try
                {
                    var endpoint = new IPEndPoint(device.IpAddress, 161);
                    var version = device.SnmpVersion == SnmpVersion.V2c ?
                        nSNMP.Message.SnmpVersion.V2c : nSNMP.Message.SnmpVersion.V1;

                    using var client = new nSNMP.Manager.SnmpClient(endpoint, version, device.Community!, TimeSpan.FromSeconds(5));

                    // Simple walk implementation - get next values
                    var results = new List<(string Oid, string Value)>();
                    var currentOid = oid;

                    for (int i = 0; i < 20; i++) // Limit to 20 results
                    {
                        try
                        {
                            var result = await client.GetNextAsync(currentOid);
                            if (result?.Length > 0)
                            {
                                var resultOid = result[0].Oid.ToString();
                                var value = result[0].Value.ToString();

                                if (!resultOid.StartsWith(oid))
                                    break; // Out of subtree

                                results.Add((resultOid, value ?? ""));
                                currentOid = resultOid;
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }

                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine($"[bold yellow]{EmojiHelper.Search} SNMP Walk Results: {device.DisplayName}[/]");
                    AnsiConsole.MarkupLine($"[dim]Base OID: {oid}[/]");
                    AnsiConsole.WriteLine();

                    if (results.Any())
                    {
                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn("[bold]OID[/]");
                        table.AddColumn("[bold]Value[/]");

                        foreach (var (resultOid, value) in results)
                        {
                            table.AddRow(
                                $"[cyan]{resultOid}[/]",
                                $"[yellow]{value}[/]"
                            );
                        }

                        AnsiConsole.Write(table);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]No results found for the specified OID.[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to return to device details...[/]");
        Console.ReadKey();
    }

    public NetworkDevice? SelectDevice(IEnumerable<NetworkDevice> devices)
    {
        var deviceList = devices.ToList();
        if (!deviceList.Any())
            return null;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold cyan1]{EmojiHelper.List} Discovered Devices[/]");
        AnsiConsole.MarkupLine("[dim]Select a device to view details or go back[/]");
        AnsiConsole.WriteLine();

        var choices = deviceList.Select(d =>
            $"{d.StatusEmoji} {d.IpAddress} - {d.DisplayName} ({d.TypeEmoji} {d.DeviceType})"
        ).ToList();
        choices.Add($"{EmojiHelper.Back} Back to Main Menu");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan1]Select a device:[/]")
                .PageSize(15)
                .AddChoices(choices));

        if (choice.Contains("Back to Main Menu"))
            return null;

        var selectedIndex = choices.IndexOf(choice);
        return selectedIndex >= 0 && selectedIndex < deviceList.Count ? deviceList[selectedIndex] : null;
    }

    public string? GetNetworkRange()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Leave empty and press Enter to cancel[/]");
        var network = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter network range (e.g., [green]192.168.1.0/24[/] or [green]10.0.0.1-10.0.0.100[/]):")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(network))
            return null;

        if (!NetworkUtils.IsValidNetworkRange(network))
        {
            AnsiConsole.MarkupLine("[red]Invalid network range format.[/]");
            return null;
        }

        return network;
    }

    public string? GetIpAddress()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Leave empty and press Enter to cancel[/]");
        var ip = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter IP address:")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(ip))
            return null;

        if (!IPAddress.TryParse(ip, out _))
        {
            AnsiConsole.MarkupLine("[red]Invalid IP address format.[/]");
            return null;
        }

        return ip;
    }

    public string? GetExportFilename()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Leave empty and press Enter to cancel[/]");
        var filename = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter filename ([green].json[/], [green].csv[/], or [green].txt[/]):")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(filename))
            return null;

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension != ".json" && extension != ".csv" && extension != ".txt")
        {
            AnsiConsole.MarkupLine("[red]Unsupported file format. Use .json, .csv, or .txt[/]");
            return null;
        }

        return filename;
    }

    public void ShowSettings()
    {
        AnsiConsole.Clear();
        var panel = new Panel(
            $"[bold yellow]{EmojiHelper.Settings}  Settings[/]\n\n" +
            "• SNMP Timeout: 2 seconds\n" +
            "• Max Concurrent Scans: 20\n" +
            "• Supported SNMP Versions: v1, v2c\n" +
            "• Common Communities: public, private, admin, manager, snmp\n" +
            "• Export Formats: JSON, CSV, Text\n\n" +
            "[dim]Settings are currently read-only in this version.[/]"
        ).BorderColor(Color.Yellow);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
        Console.ReadKey();
    }

    public void ShowGoodbye()
    {
        AnsiConsole.Clear();

        var panel = new Panel(
            $"[bold cyan1]Thank you for using SnmpScout![/]\n\n" +
            $"{EmojiHelper.Search} Happy network discovery! {EmojiHelper.Rocket}"
        ).BorderColor(Color.Cyan1);

        AnsiConsole.Write(Align.Center(panel));
        AnsiConsole.WriteLine();
    }

    private async Task ShowBandwidthMonitorAsync(NetworkDevice device)
    {
        var monitor = new BandwidthMonitor();
        var running = true;
        var updateCount = 0;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold blue]{EmojiHelper.Chart} Bandwidth Monitor: {device.DisplayName}[/]");
        AnsiConsole.MarkupLine("[dim]Real-time network interface monitoring - Press any key to stop[/]");
        AnsiConsole.WriteLine();

        // Start monitoring in background
        var monitoringTask = Task.Run(async () =>
        {
            while (running)
            {
                try
                {
                    var stats = await monitor.GetInterfaceStatsAsync(device);
                    if (stats.Any())
                    {
                        updateCount++;
                        DisplayBandwidthStats(stats, updateCount);
                    }
                    await Task.Delay(3000); // Update every 3 seconds
                }
                catch
                {
                    // Continue monitoring even if there are errors
                }
            }
        });

        // Wait for user input to stop
        var inputTask = Task.Run(() => Console.ReadKey());
        await Task.WhenAny(inputTask, Task.Delay(60000)); // Stop after 1 minute max

        running = false;
        await monitoringTask;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[green]{EmojiHelper.Chart} Bandwidth monitoring stopped.[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to return to device details...[/]");
        Console.ReadKey();
    }

    private void DisplayBandwidthStats(List<NetworkInterfaceStats> stats, int updateCount)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold blue]{EmojiHelper.Chart} Bandwidth Monitor - Update #{updateCount}[/]");
        AnsiConsole.MarkupLine($"[dim]Last updated: {DateTime.Now:HH:mm:ss} - Press any key to stop[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Blue);
        table.Title($"[bold blue]{EmojiHelper.Chart} Interface Statistics[/]");

        table.AddColumn("[bold]Interface[/]");
        table.AddColumn("[bold]Throughput In[/]");
        table.AddColumn("[bold]Throughput Out[/]");
        table.AddColumn("[bold]Utilization[/]");
        table.AddColumn("[bold]Packets/sec[/]");
        table.AddColumn("[bold]Errors/sec[/]");

        for (int i = 0; i < stats.Count; i++)
        {
            var stat = stats[i];
            var interfaceIndex = i + 1;

            // Color code utilization
            var utilizationColor = stat.TotalUtilizationPercent switch
            {
                > 80 => "red",
                > 60 => "yellow",
                > 30 => "green",
                _ => "dim"
            };

            // Format throughput values
            var inThroughput = BandwidthMonitor.FormatThroughput(stat.InThroughputBps);
            var outThroughput = BandwidthMonitor.FormatThroughput(stat.OutThroughputBps);
            var utilization = $"[{utilizationColor}]{stat.TotalUtilizationPercent:F1}%[/]";
            var packetsPerSec = $"{stat.InPacketsPerSecond:F0}{EmojiHelper.Down} / {stat.OutPacketsPerSecond:F0}{EmojiHelper.Up}";
            var errorsPerSec = $"{stat.InErrorRate:F2}{EmojiHelper.Down} / {stat.OutErrorRate:F2}{EmojiHelper.Up}";

            table.AddRow(
                $"eth{interfaceIndex}",
                $"[green]{inThroughput}[/]",
                $"[cyan]{outThroughput}[/]",
                utilization,
                packetsPerSec,
                errorsPerSec
            );
        }

        AnsiConsole.Write(table);

        // Show summary stats
        var totalInThroughput = stats.Sum(s => s.InThroughputBps);
        var totalOutThroughput = stats.Sum(s => s.OutThroughputBps);
        var avgUtilization = stats.Any() ? stats.Average(s => s.TotalUtilizationPercent) : 0;

        AnsiConsole.WriteLine();
        var summaryPanel = new Panel(
            $"[bold]Total Throughput:[/] [green]{BandwidthMonitor.FormatThroughput(totalInThroughput)}[/] {EmojiHelper.Down} / [cyan]{BandwidthMonitor.FormatThroughput(totalOutThroughput)}[/] {EmojiHelper.Up}\n" +
            $"[bold]Average Utilization:[/] {avgUtilization:F1}%\n" +
            $"[bold]Active Interfaces:[/] {stats.Count}"
        ).Header($"[bold yellow]{EmojiHelper.Summary} Summary[/]").BorderColor(Color.Yellow);

        AnsiConsole.Write(summaryPanel);
    }
}