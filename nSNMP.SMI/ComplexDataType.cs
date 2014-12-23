using System.IO;

namespace nSNMP.SMI
{
    public abstract class ComplexDataType : IDataType
    {
        protected byte[] Data { get; private set; }

        protected ComplexDataType(byte[] data)
        {
            Data = data;
        }
    }
}
