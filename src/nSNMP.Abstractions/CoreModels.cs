namespace nSNMP.Abstractions
{
    /// <summary>
    /// SNMP protocol versions
    /// </summary>
    public enum SnmpVersion
    {
        /// <summary>
        /// SNMP version 1
        /// </summary>
        V1 = 0,

        /// <summary>
        /// SNMP version 2c
        /// </summary>
        V2c = 1,

        /// <summary>
        /// SNMP version 3
        /// </summary>
        V3 = 3
    }

    /// <summary>
    /// SNMP error status codes
    /// </summary>
    public enum ErrorStatus
    {
        /// <summary>
        /// No error
        /// </summary>
        NoError = 0,

        /// <summary>
        /// Request too big
        /// </summary>
        TooBig = 1,

        /// <summary>
        /// No such name
        /// </summary>
        NoSuchName = 2,

        /// <summary>
        /// Bad value
        /// </summary>
        BadValue = 3,

        /// <summary>
        /// Read only
        /// </summary>
        ReadOnly = 4,

        /// <summary>
        /// General error
        /// </summary>
        GenErr = 5,

        /// <summary>
        /// No access
        /// </summary>
        NoAccess = 6,

        /// <summary>
        /// Wrong type
        /// </summary>
        WrongType = 7,

        /// <summary>
        /// Wrong length
        /// </summary>
        WrongLength = 8,

        /// <summary>
        /// Wrong encoding
        /// </summary>
        WrongEncoding = 9,

        /// <summary>
        /// Wrong value
        /// </summary>
        WrongValue = 10,

        /// <summary>
        /// No creation
        /// </summary>
        NoCreation = 11,

        /// <summary>
        /// Inconsistent value
        /// </summary>
        InconsistentValue = 12,

        /// <summary>
        /// Resource unavailable
        /// </summary>
        ResourceUnavailable = 13,

        /// <summary>
        /// Commit failed
        /// </summary>
        CommitFailed = 14,

        /// <summary>
        /// Undo failed
        /// </summary>
        UndoFailed = 15,

        /// <summary>
        /// Authorization error
        /// </summary>
        AuthorizationError = 16,

        /// <summary>
        /// Not writable
        /// </summary>
        NotWritable = 17,

        /// <summary>
        /// Inconsistent name
        /// </summary>
        InconsistentName = 18
    }

    /// <summary>
    /// SNMPv3 security levels
    /// </summary>
    public enum SecurityLevel
    {
        /// <summary>
        /// No authentication, no privacy
        /// </summary>
        NoAuthNoPriv = 0,

        /// <summary>
        /// Authentication without privacy
        /// </summary>
        AuthNoPriv = 1,

        /// <summary>
        /// Authentication with privacy
        /// </summary>
        AuthPriv = 3
    }

    /// <summary>
    /// SNMPv3 authentication protocols
    /// </summary>
    public enum AuthProtocol
    {
        /// <summary>
        /// No authentication
        /// </summary>
        None = 0,

        /// <summary>
        /// MD5 authentication
        /// </summary>
        MD5 = 1,

        /// <summary>
        /// SHA-1 authentication
        /// </summary>
        SHA1 = 2,

        /// <summary>
        /// SHA-224 authentication
        /// </summary>
        SHA224 = 3,

        /// <summary>
        /// SHA-256 authentication
        /// </summary>
        SHA256 = 4,

        /// <summary>
        /// SHA-384 authentication
        /// </summary>
        SHA384 = 5,

        /// <summary>
        /// SHA-512 authentication
        /// </summary>
        SHA512 = 6
    }

    /// <summary>
    /// SNMPv3 privacy protocols
    /// </summary>
    public enum PrivProtocol
    {
        /// <summary>
        /// No privacy
        /// </summary>
        None = 0,

        /// <summary>
        /// DES privacy
        /// </summary>
        DES = 1,

        /// <summary>
        /// Triple DES privacy
        /// </summary>
        TripleDES = 2,

        /// <summary>
        /// AES-128 privacy
        /// </summary>
        AES128 = 3,

        /// <summary>
        /// AES-192 privacy
        /// </summary>
        AES192 = 4,

        /// <summary>
        /// AES-256 privacy
        /// </summary>
        AES256 = 5
    }

    /// <summary>
    /// SNMP PDU types
    /// </summary>
    public enum PduType : byte
    {
        /// <summary>
        /// GET request
        /// </summary>
        Get = 0xa0,

        /// <summary>
        /// GET-NEXT request
        /// </summary>
        GetNext = 0xa1,

        /// <summary>
        /// GET-RESPONSE
        /// </summary>
        GetResponse = 0xa2,

        /// <summary>
        /// SET request
        /// </summary>
        Set = 0xa3,

        /// <summary>
        /// Trap (v1)
        /// </summary>
        Trap = 0xa4,

        /// <summary>
        /// GET-BULK request (v2c/v3)
        /// </summary>
        GetBulk = 0xa5,

        /// <summary>
        /// INFORM request (v2c/v3)
        /// </summary>
        Inform = 0xa6,

        /// <summary>
        /// Trap (v2c/v3)
        /// </summary>
        TrapV2 = 0xa7,

        /// <summary>
        /// REPORT (v3)
        /// </summary>
        Report = 0xa8
    }
}