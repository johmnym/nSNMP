namespace nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes
{
    public class Sequence : DataType
    {
        public Sequence(SnmpDataType type, byte[] data) : base(data)
        {
        }

        public DataType Create(byte[] data)
        {
            throw new System.NotImplementedException();
        }
    }
}
