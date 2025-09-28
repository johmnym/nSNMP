using System;
using System.Collections.Generic;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// Represents a Counter64 SNMP data type - a 64-bit unsigned integer that can only increase
    /// </summary>
    public record Counter64(byte[] Data) : PrimitiveDataType(Data)
    {
        public static Counter64 Create(ulong value)
        {
            byte[] data = EncodeUInt64(value);
            return new Counter64(data);
        }

        public ulong Value
        {
            get { return DecodeUInt64(); }
        }

        private ulong DecodeUInt64()
        {
            if (Data == null || Data.Length == 0) return 0;

            ulong value = 0;
            for (var j = 0; j < Data.Length; j++)
            {
                value = (value << 8) | Data[j];
            }
            return value;
        }

        private static byte[] EncodeUInt64(ulong value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            var bytes = new List<byte>();
            while (value > 0)
            {
                bytes.Insert(0, (byte)(value & 0xFF));
                value >>= 8;
            }

            // Ensure the high bit is not set (to avoid negative interpretation)
            if ((bytes[0] & 0x80) != 0)
            {
                bytes.Insert(0, 0x00);
            }

            return bytes.ToArray();
        }

        public override byte[] ToBytes()
        {
            var valueBytes = EncodeUInt64(Value);
            return BEREncoder.EncodeTLV((byte)SnmpDataType.Counter64, valueBytes);
        }

        public static implicit operator ulong(Counter64 counter)
        {
            return counter.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}