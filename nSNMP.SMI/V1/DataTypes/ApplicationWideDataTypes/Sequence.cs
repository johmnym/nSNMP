using System.Collections.Generic;
using System.IO;

namespace nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes
{
    public class Sequence : ComplexDataType
    {
        public List<IDataType> Elements { get; private set; }
 
        protected Sequence(byte[] data) : base(data)
        {
            Elements = new List<IDataType>();
        }

        public static Sequence Create(byte[] data)
        {
            var sequence = new Sequence(data);

            sequence.Initialize();

            return sequence;
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