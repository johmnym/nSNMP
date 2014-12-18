using System;
using System.IO;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
using nSNMP.SMI.X690;

namespace nSNMP.SMI
{
    public static class SMIDataFactory
    {
        public static SimpleDataType Create(MemoryStream dataStream)
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

                default: throw new Exception();
            }
        }
    }
}