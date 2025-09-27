using System;
using System.Collections.Generic;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// Represents a Gauge32 SNMP data type - a 32-bit unsigned integer gauge
    /// </summary>
    public record Gauge32(byte[] Data) : PrimitiveDataType(Data)
    {
        public static Gauge32 Create(uint value)
        {
            if (value > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Gauge32 value cannot exceed 4294967295");

            byte[] data = EncodeUInt32(value);
            return new Gauge32(data);
        }

        public uint Value
        {
            get { return DecodeUInt32(); }
        }

        private uint DecodeUInt32()
        {
            if (Data == null || Data.Length == 0) return 0;

            uint value = 0;
            for (var j = 0; j < Data.Length; j++)
            {
                value = (value << 8) | Data[j];
            }
            return value;
        }

        private static byte[] EncodeUInt32(uint value)
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
            var valueBytes = EncodeUInt32(Value);
            return BEREncoder.EncodeTLV((byte)SnmpDataType.Gauge32, valueBytes);
        }

        public static implicit operator uint(Gauge32 gauge)
        {
            return gauge.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}