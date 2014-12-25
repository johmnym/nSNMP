using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;

namespace nSNMP.SMI.Message
{
    public class VarbindList : Sequence
    {
        protected VarbindList(byte[] data) : base(data)
        {
        }

        public new static VarbindList Create(byte[] data)
        {
            var varbindList = new VarbindList(data);

            varbindList.Initialize();

            return varbindList;
        }
    }
}
