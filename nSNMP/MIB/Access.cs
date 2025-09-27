namespace nSNMP.MIB
{
    /// <summary>
    /// SNMP MIB object access types
    /// </summary>
    public enum Access
    {
        /// <summary>
        /// Object can only be read
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Object can be read and written
        /// </summary>
        ReadWrite,

        /// <summary>
        /// Object cannot be accessed (deprecated objects)
        /// </summary>
        NotAccessible,

        /// <summary>
        /// Object is accessible for notifications only
        /// </summary>
        AccessibleForNotify
    }
}