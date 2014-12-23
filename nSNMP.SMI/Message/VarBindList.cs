using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;

namespace nSNMP.SMI.Message
{
    public class VarbindList : Sequence
    {
        public VarbindList(byte[] data) : base(data)
        {
        }
    }
}
