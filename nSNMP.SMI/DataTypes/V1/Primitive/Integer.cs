
using System;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public class Integer : PrimitiveDataType
    {
        public Integer(byte[] data) : base(data)
        {

        }

        public static Integer Create(int value)
        {
            byte[] data = Encode(value);

            return new Integer(data);
        }

        public int Value
        {
            get { return Decode(); }
        }

        private int Decode()
        {
            var value = ((Data[0] & 0x80) == 0x80) ? -1 : 0;

            for (var j = 0; j < Data.Length; j++)
            {
                value = (value << 8) | Data[j];
            }

            return value;
        }

        protected static byte[] Encode(int value)
        {
            return GetRawBytes(BitConverter.GetBytes(value), value < 0);
        }

        public static implicit operator int(Integer integer)
        {
            return integer.Value;
        }
    }
}
