using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Agent
{
    /// <summary>
    /// Provider interface for single-value OIDs
    /// </summary>
    public interface IScalarProvider
    {
        /// <summary>
        /// Get the value for the specified OID
        /// </summary>
        /// <param name="oid">The object identifier to retrieve</param>
        /// <returns>The data value, or null if not found</returns>
        IDataType? GetValue(ObjectIdentifier oid);

        /// <summary>
        /// Set the value for the specified OID
        /// </summary>
        /// <param name="oid">The object identifier to set</param>
        /// <param name="value">The new value</param>
        /// <returns>True if the set operation was successful</returns>
        bool SetValue(ObjectIdentifier oid, IDataType value);

        /// <summary>
        /// Check if this provider handles the specified OID
        /// </summary>
        /// <param name="oid">The object identifier to check</param>
        /// <returns>True if this provider handles the OID</returns>
        bool CanHandle(ObjectIdentifier oid);

        /// <summary>
        /// Get the next OID in lexicographic order from this provider
        /// </summary>
        /// <param name="oid">The starting OID</param>
        /// <returns>The next OID, or null if none exists</returns>
        ObjectIdentifier? GetNextOid(ObjectIdentifier oid);
    }
}