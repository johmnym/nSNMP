
namespace nSNMP.SMI
{
    public enum SnmpDataType
    {
        //Primitive V1
        Integer = 0x02,
        OctetString = 0x04,
        Null = 0x05,
        ObjectIdentifier = 0x06,

        //Application-specific primitive types
        IpAddress = 0x40,        // [APPLICATION 0] IMPLICIT OCTET STRING (SIZE (4))
        Counter32 = 0x41,        // [APPLICATION 1] IMPLICIT INTEGER (0..4294967295)
        Gauge32 = 0x42,          // [APPLICATION 2] IMPLICIT INTEGER (0..4294967295)
        TimeTicks = 0x43,        // [APPLICATION 3] IMPLICIT INTEGER (0..4294967295)
        Opaque = 0x44,           // [APPLICATION 4] IMPLICIT OCTET STRING
        Counter64 = 0x46,        // [APPLICATION 6] IMPLICIT INTEGER (0..18446744073709551615)

        //Exception values (SNMPv2c/v3)
        NoSuchObject = 0x80,      // [0] IMPLICIT NULL
        NoSuchInstance = 0x81,    // [1] IMPLICIT NULL
        EndOfMibView = 0x82,      // [2] IMPLICIT NULL

        //Constructed V1
        Sequence = 0x30,
        GetRequestPDU = 0xA0,
        GetNextRequestPDU = 0xA1,
        GetResponsePDU = 0xA2,
        SetRequestPDU = 0xA3,
        TrapPDU = 0xA4,          // SNMPv1 Trap
        GetBulkRequestPDU = 0xA5, // SNMPv2c/v3 GetBulk
        InformRequestPDU = 0xA6,  // SNMPv2c/v3 Inform
        TrapV2PDU = 0xA7,        // SNMPv2c/v3 Trap (Notification)
        ReportPDU = 0xA8,        // SNMPv3 Report
    }
}
