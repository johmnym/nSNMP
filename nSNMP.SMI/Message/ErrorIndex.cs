using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class ErrorIndex : Integer
    {
        public ErrorIndex(SnmpDataType type, byte[] data) : base(data)
        {
        }
    }
}
