using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// SNMP EndOfMibView exception value (RFC 1905)
    /// </summary>
    public record EndOfMibView() : PrimitiveDataType(Array.Empty<byte>())
    {
        public override string ToString() => "endOfMibView";

        public override byte[] ToBytes()
        {
            return BEREncoder.EncodeTLV((byte)SnmpDataType.EndOfMibView, Array.Empty<byte>());
        }
    }
}