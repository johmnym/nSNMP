using System.IO;

namespace nSNMP.SMI
{
    public interface IDataType
    {
        
    }

    public abstract class SimpleDataType : IDataType
    {  
        protected byte[] Data;

        public MemoryStream DataStream { get { return new MemoryStream(Data); } }

        //public int Length { get {return BERParser.ParseLength(Data);} }

        protected SimpleDataType(byte[] data)
        {
            Data = data;
        }
    }
}