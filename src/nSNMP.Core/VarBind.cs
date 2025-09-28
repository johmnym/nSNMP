using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Core
{
    /// <summary>
    /// Represents an SNMP variable binding (OID + Value)
    /// </summary>
    public record VarBind(ObjectIdentifier Oid, IDataType Value)
    {
        /// <summary>
        /// Creates a VarBind for GET operations (value is Null)
        /// </summary>
        public VarBind(ObjectIdentifier oid) : this(oid, new Null()) { }

        /// <summary>
        /// Creates a VarBind from string OID for GET operations
        /// </summary>
        public VarBind(string oid) : this(ObjectIdentifier.Create(oid), new Null()) { }

        /// <summary>
        /// Creates a VarBind from string OID with value for SET operations
        /// </summary>
        public VarBind(string oid, IDataType value) : this(ObjectIdentifier.Create(oid), value) { }

        /// <summary>
        /// True if this is an end-of-MIB-view marker (for walks)
        /// </summary>
        public bool IsEndOfMibView => Value is Null;

        /// <summary>
        /// Gets the OID as a string
        /// </summary>
        public string OidString => Oid.ToString();

        public override string ToString()
        {
            return $"{Oid} = {Value}";
        }
    }
}