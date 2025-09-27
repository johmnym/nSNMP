using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public record Null() : PrimitiveDataType((byte[]?)null)
    {
        public string Value { get { return "NULL"; } }


        public override string ToString()
        {
            return Value;
        }

        public override byte[] ToBytes()
        {
            return BEREncoder.EncodeTLV((byte)SnmpDataType.Null, System.Array.Empty<byte>());
        }
    }
}
