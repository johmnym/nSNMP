using System.IO;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.SMI.PDUs
{
    /// <summary>
    /// SNMPv3 Report PDU - used for error reporting and engine discovery
    /// </summary>
    public record Report(byte[]? Data, Integer? RequestId, Integer? Error, Integer? ErrorIndex, Sequence? VarbindList) : PDU(Data, RequestId, Error, ErrorIndex, VarbindList)
    {
        protected override SnmpDataType PduType => SnmpDataType.ReportPDU;

        public Report() : this(null, null, null, null, new Sequence(new IDataType[] { }))
        {
        }

        public static Report Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var requestId = (Integer)SMIDataFactory.Create(ref memory);
            var error = (Integer)SMIDataFactory.Create(ref memory);
            var errorIndex = (Integer)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new Report(data, requestId, error, errorIndex, varbindList);
        }
    }
}