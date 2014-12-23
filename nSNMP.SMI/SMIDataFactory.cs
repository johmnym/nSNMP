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
                    return new Sequence(data);

                case SnmpDataType.GetResponsePDU:
                    return GetResponseSnmpPdu.Create(data);

                default: throw new Exception();
            }
        }
    }
}