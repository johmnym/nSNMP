
namespace nSNMP.SMI
{
    public enum SnmpDataType
    {
        //Primitive V1
        Integer = 0x02,
        OctetString = 0x04,
        Null = 0x05,
        ObjectIdentifier = 0x06,
        
        //Constructed V1
        Sequence = 0x30,
        GetRequestPDU = 0xA0,
        GetResponsePDU = 0xA2,
        SetRequestPDU = 0xA3,

        //Message
        SnmpMessage = 10000,
        VarbindsList = 11000,
    }
}
