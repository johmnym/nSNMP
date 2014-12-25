using System;
using System.IO;
using nSNMP.SMI.Message;
using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
using nSNMP.SMI.X690;

namespace nSNMP.SMI
{
    public static class SMIDataFactory
    {
        public static SnmpMessage CreateSnmpMessage(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                SnmpDataType type = BERParser.ParseType(stream);

                if (type != SnmpDataType.Sequence) throw new Exception();

                return (SnmpMessage)Create(stream, SnmpDataType.SnmpMessage);
            }
        }

        public static IDataType Create(MemoryStream dataStream)
        {
            SnmpDataType type = BERParser.ParseType(dataStream);

            return Create(dataStream, type);
        }

        public static IDataType Create(MemoryStream dataStream, SnmpDataType type)
        {
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
                    return GetResponseSnmpPdu.Create(data);

                case SnmpDataType.SnmpMessage:
                    return SnmpMessage.Create(data);

                case SnmpDataType.VarbindsList:
                    return VarbindList.Create(data);

                default: throw new Exception();
            }
        }

    }
}