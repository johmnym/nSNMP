using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class Version : Integer
    {
        public Version(byte[] data) : base(data)
        {
        }

        public SnmpVersion SnmpVersion { get { return (SnmpVersion) Value; } }

    }
}