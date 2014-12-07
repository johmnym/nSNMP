using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI
{
    public static class SMIDataFactory
    {
        public static DataType Create(SnmpDataType type, byte[] data)
        {
            switch (type)
            {
                case SnmpDataType.Integer: return new Integer(data);
            }
            return null;
        }
    }
}