using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class Error : Integer
    {
        public Error(SnmpDataType type, byte[] data) : base(data)
        {
        }

        //public ErrorCodes ErrorCode { get { return (ErrorCodes) Value; } }
    }

    public enum ErrorCodes
    {
        NoError = 0,
        ResponseToLarge = 1,
        NameNotFound = 2,
        DataTypeMismatch = 3,
        ReadOnlyField = 4,
        GeneralError = 5
    }
}
