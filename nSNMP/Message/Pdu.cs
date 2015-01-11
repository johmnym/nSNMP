using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Message
{
    public abstract class Pdu : ConstructedDataType
    {
        public Integer RequestId
        {
            get { return (Integer) Elements[0]; }
            set { Elements[0] = value; }
        }
        public Integer Error { get { return (Integer)Elements[1]; } }
        public Integer ErrorIndex { get { return (Integer)Elements[2]; } }

        public Sequence VarbindList
        {
            get { return (Sequence)Elements[3]; }
            private set { Elements[3] = value; }
        }

        protected Pdu(byte[] data) : base(data)
        {

        }

        protected Pdu() : base(null)
        {
            VarbindList = new Sequence();
        }
    }
}
