using System;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// Represents an Opaque SNMP data type - arbitrary binary data
    /// </summary>
    public record Opaque(byte[]? Data) : PrimitiveDataType(Data)
    {
        public static Opaque Create(byte[] data)
        {
            return new Opaque(data);
        }

        public static Opaque Create(ReadOnlySpan<byte> data)
        {
            return new Opaque(data.ToArray());
        }

        public byte[] Value
        {
            get { return Data ?? Array.Empty<byte>(); }
        }

        public override byte[] ToBytes()
        {
            var data = Data ?? Array.Empty<byte>();
            return BEREncoder.EncodeTLV((byte)SnmpDataType.Opaque, data);
        }

        public static implicit operator byte[](Opaque opaque)
        {
            return opaque.Value;
        }

        public override string ToString()
        {
            if (Data == null || Data.Length == 0)
                return "0x";

            return "0x" + Convert.ToHexString(Data);
        }
    }
}