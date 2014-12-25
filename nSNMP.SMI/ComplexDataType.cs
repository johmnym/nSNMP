
namespace nSNMP.SMI
{
    public abstract class ComplexDataType : IDataType
    {
        public byte[] Data { get; private set; }

        protected ComplexDataType(byte[] data)
        {
            Data = data;
        }
    }
}
