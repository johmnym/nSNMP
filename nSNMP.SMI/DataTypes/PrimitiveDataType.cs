
namespace nSNMP.SMI.DataTypes
{
    public abstract class PrimitiveDataType : IDataType
    {  
        public byte[] Data;

        protected PrimitiveDataType(byte[] data)
        {
            Data = data;
        }
    }
}