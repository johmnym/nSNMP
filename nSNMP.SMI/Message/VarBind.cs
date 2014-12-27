using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.Message
{
    public class Varbind : Sequence
    {
        public Varbind(byte[] data) : base(data)
        {
        }

        public ObjectIdentifier ObjectIdentifier { get { return (ObjectIdentifier) Elements[0]; } }
        public IDataType Value { get { return Elements[1]; } }
    }
}