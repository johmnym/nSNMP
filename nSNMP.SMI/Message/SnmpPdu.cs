using System.Collections.Generic;

namespace nSNMP.SMI.Message
{
    public class SnmpPdu
    {
        public RequestId RequestId { get; set; }
        public Error Error { get; set; }
        public ErrorIndex ErrorIndex { get; set; }
        public List<VarBind> VarBinds { get; set; }
    }
}