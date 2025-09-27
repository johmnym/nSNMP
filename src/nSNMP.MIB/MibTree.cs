using System;
using System.Collections.Generic;
using System.Linq;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.MIB
{
    /// <summary>
    /// Manages the MIB object tree and provides OID-to-name mapping
    /// </summary>
    public class MibTree
    {
        private readonly Dictionary<string, MibObject> _objectsByName;
        private readonly Dictionary<string, MibObject> _objectsByOid;
        private readonly Dictionary<string, MibModule> _modules;
        private MibObject? _root;

        public MibTree()
        {
            _objectsByName = new Dictionary<string, MibObject>();
            _objectsByOid = new Dictionary<string, MibObject>();
            _modules = new Dictionary<string, MibModule>();
            InitializeStandardTree();
        }

        /// <summary>
        /// Initialize the standard MIB tree structure
        /// </summary>
        private void InitializeStandardTree()
        {
            // Create root node
            _root = new MibObject("root");

            // Standard OID tree structure - using valid multi-component OIDs
            var org = new MibObject("org") { Oid = ObjectIdentifier.Create("1.3") };
            var dod = new MibObject("dod") { Oid = ObjectIdentifier.Create("1.3.6") };
            var internet = new MibObject("internet") { Oid = ObjectIdentifier.Create("1.3.6.1") };

            var mgmt = new MibObject("mgmt") { Oid = ObjectIdentifier.Create("1.3.6.1.2") };
            var mib2 = new MibObject("mib-2") { Oid = ObjectIdentifier.Create("1.3.6.1.2.1") };

            var experimental = new MibObject("experimental") { Oid = ObjectIdentifier.Create("1.3.6.1.3") };
            var privateNode = new MibObject("private") { Oid = ObjectIdentifier.Create("1.3.6.1.4") };
            var enterprises = new MibObject("enterprises") { Oid = ObjectIdentifier.Create("1.3.6.1.4.1") };

            // Build tree structure
            _root.AddChild(org);
            org.AddChild(dod);
            dod.AddChild(internet);
            internet.AddChild(mgmt);
            mgmt.AddChild(mib2);
            internet.AddChild(experimental);
            internet.AddChild(privateNode);
            privateNode.AddChild(enterprises);

            // Register objects
            RegisterObject(org);
            RegisterObject(dod);
            RegisterObject(internet);
            RegisterObject(mgmt);
            RegisterObject(mib2);
            RegisterObject(experimental);
            RegisterObject(privateNode);
            RegisterObject(enterprises);
        }

        /// <summary>
        /// Load a MIB module into the tree
        /// </summary>
        public void LoadModule(MibModule module)
        {
            _modules[module.Name] = module;

            foreach (var obj in module.Objects.Values)
            {
                RegisterObject(obj);
            }
        }

        /// <summary>
        /// Register an object in the tree
        /// </summary>
        public void RegisterObject(MibObject obj)
        {
            _objectsByName[obj.Name] = obj;

            if (obj.Oid != null)
            {
                var oidString = obj.Oid.ToString();
                // Remove leading dot if present
                if (oidString.StartsWith("."))
                    oidString = oidString.Substring(1);

                _objectsByOid[oidString] = obj;
            }
        }

        /// <summary>
        /// Get object by name
        /// </summary>
        public MibObject? GetObjectByName(string name)
        {
            return _objectsByName.TryGetValue(name, out var obj) ? obj : null;
        }

        /// <summary>
        /// Get object by OID
        /// </summary>
        public MibObject? GetObjectByOid(ObjectIdentifier oid)
        {
            return GetObjectByOid(oid.ToString());
        }

        /// <summary>
        /// Get object by OID string
        /// </summary>
        public MibObject? GetObjectByOid(string oidString)
        {
            return _objectsByOid.TryGetValue(oidString, out var obj) ? obj : null;
        }

        /// <summary>
        /// Convert OID to human-readable name
        /// </summary>
        public string OidToName(ObjectIdentifier oid)
        {
            return OidToName(oid.ToString());
        }

        /// <summary>
        /// Convert OID string to human-readable name
        /// </summary>
        public string OidToName(string oidString)
        {
            var obj = GetObjectByOid(oidString);
            if (obj != null)
            {
                // Return just the object name, not the full path
                return obj.Name;
            }

            // Try to find the closest parent
            var parts = oidString.Split('.');
            for (int i = parts.Length - 1; i > 0; i--)
            {
                var parentOid = string.Join(".", parts.Take(i));
                var parentObj = GetObjectByOid(parentOid);
                if (parentObj != null)
                {
                    var remainingParts = string.Join(".", parts.Skip(i));
                    return $"{parentObj.Name}.{remainingParts}";
                }
            }

            return oidString; // Return original if no match found
        }

        /// <summary>
        /// Convert name to OID
        /// </summary>
        public ObjectIdentifier? NameToOid(string name)
        {
            var obj = GetObjectByName(name);
            if (obj?.Oid == null) return null;

            // Create a new ObjectIdentifier with normalized string (without leading dot)
            var oidString = obj.Oid.ToString();
            if (oidString.StartsWith("."))
                oidString = oidString.Substring(1);

            return ObjectIdentifier.Create(oidString);
        }

        /// <summary>
        /// Get the next OID in lexicographic order
        /// </summary>
        public ObjectIdentifier? GetNextOid(ObjectIdentifier oid)
        {
            var oidString = oid.ToString();
            var allOids = _objectsByOid.Keys.OrderBy(o => o, new OidComparer()).ToList();

            var index = allOids.BinarySearch(oidString);
            if (index < 0)
            {
                index = ~index; // Get insertion point
            }
            else
            {
                index++; // Move to next
            }

            if (index < allOids.Count)
            {
                return ObjectIdentifier.Create(allOids[index]);
            }

            return null; // End of MIB
        }

        /// <summary>
        /// Get all objects under a subtree
        /// </summary>
        public IEnumerable<MibObject> GetSubtree(ObjectIdentifier rootOid)
        {
            var rootString = rootOid.ToString();
            // Remove leading dot if present and add trailing dot
            if (rootString.StartsWith("."))
                rootString = rootString.Substring(1);
            rootString += ".";

            return _objectsByOid
                .Where(kvp => kvp.Key.StartsWith(rootString))
                .Select(kvp => kvp.Value)
                .OrderBy(obj => obj.Oid?.ToString(), new OidComparer());
        }

        /// <summary>
        /// Get loaded modules
        /// </summary>
        public IReadOnlyDictionary<string, MibModule> Modules => _modules;

        /// <summary>
        /// Get all registered objects
        /// </summary>
        public IEnumerable<MibObject> AllObjects => _objectsByName.Values;

        /// <summary>
        /// Get statistics about the loaded MIB tree
        /// </summary>
        public MibTreeStats GetStats()
        {
            return new MibTreeStats
            {
                TotalObjects = _objectsByName.Count,
                TotalModules = _modules.Count,
                ObjectsWithOids = _objectsByOid.Count,
                Tables = _objectsByName.Values.Count(o => o.IsTable),
                Columns = _objectsByName.Values.Count(o => o.IsColumn)
            };
        }
    }

    /// <summary>
    /// Statistics about the MIB tree
    /// </summary>
    public record MibTreeStats
    {
        public int TotalObjects { get; init; }
        public int TotalModules { get; init; }
        public int ObjectsWithOids { get; init; }
        public int Tables { get; init; }
        public int Columns { get; init; }

        public override string ToString()
        {
            return $"MIB Tree: {TotalModules} modules, {TotalObjects} objects, {Tables} tables, {Columns} columns";
        }
    }

    /// <summary>
    /// Comparer for OID strings in lexicographic order
    /// </summary>
    public class OidComparer : IComparer<string?>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var xParts = x.Split('.', StringSplitOptions.RemoveEmptyEntries)
                          .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
                          .Select(int.Parse)
                          .ToArray();
            var yParts = y.Split('.', StringSplitOptions.RemoveEmptyEntries)
                          .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
                          .Select(int.Parse)
                          .ToArray();

            int minLength = Math.Min(xParts.Length, yParts.Length);

            for (int i = 0; i < minLength; i++)
            {
                int result = xParts[i].CompareTo(yParts[i]);
                if (result != 0) return result;
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}