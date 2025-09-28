using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Agent
{
    /// <summary>
    /// Provider interface for SNMP tables
    /// </summary>
    public interface ITableProvider
    {
        /// <summary>
        /// Get the value for the specified table cell OID
        /// </summary>
        /// <param name="oid">The complete OID including table and index</param>
        /// <returns>The data value, or null if not found</returns>
        IDataType? GetValue(ObjectIdentifier oid);

        /// <summary>
        /// Set the value for the specified table cell OID
        /// </summary>
        /// <param name="oid">The complete OID including table and index</param>
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
        /// Get the next OID in lexicographic order from this table
        /// </summary>
        /// <param name="oid">The starting OID</param>
        /// <returns>The next OID, or null if none exists</returns>
        ObjectIdentifier? GetNextOid(ObjectIdentifier oid);

        /// <summary>
        /// Get all available row indices for this table
        /// </summary>
        /// <returns>Enumerable of row indices</returns>
        IEnumerable<ObjectIdentifier> GetRowIndices();

        /// <summary>
        /// Get all column OIDs for this table
        /// </summary>
        /// <returns>Enumerable of column OIDs</returns>
        IEnumerable<ObjectIdentifier> GetColumns();
    }
}