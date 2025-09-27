
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Constructed
{
    public record Sequence(byte[]? Data, IReadOnlyList<IDataType> Elements) : ConstructedDataType(Data)
    {
        public override IReadOnlyList<IDataType> Elements { get; } = Elements;

        public Sequence(IReadOnlyList<IDataType> elements) : this(null, elements)
        {
        }

        public static Sequence Create(byte[] data)
        {
            var elements = new List<IDataType>();
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            while (!memory.IsEmpty)
            {
                elements.Add(SMIDataFactory.Create(ref memory));
            }

            return new Sequence(data, elements);
        }

        public override byte[] ToBytes()
        {
            // Encode all child elements and concatenate
            var childBytes = Elements.SelectMany(element => element.ToBytes()).ToArray();
            return BEREncoder.EncodeTLV((byte)SnmpDataType.Sequence, childBytes);
        }
    }
}