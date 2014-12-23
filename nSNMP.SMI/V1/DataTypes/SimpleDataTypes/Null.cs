namespace nSNMP.SMI.V1.DataTypes.SimpleDataTypes
{
    public class Null : SimpleDataType
    {
        public Null(byte[] data) : base(data)
        {
        }

        public override string ToString()
        {
            return "NULL";
        }
    }
}
