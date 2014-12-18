namespace nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes
{
    public class Sequence : IDataType
    {
        public Sequence(SnmpDataType type, byte[] data)
        {
        }

        public SimpleDataType Create(byte[] data)
        {
            throw new System.NotImplementedException();
        }
    }
}
