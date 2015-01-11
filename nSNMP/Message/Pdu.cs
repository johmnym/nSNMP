using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Message
{
    public abstract class Pdu : ConstructedDataType
    {
        public Integer RequestId { get { return (Integer) Elements[0]; } }
        public Integer Error { get { return (Integer)Elements[1]; } }
        public Integer ErrorIndex { get { return (Integer)Elements[2]; } }
        public Sequence VarbindList { get { return (Sequence)Elements[3]; } }

        protected Pdu(byte[] data) : base(data)
        {
        }
    }
}
