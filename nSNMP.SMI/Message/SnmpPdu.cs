using System;
using System.IO;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.Message
{
    public abstract class SnmpPdu : ComplexDataType
    {
        public RequestId RequestId { get; set; }
        public Error Error { get; set; }
        public ErrorIndex ErrorIndex { get; set; }
        public VarbindList VarBinds { get; set; }

        protected SnmpPdu(byte[] data) : base(data)
        {
        }

        protected void Initialize()
        {
            using (var dataStream = new MemoryStream(Data))
            {
                ReadRequestId(dataStream);
                ReadError(dataStream);
                ReadErrorIndex(dataStream);
                ReadVarbindsList(dataStream);
            }
        }

        protected void ReadRequestId(MemoryStream dataStream)
        {
            var integer = (Integer)SMIDataFactory.Create(dataStream);

            RequestId = new RequestId(integer.Data);
        }

        protected void ReadError(MemoryStream dataStream)
        {
            var integer = (Integer)SMIDataFactory.Create(dataStream);

            Error = new Error(integer.Data);
        }

        protected void ReadErrorIndex(MemoryStream dataStream)
        {
            var integer = (Integer)SMIDataFactory.Create(dataStream);

            ErrorIndex = new ErrorIndex(integer.Data);
        }

        protected void ReadVarbindsList(MemoryStream dataStream)
        {
            SnmpDataType type = BERParser.ParseType(dataStream);

            if (type != SnmpDataType.Sequence) throw new Exception();

            VarBinds = (VarbindList)SMIDataFactory.Create(dataStream, SnmpDataType.VarbindsList);
        }
    }
}
