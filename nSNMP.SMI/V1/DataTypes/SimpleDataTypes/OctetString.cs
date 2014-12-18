using System;
using System.Text;

namespace nSNMP.SMI.V1.DataTypes.SimpleDataTypes
{
    public class OctetString : SimpleDataType
    {
        private readonly Encoding _encoding;

        public OctetString(byte[] data) : base(data)
        {
            _encoding = Encoding.GetEncoding("ASCII");
        }

        public override string ToString()
        {
            return ToString(_encoding);
        }

        public string ToString(Encoding encoding)
        {
            if (encoding == null)
            {
                throw new Exception();
            }

            return encoding.GetString(Data, 0, Data.Length);
        }
    }
}
