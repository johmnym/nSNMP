using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

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