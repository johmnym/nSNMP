
using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    public record GetRequest(byte[]? Data, Integer? RequestId, Integer? Error, Integer? ErrorIndex, Sequence? VarbindList) : PDU(Data, RequestId, Error, ErrorIndex, VarbindList)
    {
        protected override SnmpDataType PduType => SnmpDataType.GetRequestPDU;
        public GetRequest() : this(null, null, null, null, new Sequence(new IDataType[]{}))
        {
        }

        public static GetRequest Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var requestId = (Integer)SMIDataFactory.Create(ref memory);
            var error = (Integer)SMIDataFactory.Create(ref memory);
            var errorIndex = (Integer)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new GetRequest(data, requestId, error, errorIndex, varbindList);
        }
    }
}
