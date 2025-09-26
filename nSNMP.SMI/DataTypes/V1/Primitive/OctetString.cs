using System;
using System.Text;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public class OctetString : PrimitiveDataType
    {
        public OctetString(byte[]? data) : base(data)
        {
        }

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
    }
}
