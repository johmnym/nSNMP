using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    /// <summary>
    /// SNMPv2c/v3 Trap PDU (also called Notification) - uses same structure as other v2c PDUs
    /// </summary>
    public record TrapV2(byte[]? Data, Integer? RequestId, Integer? Error, Integer? ErrorIndex, Sequence? VarbindList) : PDU(Data, RequestId, Error, ErrorIndex, VarbindList)
    {
        protected override SnmpDataType PduType => SnmpDataType.TrapV2PDU;

        public TrapV2() : this(null, null, null, null, new Sequence(new IDataType[] { }))
        {
        }

        public static TrapV2 Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var requestId = (Integer)SMIDataFactory.Create(ref memory);
            var error = (Integer)SMIDataFactory.Create(ref memory);
            var errorIndex = (Integer)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new TrapV2(data, requestId, error, errorIndex, varbindList);
        }
    }
}