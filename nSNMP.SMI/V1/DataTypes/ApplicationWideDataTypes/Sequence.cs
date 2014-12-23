using System.Collections.Generic;

namespace nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes
{
    public class Sequence : IDataType
    {
        private List<IDataType> _elements;
        private byte[] _data;
 
        public Sequence(byte[] data)
        {
            _data = data;
            _elements = new List<IDataType>();
        }

    }
}