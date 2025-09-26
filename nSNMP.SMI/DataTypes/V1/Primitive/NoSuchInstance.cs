using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// SNMP NoSuchInstance exception value (RFC 1905)
    /// </summary>
    public record NoSuchInstance() : PrimitiveDataType(Array.Empty<byte>())
    {
        public override string ToString() => "noSuchInstance";

        public override byte[] ToBytes()
        {
            return BEREncoder.EncodeTLV((byte)SnmpDataType.NoSuchInstance, Array.Empty<byte>());
        }
    }
}