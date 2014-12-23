using System.Collections.Generic;

namespace nSNMP.SMI.Message
{
    public abstract class SnmpPdu : ComplexDataType
    {
        public RequestId RequestId { get; set; }
        public Error Error { get; set; }
        public ErrorIndex ErrorIndex { get; set; }
        public List<Varbind> VarBinds { get; set; }

        protected SnmpPdu(byte[] data) : base(data)
        {
        }
    }
}
