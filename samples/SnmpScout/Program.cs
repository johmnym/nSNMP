using SnmpScout;
using SnmpScout.Services;
using Spectre.Console;
using System.CommandLine;

// Create the root command with options
var rootCommand = new RootCommand("🔍 SnmpScout - Network Discovery Tool");

var interactiveOption = new Option<bool>(
    "--interactive",
    getDefaultValue: () => true,
    description: "Run in interactive mode");

var networkOption = new Option<string?>(
    "--network",
    description: "Network range to scan (e.g., 192.168.1.0/24)");

var outputOption = new Option<string?>(
    "--output",
    description: "Output file for results");

rootCommand.AddOption(interactiveOption);
rootCommand.AddOption(networkOption);
rootCommand.AddOption(outputOption);

rootCommand.SetHandler(async (bool interactive, string? network, string? output) =>
{
    try
    {
        var app = new SnmpScoutApp();

        if (interactive)
        {
            await app.RunInteractiveAsync();
        }
        else
        {
            await app.RunCommandLineAsync(network, output);
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex);
        Environment.Exit(1);
    }
}, interactiveOption, networkOption, outputOption);

return await rootCommand.InvokeAsync(args);