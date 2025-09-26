using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    public abstract class PDU : IDataType
    {
        protected PDU()
        {
            
        }

        protected PDU(byte[] data)
        {
            Data = data;
        }

        public byte[]? Data { get; private set; }

        public Integer? RequestId { get; set; }

        public Integer? Error { get; set; }

        public Integer? ErrorIndex { get; set; }

        public Sequence? VarbindList { get; set; } = new Sequence();

        protected void Initialize()
        {
            if (Data == null) return;
            var dataStream = new MemoryStream(Data);

            RequestId = (Integer)SMIDataFactory.Create(dataStream);
            Error = (Integer)SMIDataFactory.Create(dataStream);
            ErrorIndex = (Integer)SMIDataFactory.Create(dataStream);
            VarbindList = (Sequence)SMIDataFactory.Create(dataStream);
        }
    }
}
