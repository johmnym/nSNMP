using System.IO;

namespace nSNMP.SMI
{
    public abstract class DataType
    {  
        protected byte[] Data;

        public MemoryStream DataStream { get { return new MemoryStream(Data); } }

        public int Length { get {return BERParser.ParseLength(Data);} } //funkar inte

        protected DataType(byte[] data)
        {
            Data = data;
        }
    }
}