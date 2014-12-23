using System.IO;

namespace nSNMP.SMI
{
    public abstract class SimpleDataType : IDataType
    {  
        protected byte[] Data;

        public MemoryStream DataStream { get { return new MemoryStream(Data); } }

        protected SimpleDataType(byte[] data)
        {
            Data = data;
        }
    }
}