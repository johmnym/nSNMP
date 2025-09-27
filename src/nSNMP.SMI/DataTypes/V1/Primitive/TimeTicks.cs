using System;
using System.Collections.Generic;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// Represents a TimeTicks SNMP data type - time duration in hundredths of seconds
    /// </summary>
    public record TimeTicks(byte[] Data) : PrimitiveDataType(Data)
    {
        public static TimeTicks Create(uint centiseconds)
        {
            byte[] data = EncodeUInt32(centiseconds);
            return new TimeTicks(data);
        }

        public static TimeTicks Create(TimeSpan timeSpan)
        {
            // Convert to centiseconds (hundredths of a second)
            uint centiseconds = (uint)(timeSpan.TotalMilliseconds / 10);
            return Create(centiseconds);
        }

        public uint Value
        {
            get { return DecodeUInt32(); }
        }

        public TimeSpan TimeSpan
        {
            get { return TimeSpan.FromMilliseconds(Value * 10); }
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
            return BEREncoder.EncodeTLV((byte)SnmpDataType.TimeTicks, valueBytes);
        }

        public static implicit operator uint(TimeTicks ticks)
        {
            return ticks.Value;
        }

        public static implicit operator TimeSpan(TimeTicks ticks)
        {
            return ticks.TimeSpan;
        }

        public override string ToString()
        {
            var ts = TimeSpan;
            if (ts.TotalDays >= 1)
                return $"{ts.Days}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}.{ts.Milliseconds / 10:D2}s";
            else if (ts.TotalHours >= 1)
                return $"{ts.Hours}h {ts.Minutes:D2}m {ts.Seconds:D2}.{ts.Milliseconds / 10:D2}s";
            else if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds:D2}.{ts.Milliseconds / 10:D2}s";
            else
                return $"{ts.Seconds}.{ts.Milliseconds / 10:D2}s";
        }
    }
}