using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class VarBind : Sequence
    {
        public VarBind(byte[] data) : base(data)
        {
        }

        public ObjectIdentifier ObjectIdentifier { get; set; }
        public IDataType Value { get; set; }
    }
}