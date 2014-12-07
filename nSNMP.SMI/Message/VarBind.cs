using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class VarBind : Sequence
    {
        public VarBind(SnmpDataType type, byte[] data) : base(type, data)
        {
        }

        public ObjectIdentifier ObjectIdentifier { get; set; }
        public DataType Value { get; set; }
    }
}
