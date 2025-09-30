using SnmpScout.Models;
using SnmpScout.Services;
using SnmpScout.UI;
using Spectre.Console;

namespace SnmpScout;

public class SnmpScoutApp
{
    
    private readonly NetworkScanner _scanner;
    private readonly DeviceManager _deviceManager;
    private readonly UserInterface _ui;

    public SnmpScoutApp()
    {
        _scanner = new NetworkScanner();
        _deviceManager = new DeviceManager();
        _ui = new UserInterface();
    }

    public async Task RunInteractiveAsync()
    {
        _ui.ShowWelcomeScreen();

        while (true)
        {
            var choice = _ui.ShowMainMenu();

            try
            {
                switch (choice)
                {
                    case MainMenuChoice.QuickScan:
                        await PerformQuickScanAsync();
                        break;

                    case MainMenuChoice.CustomScan:
                        await PerformCustomScanAsync();
                        break;

                    case MainMenuChoice.SingleDevice:
                        await ScanSingleDeviceAsync();
                        break;

                    case MainMenuChoice.ViewDevices:
                        await ViewDiscoveredDevicesAsync();
                        break;

                    case MainMenuChoice.ExportResults:
                        await ExportResultsAsync();
                        break;

                    case MainMenuChoice.Settings:
                        _ui.ShowSettings();
                        break;

                    case MainMenuChoice.Exit:
                        _ui.ShowGoodbye();
                        return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                AnsiConsole.MarkupLine("[red]Press any key to continue...[/]");
                Console.ReadKey();
            }
        }
    }

    public async Task RunCommandLineAsync(string? network, string? outputFile)
    {
        if (string.IsNullOrEmpty(network))
        {
            AnsiConsole.MarkupLine("[red]Network range must be specified in command-line mode[/]");
            return;
        }

        AnsiConsole.MarkupLine($"{EmojiHelper.Search} [cyan]Scanning network: {network}[/]");

        var devices = await _scanner.ScanNetworkAsync(network);
        _deviceManager.AddDevices(devices);

        if (!string.IsNullOrEmpty(outputFile))
        {
            await _deviceManager.ExportDevicesAsync(outputFile);
            AnsiConsole.MarkupLine($"{EmojiHelper.Document} [green]Results exported to: {outputFile}[/]");
        }
        else
        {
            _ui.ShowDeviceTable(_deviceManager.GetAllDevices());
        }
    }

    private async Task PerformQuickScanAsync()
    {
        var localNetwork = NetworkUtils.GetLocalNetworkRange();
        AnsiConsole.MarkupLine($"{EmojiHelper.Search} [cyan]Quick scanning local network: {localNetwork}[/]");

        var devices = await _scanner.ScanNetworkAsync(localNetwork);
        _deviceManager.AddDevices(devices);

        _ui.ShowScanResults(devices);
    }

    private async Task PerformCustomScanAsync()
    {
        var network = _ui.GetNetworkRange();
        if (string.IsNullOrEmpty(network)) return;

        var devices = await _scanner.ScanNetworkAsync(network);
        _deviceManager.AddDevices(devices);

        _ui.ShowScanResults(devices);
    }

    private async Task ScanSingleDeviceAsync()
    {
        var ipAddress = _ui.GetIpAddress();
        if (string.IsNullOrEmpty(ipAddress)) return;

        var device = await _scanner.ScanSingleDeviceAsync(ipAddress);
        if (device != null)
        {
            _deviceManager.AddDevice(device);
            await _ui.ShowDeviceDetailsAsync(device);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{EmojiHelper.Cross} Device not found or not responding to SNMP[/]");
        }
    }

    private async Task ViewDiscoveredDevicesAsync()
    {
        var devices = _deviceManager.GetAllDevices();
        if (!devices.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]{EmojiHelper.ExclamationMark}  No devices discovered yet. Run a scan first.[/]");
            return;
        }

        var selectedDevice = _ui.SelectDevice(devices);
        if (selectedDevice != null)
        {
            await _ui.ShowDeviceDetailsAsync(selectedDevice);
        }
    }

    private async Task ExportResultsAsync()
    {
        var devices = _deviceManager.GetAllDevices();
        if (!devices.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]{EmojiHelper.ExclamationMark}  No devices to export. Run a scan first.[/]");
            return;
        }

        var filename = _ui.GetExportFilename();
        if (!string.IsNullOrEmpty(filename))
        {
            await _deviceManager.ExportDevicesAsync(filename);
            AnsiConsole.MarkupLine($"{EmojiHelper.Document} [green]Exported {devices.Count()} devices to {filename}[/]");
        }
    }
}

public enum MainMenuChoice
{
    QuickScan,
    CustomScan,
    SingleDevice,
    ViewDevices,
    ExportResults,
    Settings,
    Exit
}