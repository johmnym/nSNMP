using System.Collections.Generic;
using nSNMP.SMI.Message;

namespace nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes
{
    public class Sequence : ComplexDataType
    {
        public List<IDataType> Elements { get; private set; }
 
        public Sequence(byte[] data) : base(data)
        {
            Elements = new List<IDataType>();
        }

        public SnmpMessage ToSnmpMessage()
        {
            return new SnmpMessage(Data);
        }
    }
}