
namespace nSNMP.SMI.DataTypes
{
    public interface IDataType
    {
        byte[]? Data { get; }

        /// <summary>
        /// Encodes this data type into BER format
        /// </summary>
        byte[] ToBytes();
    }
}