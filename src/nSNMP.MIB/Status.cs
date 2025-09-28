namespace nSNMP.MIB
{
    /// <summary>
    /// SNMP MIB object status types
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Object must be implemented
        /// </summary>
        Mandatory,

        /// <summary>
        /// Object implementation is optional
        /// </summary>
        Optional,

        /// <summary>
        /// Object is current and valid
        /// </summary>
        Current,

        /// <summary>
        /// Object is deprecated but still supported
        /// </summary>
        Deprecated,

        /// <summary>
        /// Object is obsolete and should not be used
        /// </summary>
        Obsolete
    }
}