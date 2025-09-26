using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    /// <summary>
    /// GetBulkRequest PDU - SNMPv2c/v3 efficient bulk retrieval
    /// Uses NonRepeaters and MaxRepetitions instead of Error and ErrorIndex
    /// </summary>
    public record GetBulkRequest(byte[]? Data, Integer? RequestId, Integer? NonRepeaters, Integer? MaxRepetitions, Sequence? VarbindList) : PDU(Data, RequestId, NonRepeaters, MaxRepetitions, VarbindList)
    {
        protected override SnmpDataType PduType => SnmpDataType.GetBulkRequestPDU;

        public GetBulkRequest() : this(null, null, null, null, new Sequence(new IDataType[] { }))
        {
        }

        public static GetBulkRequest Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var requestId = (Integer)SMIDataFactory.Create(ref memory);
            var nonRepeaters = (Integer)SMIDataFactory.Create(ref memory);
            var maxRepetitions = (Integer)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new GetBulkRequest(data, requestId, nonRepeaters, maxRepetitions, varbindList);
        }

        // Convenience properties with GetBulk-specific names
        public Integer? NonRepeaters => Error; // Reuses base Error field
        public Integer? MaxRepetitions => ErrorIndex; // Reuses base ErrorIndex field
    }
}