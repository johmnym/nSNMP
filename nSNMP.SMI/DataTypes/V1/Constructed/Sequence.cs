
namespace nSNMP.SMI.DataTypes.V1.Constructed
{
    public class Sequence : ConstructedDataType
    {
        protected Sequence(byte[] data) : base(data)
        {
        }

        public static Sequence Create(byte[] data)
        {
            var sequence = new Sequence(data);

            sequence.Initialize();

            return sequence;
        }
    }
}