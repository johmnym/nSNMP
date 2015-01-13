using System;
using System.IO;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.X690;

namespace nSNMP
{
    public static class SMIDataFactory
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
                case SnmpDataType.Integer:
                    return new Integer(data);

                case SnmpDataType.OctetString:
                    return new OctetString(data);

                case SnmpDataType.Null:
                    return new Null(data);

                case SnmpDataType.ObjectIdentifier:
                    return new ObjectIdentifier(data);

                case SnmpDataType.Sequence:
                    return Sequence.Create(data);

                case SnmpDataType.GetResponsePDU:
                    return GetResponse.Create(data);

                case SnmpDataType.GetRequestPDU:
                    return GetRequest.Create(data);

                default: throw new Exception();
            }
        }

    }
}