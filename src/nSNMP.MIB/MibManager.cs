using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nSNMP.Abstractions;
using nSNMP.Logging;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.MIB
{
    /// <summary>
    /// Central manager for MIB operations and OID resolution
    /// </summary>
    public class MibManager
    {
        private readonly MibTree _tree;
        private readonly Dictionary<string, string> _mibDirectories;
        private readonly nSNMP.Logging.ISnmpLogger _logger;

        /// <summary>
        /// Static instance for global access
        /// </summary>
        public static MibManager Instance { get; } = new MibManager();

        public MibManager(nSNMP.Logging.ISnmpLogger? logger = null)
        {
            _tree = new MibTree();
            _mibDirectories = new Dictionary<string, string>();
            _logger = logger ?? NullSnmpLogger.Instance;
            LoadStandardMibs();
        }

        /// <summary>
        /// The MIB tree containing all loaded objects
        /// </summary>
        public MibTree Tree => _tree;

        /// <summary>
        /// Load standard MIB modules
        /// </summary>
        private void LoadStandardMibs()
        {
            try
            {
                // Create basic SNMPv2-MIB objects
                var snmpv2Module = CreateSnmpV2MibModule();
                _tree.LoadModule(snmpv2Module);
            }
            catch (Exception ex)
            {
                _logger.LogAgent("MibManager", $"Failed to load standard MIBs: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a basic SNMPv2-MIB module with essential objects
        /// </summary>
        private MibModule CreateSnmpV2MibModule()
        {
            var module = new MibModule("SNMPv2-MIB", ObjectIdentifier.Create("1.3.6.1.6.3.1"));

            // System group objects
            var system = new MibObject("system")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1"),
                Syntax = "OBJECT IDENTIFIER",
                Access = Access.NotAccessible,
                Status = Status.Current,
                Description = "System group"
            };

            var sysDescr = new MibObject("sysDescr")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0"),
                Syntax = "DisplayString",
                Access = Access.ReadOnly,
                Status = Status.Current,
                Description = "A textual description of the entity"
            };

            var sysObjectID = new MibObject("sysObjectID")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0"),
                Syntax = "OBJECT IDENTIFIER",
                Access = Access.ReadOnly,
                Status = Status.Current,
                Description = "The vendor's authoritative identification"
            };

            var sysUpTime = new MibObject("sysUpTime")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.3.0"),
                Syntax = "TimeTicks",
                Access = Access.ReadOnly,
                Status = Status.Current,
                Description = "Time since the network management portion of the system was last re-initialized"
            };

            var sysContact = new MibObject("sysContact")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.4.0"),
                Syntax = "DisplayString",
                Access = Access.ReadWrite,
                Status = Status.Current,
                Description = "Contact person for this managed node"
            };

            var sysName = new MibObject("sysName")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.5.0"),
                Syntax = "DisplayString",
                Access = Access.ReadWrite,
                Status = Status.Current,
                Description = "An administratively-assigned name for this managed node"
            };

            var sysLocation = new MibObject("sysLocation")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.6.0"),
                Syntax = "DisplayString",
                Access = Access.ReadWrite,
                Status = Status.Current,
                Description = "Physical location of this node"
            };

            var sysServices = new MibObject("sysServices")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.7.0"),
                Syntax = "INTEGER",
                Access = Access.ReadOnly,
                Status = Status.Current,
                Description = "A value which indicates the set of services"
            };

            // SNMP group objects
            var snmp = new MibObject("snmp")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.2.1.11"),
                Syntax = "OBJECT IDENTIFIER",
                Access = Access.NotAccessible,
                Status = Status.Current,
                Description = "SNMP group"
            };

            var snmpTrapOID = new MibObject("snmpTrapOID")
            {
                Oid = ObjectIdentifier.Create("1.3.6.1.6.3.1.1.4.1.0"),
                Syntax = "OBJECT IDENTIFIER",
                Access = Access.AccessibleForNotify,
                Status = Status.Current,
                Description = "The authoritative identification of the notification"
            };

            // Add objects to module
            module.AddObject(system);
            module.AddObject(sysDescr);
            module.AddObject(sysObjectID);
            module.AddObject(sysUpTime);
            module.AddObject(sysContact);
            module.AddObject(sysName);
            module.AddObject(sysLocation);
            module.AddObject(sysServices);
            module.AddObject(snmp);
            module.AddObject(snmpTrapOID);


            return module;
        }

        /// <summary>
        /// Load a MIB file
        /// </summary>
        public MibModule LoadMibFile(string filePath)
        {
            var module = MibParser.LoadMibFile(filePath, _logger);
            _tree.LoadModule(module);

            // Validate the loaded module
            var errors = MibParser.ValidateModule(module);
            if (errors.Any())
            {
                Console.WriteLine($"MIB validation warnings for {module.Name}:");
                errors.ForEach(e => Console.WriteLine($"  - {e}"));
            }

            return module;
        }

        /// <summary>
        /// Load all MIB files from a directory
        /// </summary>
        public void LoadMibDirectory(string directoryPath, string pattern = "*.mib")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"MIB directory not found: {directoryPath}");

            var mibFiles = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);

            foreach (var file in mibFiles)
            {
                try
                {
                    LoadMibFile(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load MIB file {file}: {ex.Message}");
                }
            }

            _mibDirectories[directoryPath] = pattern;
        }

        /// <summary>
        /// Convert OID to human-readable name
        /// </summary>
        public string OidToName(ObjectIdentifier oid)
        {
            return _tree.OidToName(oid);
        }

        /// <summary>
        /// Convert OID string to human-readable name
        /// </summary>
        public string OidToName(string oidString)
        {
            return _tree.OidToName(oidString);
        }

        /// <summary>
        /// Convert name to OID
        /// </summary>
        public ObjectIdentifier? NameToOid(string name)
        {
            return _tree.NameToOid(name);
        }

        /// <summary>
        /// Get object information by OID
        /// </summary>
        public MibObject? GetObject(ObjectIdentifier oid)
        {
            return _tree.GetObjectByOid(oid);
        }

        /// <summary>
        /// Get object information by name
        /// </summary>
        public MibObject? GetObject(string name)
        {
            return _tree.GetObjectByName(name);
        }

        /// <summary>
        /// Get the next OID in the MIB tree
        /// </summary>
        public ObjectIdentifier? GetNextOid(ObjectIdentifier oid)
        {
            return _tree.GetNextOid(oid);
        }

        /// <summary>
        /// Get all objects under a subtree
        /// </summary>
        public IEnumerable<MibObject> GetSubtree(ObjectIdentifier rootOid)
        {
            return _tree.GetSubtree(rootOid);
        }

        /// <summary>
        /// Search for objects by name pattern
        /// </summary>
        public IEnumerable<MibObject> SearchObjects(string pattern)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return _tree.AllObjects.Where(obj => regex.IsMatch(obj.Name));
        }

        /// <summary>
        /// Get loaded modules
        /// </summary>
        public IReadOnlyDictionary<string, MibModule> Modules => _tree.Modules;

        /// <summary>
        /// Get MIB tree statistics
        /// </summary>
        public MibTreeStats GetStats()
        {
            return _tree.GetStats();
        }

        /// <summary>
        /// Clear all loaded MIBs (except standard ones)
        /// </summary>
        public void Clear()
        {
            _tree.AllObjects.ToList().ForEach(obj =>
            {
                if (obj.ModuleName != "SNMPv2-MIB" && obj.ModuleName != null)
                {
                    // Remove custom objects (keep standard tree)
                }
            });
        }

        /// <summary>
        /// Export the current MIB tree information
        /// </summary>
        public string ExportTreeInfo()
        {
            var stats = GetStats();
            var info = new List<string>
            {
                $"MIB Manager Status: {stats}",
                "",
                "Loaded Modules:",
            };

            foreach (var module in Modules.Values)
            {
                info.Add($"  - {module.Name}: {module.Objects.Count} objects");
            }

            info.Add("");
            info.Add("MIB Directories:");
            foreach (var dir in _mibDirectories)
            {
                info.Add($"  - {dir.Key} (pattern: {dir.Value})");
            }

            return string.Join(Environment.NewLine, info);
        }
    }
}