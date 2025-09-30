using System.Runtime.InteropServices;
using System.Text;

namespace SnmpScout.UI;

public static class EmojiHelper
{
    private static readonly bool _supportsEmoji;

    static EmojiHelper()
    {
        // Windows Terminal, PowerShell 7+, and Windows 11 support emojis well
        // Check for Windows Terminal via environment variable
        var termProgram = Environment.GetEnvironmentVariable("WT_SESSION");
        var terminalEmulator = Environment.GetEnvironmentVariable("TERM_PROGRAM");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows Terminal or modern terminal detected
            _supportsEmoji = !string.IsNullOrEmpty(termProgram) ||
                           !string.IsNullOrEmpty(terminalEmulator) ||
                           Environment.OSVersion.Version.Build >= 22000; // Windows 11

            // Try to set UTF-8 encoding for better emoji support
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                _supportsEmoji = false;
            }
        }
        else
        {
            // macOS and Linux generally support emojis in their default terminals
            _supportsEmoji = true;
        }
    }

    public static string GetEmoji(string emoji, string fallback)
    {
        return _supportsEmoji ? emoji : fallback;
    }

    // Status emojis
    public static string Online => GetEmoji("🟢", "[+]");
    public static string Offline => GetEmoji("🔴", "[-]");
    public static string Warning => GetEmoji("🟡", "[!]");
    public static string Unknown => GetEmoji("⚪", "[?]");

    // Device type emojis
    public static string Router => GetEmoji("🔀", "[R]");
    public static string Switch => GetEmoji("🔗", "[S]");
    public static string AccessPoint => GetEmoji("📶", "[AP]");
    public static string Printer => GetEmoji("🖨️", "[P]");
    public static string Server => GetEmoji("🖥️", "[SRV]");
    public static string UPS => GetEmoji("🔋", "[UPS]");
    public static string Camera => GetEmoji("📹", "[CAM]");
    public static string Phone => GetEmoji("📞", "[PH]");
    public static string Storage => GetEmoji("💾", "[NAS]");
    public static string QuestionMark => GetEmoji("❓", "[?]");

    // Action emojis
    public static string Search => GetEmoji("🔍", ">>");
    public static string Target => GetEmoji("🎯", "[]");
    public static string Computer => GetEmoji("🖥️", "[PC]");
    public static string List => GetEmoji("📋", "==");
    public static string Document => GetEmoji("📄", "[]");
    public static string Settings => GetEmoji("⚙️", "[*]");
    public static string Exit => GetEmoji("🚪", "[X]");
    public static string CheckMark => GetEmoji("✅", "[OK]");
    public static string Cross => GetEmoji("❌", "[X]");
    public static string ExclamationMark => GetEmoji("⚠️", "[!]");
    public static string Chart => GetEmoji("📊", "[#]");
    public static string Refresh => GetEmoji("🔄", "[@]");
    public static string Back => GetEmoji("🔙", "[<]");
    public static string Home => GetEmoji("🏠", "[H]");
    public static string Network => GetEmoji("🔗", "[N]");
    public static string Info => GetEmoji("📝", "[i]");
    public static string Rocket => GetEmoji("🚀", ">>>");
    public static string Summary => GetEmoji("📈", "[S]");
    public static string Up => GetEmoji("↑", "^");
    public static string Down => GetEmoji("↓", "v");
    public static string Sparkles => GetEmoji("✨", "**");
}