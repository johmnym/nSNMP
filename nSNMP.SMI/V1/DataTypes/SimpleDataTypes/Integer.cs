
namespace nSNMP.SMI.V1.DataTypes.SimpleDataTypes
{
    public class Integer : SimpleDataType
    {
        public Integer( byte[] data) : base(data)
        {

        }

        public int Value
        {
            get
            {
                var value = ((Data[0] & 0x80) == 0x80) ? -1 : 0;

                for (var j = 0; j < Data.Length; j++)
                {
                    value = (value << 8) | Data[j];
                }

                return value;
            }
        }

        public static implicit operator int(Integer integer)
        {
            return integer.Value;
        }
    }
}
