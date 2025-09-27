using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.MIB
{
    /// <summary>
    /// Represents a MIB module with its objects and metadata
    /// </summary>
    public class MibModule
    {
        /// <summary>
        /// Module name (e.g., "SNMPv2-MIB")
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Module OID identifier
        /// </summary>
        public ObjectIdentifier? ModuleOid { get; }

        /// <summary>
        /// Module description
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Organization responsible for the module
        /// </summary>
        public string? Organization { get; }

        /// <summary>
        /// Contact information
        /// </summary>
        public string? ContactInfo { get; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime? LastUpdated { get; }

        /// <summary>
        /// Revision history
        /// </summary>
        public List<MibRevision> Revisions { get; }

        /// <summary>
        /// Objects defined in this module
        /// </summary>
        public Dictionary<string, MibObject> Objects { get; }

        /// <summary>
        /// Imported modules
        /// </summary>
        public Dictionary<string, List<string>> Imports { get; }

        /// <summary>
        /// Exported symbols
        /// </summary>
        public List<string> Exports { get; }

        public MibModule(string name, ObjectIdentifier? moduleOid = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ModuleOid = moduleOid;
            Revisions = new List<MibRevision>();
            Objects = new Dictionary<string, MibObject>();
            Imports = new Dictionary<string, List<string>>();
            Exports = new List<string>();
        }

        /// <summary>
        /// Add an object to this module
        /// </summary>
        public void AddObject(MibObject obj)
        {
            Objects[obj.Name] = obj;
        }

        /// <summary>
        /// Get object by name
        /// </summary>
        public MibObject? GetObject(string name)
        {
            return Objects.TryGetValue(name, out var obj) ? obj : null;
        }

        /// <summary>
        /// Get object by OID
        /// </summary>
        public MibObject? GetObject(ObjectIdentifier oid)
        {
            return Objects.Values.FirstOrDefault(obj => obj.Oid?.Equals(oid) == true);
        }

        public override string ToString()
        {
            return $"MIB Module: {Name} ({Objects.Count} objects)";
        }
    }

    /// <summary>
    /// Represents a MIB module revision
    /// </summary>
    public record MibRevision(DateTime Date, string Description);
}