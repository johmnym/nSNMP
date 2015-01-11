using System;
using System.IO;
using nSNMP.Message;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.X690;

namespace nSNMP
{
    public static class PDUDataFactory
    {
        public static IDataType Create(byte[] data)
        {
            using (var dataStream = new MemoryStream(data))
            {
                return Create(dataStream);
            }
        }

        public static IDataType Create(MemoryStream dataStream)
        {
            SnmpDataType type = BERParser.ParseType(dataStream);

            int length = BERParser.ParseLengthOfNextDataField(dataStream);

            byte[] data = BERParser.ParseDataField(dataStream, length);

            switch (type)
            {
                case SnmpDataType.GetResponsePDU:
                    return GetResponse.Create(data);

                case SnmpDataType.GetRequestPDU:
                    return GetRequest.Create(data);

                default: throw new Exception();
            }
        }

    }
}