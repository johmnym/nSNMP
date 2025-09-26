using System;
using System.Text;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public record OctetString(byte[]? Data) : PrimitiveDataType(Data)
    {
        public static OctetString Create(string content)
        {
            var encoding = Encoding.GetEncoding("ASCII");
            
            byte[] data = encoding.GetBytes(content);

            return new OctetString(data);
        }

        public string Value
        {
            get
            {
                if (Data == null)
                {
                    return string.Empty;
                }

                return Encoding.ASCII.GetString(Data, 0, Data.Length);
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public override byte[] ToBytes()
        {
            var data = Data ?? Array.Empty<byte>();
            return BEREncoder.EncodeTLV((byte)SnmpDataType.OctetString, data);
        }
    }
}
