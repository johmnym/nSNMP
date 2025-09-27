using System.Collections.Generic;

namespace nSNMP.SMI.DataTypes
{
    public abstract record ConstructedDataType(byte[]? Data) : IDataType
    {
        public abstract IReadOnlyList<IDataType> Elements { get; }
        public abstract byte[] ToBytes();
    }
}
