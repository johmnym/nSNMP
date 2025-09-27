# 🔍 SnmpScout - Network Discovery Tool

**SnmpScout** is a powerful console-based network discovery and SNMP management tool built using the **nSNMP** library. It demonstrates real-world usage of nSNMP for network device discovery, monitoring, and management.

## ✨ Features

### 🚀 **Network Discovery**
- **Quick Scan**: Automatically discover devices on your local network
- **Custom Range Scan**: Scan specific network ranges (CIDR or IP ranges)
- **Single Device Details**: Deep dive into individual device information
- **Multi-threaded Scanning**: Fast concurrent device discovery
- **SNMP Version Detection**: Automatic fallback from v2c to v1

### 📊 **Device Management**
- **Device Categorization**: Automatic detection of routers, switches, printers, etc.
- **Real-time Status**: Live device status monitoring
- **System Information**: Comprehensive device details
- **Interface Discovery**: Network interface enumeration
- **SNMP Walking**: Advanced OID browsing capabilities

### 💾 **Export & Reporting**
- **Multiple Formats**: Export to JSON, CSV, or Text
- **Detailed Reports**: Complete device inventory reports
- **Statistics**: Device type and status summaries

## 🎨 **Beautiful Console UI**

SnmpScout features a modern, colorful console interface powered by Spectre.Console:
- 🌈 **Color-coded output** (green=online, red=offline, yellow=warning)
- 📊 **Progress bars** during scanning
- 📋 **Interactive tables** for device listings
- 🎯 **Menu-driven navigation**
- ✨ **ASCII art and styling**

## 🛠 **Built with nSNMP**

SnmpScout showcases the power and simplicity of the nSNMP library:

```csharp
// Simple SNMP client creation
using var client = new SnmpClient(endpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(2));

// Get system information
var sysDescr = await client.GetAsync("1.3.6.1.2.1.1.1.0");
var sysName = await client.GetAsync("1.3.6.1.2.1.1.5.0");

// Walk SNMP tree
var nextResult = await client.GetNextAsync("1.3.6.1.2.1.2.2.1");
```

### **nSNMP Features Demonstrated**:
- ✅ **SnmpClient** for device communication
- ✅ **Async/await** operations
- ✅ **Version detection** and fallback
- ✅ **Community string testing**
- ✅ **MIB browsing** with GetNext
- ✅ **Error handling** and timeouts
- ✅ **Standard MIB OIDs** usage

## 🚀 **Quick Start**

### **Interactive Mode** (Default)
```bash
dotnet run --project samples/SnmpScout
```

### **Command Line Mode**
```bash
# Scan a network range
dotnet run --project samples/SnmpScout -- --interactive false --network "192.168.1.0/24"

# Scan and export results
dotnet run --project samples/SnmpScout -- --interactive false --network "10.0.0.0/24" --output devices.json
```

## 📋 **Usage Examples**

### **1. Quick Local Network Scan**
```
🔍 SnmpScout - Main Menu

What would you like to do?
> 🔍 Quick Scan (Local Network)

🔍 Scanning network: 192.168.1.0/24
✅ Found: Home-Router (192.168.1.1)
✅ Found: Office-Printer (192.168.1.100)
✅ Found: NAS-Server (192.168.1.200)

✅ Scan completed! Found 3 devices
```

### **2. Device Details View**
```
🖥️ Home-Router
192.168.1.1 • Router • 🟢 Online

📊 System Information
┌─────────────────┬──────────────────────────────┐
│ IP Address      │ 192.168.1.1                 │
│ Device Type     │ 🔀 Router                   │
│ SNMP Version    │ V2c                         │
│ System Uptime   │ 15d 8h 42m                  │
│ Location        │ Home Office                  │
└─────────────────┴──────────────────────────────┘
```

### **3. Export Results**
```bash
# Export discovered devices
📄 Export Results
Enter filename (.json, .csv, or .txt): network-inventory.json
📄 Exported 12 devices to network-inventory.json
```

## 🔧 **Device Types Detected**

SnmpScout automatically categorizes devices based on their SNMP system description:

| Type | Emoji | Detection Keywords |
|------|-------|-------------------|
| Router | 🔀 | "router" |
| Switch | 🔗 | "switch" |
| Access Point | 📶 | "access point", "wireless" |
| Printer | 🖨️ | "printer" |
| Server | 🖥️ | "server" |
| UPS | 🔋 | "ups", "power" |
| Camera | 📹 | "camera" |
| Phone | 📞 | "phone" |
| Storage | 💾 | "storage", "nas" |
| Firewall | 🔥 | "firewall" |

## 📊 **Sample Output**

### **Device Table**
```
┌────────┬─────────────────┬──────────────────┬─────────────┬──────────┬──────────┐
│ Status │ IP Address      │ Device Name      │ Type        │ SNMP Ver │ Uptime   │
├────────┼─────────────────┼──────────────────┼─────────────┼──────────┼──────────┤
│ 🟢     │ 192.168.1.1     │ Home-Router      │ 🔀 Router   │ V2c      │ 15d 8h   │
│ 🟢     │ 192.168.1.100   │ Office-Printer   │ 🖨️ Printer  │ V1       │ 2d 14h   │
│ 🟢     │ 192.168.1.200   │ NAS-Server       │ 💾 Storage  │ V2c      │ 45d 12h  │
└────────┴─────────────────┴──────────────────┴─────────────┴──────────┴──────────┘
```

### **JSON Export Sample**
```json
{
  "ipAddress": "192.168.1.1",
  "systemName": "Home-Router",
  "systemDescription": "Linux 3.14.77 router",
  "deviceType": "Router",
  "snmpVersion": "V2c",
  "community": "public",
  "systemUptime": "15.08:42:33",
  "interfaces": [
    {
      "index": 1,
      "description": "eth0",
      "status": "Up",
      "type": "Ethernet"
    }
  ]
}
```

## 🎯 **Advanced Features**

### **SNMP Walking**
Interactively browse SNMP MIB trees:
```
🔍 SNMP Walk: Home-Router
Enter OID to walk (e.g., 1.3.6.1.2.1.1):

┌─────────────────────┬──────────────────────────────┐
│ OID                 │ Value                        │
├─────────────────────┼──────────────────────────────┤
│ 1.3.6.1.2.1.1.1.0   │ Linux 3.14.77 router        │
│ 1.3.6.1.2.1.1.2.0   │ 1.3.6.1.4.1.2021.250.10     │
│ 1.3.6.1.2.1.1.3.0   │ 131234567                    │
└─────────────────────┴──────────────────────────────┘
```

### **Community String Testing**
Automatically tests common community strings:
- `public`
- `private`
- `admin`
- `manager`
- `snmp`

## 🏗 **Architecture**

SnmpScout is built with a clean, modular architecture:

```
SnmpScout/
├── Models/           # Device and network models
│   └── NetworkDevice.cs
├── Services/         # Core business logic
│   ├── NetworkScanner.cs     # nSNMP-powered scanning
│   ├── DeviceManager.cs      # Device management
│   └── NetworkUtils.cs       # Network utilities
├── UI/               # Console user interface
│   └── UserInterface.cs     # Spectre.Console UI
└── SnmpScoutApp.cs   # Main application orchestrator
```

## 🔗 **Dependencies**

- **nSNMP.Core** - Core SNMP functionality
- **nSNMP.Abstractions** - SNMP abstractions
- **nSNMP.Extensions** - Extended SNMP features
- **Spectre.Console** - Beautiful console UI
- **System.CommandLine** - Command-line parsing

## 🎓 **Learning Example**

SnmpScout serves as a comprehensive example of:
- **Real-world nSNMP usage**
- **Async SNMP operations**
- **Network device discovery patterns**
- **SNMP error handling**
- **MIB navigation techniques**
- **Performance optimization**

Perfect for learning how to build network management tools with nSNMP! 🚀

## 📝 **License**

Same license as the nSNMP project.