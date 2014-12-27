using System.Collections.Generic;
using System.IO;

namespace nSNMP.SMI.DataTypes
{
    public abstract class ConstructedDataType : IDataType
    {
        public byte[] Data { get; private set; }

        public List<IDataType> Elements { get; protected set; }

        protected ConstructedDataType(byte[] data)
        {
            Data = data;
            Elements = new List<IDataType>();
        }

        protected void Initialize()
        {
            using (var dataStream = new MemoryStream(Data))
            {
                while (dataStream.Position < Data.Length)
                {
                    IDataType item = SMIDataFactory.Create(dataStream);

                    Elements.Add(item);
                }
            }
        }
    }
}
