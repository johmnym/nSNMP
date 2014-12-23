
using System.IO;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class GetResponseSnmpPdu : SnmpPdu
    {
        private GetResponseSnmpPdu(byte[] data) : base(data)
        {
        }

        public static GetResponseSnmpPdu Create(byte[] data)
        {
            var pdu = new GetResponseSnmpPdu(data);

            using (var dataStream = new MemoryStream(pdu.Data))
            {
                pdu.RequestId = ReadRequestId(dataStream);

                pdu.Error = ReadError(dataStream);

                pdu.ErrorIndex = ReadErrorIndex(dataStream);
            }

            return pdu;
        }

        private static RequestId ReadRequestId(MemoryStream dataStream)
        {
            var integer = (Integer) SMIDataFactory.Create(dataStream);

            return new RequestId(integer.Data);
        }

        private static Error ReadError(MemoryStream dataStream)
        {
            var integer = (Integer) SMIDataFactory.Create(dataStream);

            return new Error(integer.Data);
        }

        private static ErrorIndex ReadErrorIndex(MemoryStream dataStream)
        {
            var integer = (Integer) SMIDataFactory.Create(dataStream);

            return new ErrorIndex(integer.Data);
        }
    }
}