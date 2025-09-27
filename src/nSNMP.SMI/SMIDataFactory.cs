using System;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.X690;

namespace nSNMP.SMI
{
    public static class SMIDataFactory
    {
        public static IDataType Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            return Create(ref memory);
        }

        public static IDataType Create(ref ReadOnlyMemory<byte> memory)
        {
            SnmpDataType type = BERParser.ParseType(ref memory);

            int length = BERParser.ParseLengthOfNextDataField(ref memory);

            ReadOnlyMemory<byte> data = BERParser.ParseDataField(ref memory, length);

            switch (type)
            {
                case SnmpDataType.Integer:
                    return new Integer(data.ToArray());

                case SnmpDataType.OctetString:
                    return new OctetString(data.ToArray());

                case SnmpDataType.Null:
                    return new Null();

                case SnmpDataType.ObjectIdentifier:
                    return new ObjectIdentifier(data.ToArray());

                case SnmpDataType.Sequence:
                    return Sequence.Create(data.ToArray());

                case SnmpDataType.GetResponsePDU:
                    return GetResponse.Create(data.ToArray());

                case SnmpDataType.GetRequestPDU:
                    return GetRequest.Create(data.ToArray());

                case SnmpDataType.SetRequestPDU:
                    return SetRequest.Create(data.ToArray());

                case SnmpDataType.GetNextRequestPDU:
                    return GetNextRequest.Create(data.ToArray());

                case SnmpDataType.GetBulkRequestPDU:
                    return GetBulkRequest.Create(data.ToArray());

                case SnmpDataType.InformRequestPDU:
                    return InformRequest.Create(data.ToArray());

                case SnmpDataType.TrapPDU:
                    return TrapV1.Create(data.ToArray());

                case SnmpDataType.TrapV2PDU:
                    return TrapV2.Create(data.ToArray());

                case SnmpDataType.ReportPDU:
                    return Report.Create(data.ToArray());

                case SnmpDataType.IpAddress:
                    return new DataTypes.V1.Primitive.IpAddress(data.ToArray());

                case SnmpDataType.Counter32:
                    return new DataTypes.V1.Primitive.Counter32(data.ToArray());

                case SnmpDataType.Gauge32:
                    return new DataTypes.V1.Primitive.Gauge32(data.ToArray());

                case SnmpDataType.TimeTicks:
                    return new DataTypes.V1.Primitive.TimeTicks(data.ToArray());

                case SnmpDataType.Opaque:
                    return new DataTypes.V1.Primitive.Opaque(data.ToArray());

                case SnmpDataType.Counter64:
                    return new DataTypes.V1.Primitive.Counter64(data.ToArray());

                case SnmpDataType.NoSuchObject:
                    return new DataTypes.V1.Primitive.NoSuchObject();

                case SnmpDataType.NoSuchInstance:
                    return new DataTypes.V1.Primitive.NoSuchInstance();

                case SnmpDataType.EndOfMibView:
                    return new DataTypes.V1.Primitive.EndOfMibView();

                default: throw new Exception($"Unknown SNMP data type: {type}");
            }
        }

    }
}