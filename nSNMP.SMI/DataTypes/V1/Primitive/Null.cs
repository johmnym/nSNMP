namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public class Null : PrimitiveDataType
    {
        public Null(byte[] data) : base(data)
        {
        }

        public Null() : base(null) { }

        public string Value { get { return "NULL"; } }


        public override string ToString()
        {
            return Value;
        }
    }
}
