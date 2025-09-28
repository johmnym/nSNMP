using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// SNMP NoSuchObject exception value (RFC 1905)
    /// </summary>
    public record NoSuchObject() : PrimitiveDataType(Array.Empty<byte>())
    {
        public override string ToString() => "noSuchObject";

        public override byte[] ToBytes()
        {
            return BEREncoder.EncodeTLV((byte)SnmpDataType.NoSuchObject, Array.Empty<byte>());
        }
    }
}