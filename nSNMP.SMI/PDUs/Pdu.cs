using System.Linq;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.PDUs
{
    public abstract record PDU(byte[]? Data, Integer? RequestId, Integer? Error, Integer? ErrorIndex, Sequence? VarbindList) : IDataType
    {
        protected abstract SnmpDataType PduType { get; }

        public virtual byte[] ToBytes()
        {
            // PDU structure: RequestId, Error, ErrorIndex, VarbindList
            var elements = new IDataType[]
            {
                RequestId ?? Integer.Create(0),
                Error ?? Integer.Create(0),
                ErrorIndex ?? Integer.Create(0),
                VarbindList ?? new Sequence(new IDataType[] { })
            };

            var childBytes = elements.SelectMany(element => element.ToBytes()).ToArray();
            return BEREncoder.EncodeTLV((byte)PduType, childBytes);
        }
    }
}
