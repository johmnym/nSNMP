namespace nSNMP.Core
{
    /// <summary>
    /// Base exception for SNMP protocol errors
    /// </summary>
    public class SnmpException : Exception
    {
        public SnmpException(string message) : base(message) { }
        public SnmpException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when SNMP request times out
    /// </summary>
    public class SnmpTimeoutException : SnmpException
    {
        public TimeSpan Timeout { get; }

        public SnmpTimeoutException(TimeSpan timeout)
            : base($"SNMP request timed out after {timeout}")
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Exception thrown when SNMP agent returns an error
    /// </summary>
    public class SnmpErrorException : SnmpException
    {
        public int ErrorStatus { get; }
        public int ErrorIndex { get; }

        public SnmpErrorException(int errorStatus, int errorIndex, string message)
            : base(message)
        {
            ErrorStatus = errorStatus;
            ErrorIndex = errorIndex;
        }

        public static SnmpErrorException FromErrorStatus(int errorStatus, int errorIndex)
        {
            var message = errorStatus switch
            {
                1 => "tooBig - Response too large for transport",
                2 => "noSuchName - Variable does not exist",
                3 => "badValue - Invalid value for variable",
                4 => "readOnly - Variable is read-only",
                5 => "genErr - General error",
                _ => $"Unknown error status: {errorStatus}"
            };

            return new SnmpErrorException(errorStatus, errorIndex, message);
        }
    }
}