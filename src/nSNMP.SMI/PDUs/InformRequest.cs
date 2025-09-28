using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    /// <summary>
    /// InformRequest PDU - SNMPv2c/v3 manager-to-manager notification with acknowledgment
    /// </summary>
    public record InformRequest(byte[]? Data, Integer? RequestId, Integer? Error, Integer? ErrorIndex, Sequence? VarbindList) : PDU(Data, RequestId, Error, ErrorIndex, VarbindList)
    {
        protected override SnmpDataType PduType => SnmpDataType.InformRequestPDU;

        public InformRequest() : this(null, null, null, null, new Sequence(new IDataType[] { }))
        {
        }

        public static InformRequest Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var requestId = (Integer)SMIDataFactory.Create(ref memory);
            var error = (Integer)SMIDataFactory.Create(ref memory);
            var errorIndex = (Integer)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new InformRequest(data, requestId, error, errorIndex, varbindList);
        }
    }
}