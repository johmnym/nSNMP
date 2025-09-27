using System;
using System.Collections.Generic;
using System.Linq;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.MIB
{
    /// <summary>
    /// Represents a MIB object definition
    /// </summary>
    public class MibObject
    {
        /// <summary>
        /// Object name/identifier
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Object OID
        /// </summary>
        public ObjectIdentifier? Oid { get; set; }

        /// <summary>
        /// Object type (e.g., "INTEGER", "OCTET STRING", etc.)
        /// </summary>
        public string? Syntax { get; set; }

        /// <summary>
        /// Maximum access level
        /// </summary>
        public Access Access { get; set; }

        /// <summary>
        /// Implementation status
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Object description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Reference information
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Index information for table entries
        /// </summary>
        public List<string>? Index { get; set; }

        /// <summary>
        /// Augments clause for table rows
        /// </summary>
        public string? Augments { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Size constraints
        /// </summary>
        public (int? Min, int? Max)? SizeConstraint { get; set; }

        /// <summary>
        /// Value constraints for INTEGER types
        /// </summary>
        public Dictionary<string, int>? NamedValues { get; set; }

        /// <summary>
        /// Range constraints
        /// </summary>
        public List<(long Min, long Max)>? RangeConstraints { get; set; }

        /// <summary>
        /// Parent object (for tree structure)
        /// </summary>
        public MibObject? Parent { get; set; }

        /// <summary>
        /// Child objects
        /// </summary>
        public List<MibObject> Children { get; }

        /// <summary>
        /// Module this object belongs to
        /// </summary>
        public string? ModuleName { get; set; }

        public MibObject(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Children = new List<MibObject>();
            Access = Access.NotAccessible;
            Status = Status.Current;
        }

        /// <summary>
        /// Add a child object
        /// </summary>
        public void AddChild(MibObject child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Check if this object is a table
        /// </summary>
        public bool IsTable => Syntax?.Contains("SEQUENCE OF") == true;

        /// <summary>
        /// Check if this object is a table entry
        /// </summary>
        public bool IsTableEntry => (Syntax?.Contains("SEQUENCE") == true && !IsTable) || Index?.Any() == true;

        /// <summary>
        /// Check if this object is a column (has index)
        /// </summary>
        public bool IsColumn => Index?.Any() == true || Parent?.IsTableEntry == true;

        /// <summary>
        /// Get full OID path as string (without leading dot for MIB context)
        /// </summary>
        public string GetOidPath()
        {
            if (Oid == null) return string.Empty;
            var oidString = Oid.ToString();
            return oidString.StartsWith(".") ? oidString.Substring(1) : oidString;
        }

        /// <summary>
        /// Get human-readable path (name hierarchy)
        /// </summary>
        public string GetNamePath()
        {
            var path = new List<string>();
            var current = this;

            while (current != null)
            {
                path.Insert(0, current.Name);
                current = current.Parent;
            }

            return string.Join(".", path);
        }

        public override string ToString()
        {
            return $"{Name} ({GetOidPath()}) - {Syntax} [{Access}]";
        }
    }
}