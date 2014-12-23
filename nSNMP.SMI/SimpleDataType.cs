
namespace nSNMP.SMI
{
    public abstract class SimpleDataType : IDataType
    {  
        public byte[] Data;

        protected SimpleDataType(byte[] data)
        {
            Data = data;
        }
    }
}