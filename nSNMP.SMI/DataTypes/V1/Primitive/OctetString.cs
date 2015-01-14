using System;
using System.Text;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public class OctetString : PrimitiveDataType
    {
        private Encoding _encoding;

        public OctetString(byte[] data) : base(data)
        {
            SetDefaultEncoding();
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
                if (_encoding == null)
                {
                    throw new Exception();
                }

                return _encoding.GetString(Data, 0, Data.Length);
            }
        }

        private void SetDefaultEncoding()
        {
            _encoding = Encoding.GetEncoding("ASCII");
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
