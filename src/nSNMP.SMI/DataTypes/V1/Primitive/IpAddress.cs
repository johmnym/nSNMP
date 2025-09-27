using System;
using System.Net;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    /// <summary>
    /// Represents an IpAddress SNMP data type - IPv4 address (4 bytes)
    /// </summary>
    public record IpAddress(byte[] Data) : PrimitiveDataType(Data)
    {
        public static IpAddress Create(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 addresses are supported", nameof(ipAddress));

            byte[] addressBytes = ipAddress.GetAddressBytes();
            if (addressBytes.Length != 4)
                throw new ArgumentException("IPv4 address must be exactly 4 bytes", nameof(ipAddress));

            return new IpAddress(addressBytes);
        }

        public static IpAddress Create(string ipAddressString)
        {
            if (!IPAddress.TryParse(ipAddressString, out IPAddress? ipAddress))
                throw new ArgumentException($"Invalid IP address format: {ipAddressString}", nameof(ipAddressString));

            return Create(ipAddress);
        }

        public static IpAddress Create(byte a, byte b, byte c, byte d)
        {
            return new IpAddress(new byte[] { a, b, c, d });
        }

        public IPAddress Value
        {
            get
            {
                if (Data == null || Data.Length != 4)
                    throw new InvalidOperationException("Invalid IP address data");

                return new IPAddress(Data);
            }
        }

        public override byte[] ToBytes()
        {
            var addressBytes = Data ?? new byte[4];
            if (addressBytes.Length != 4)
                throw new InvalidOperationException("IP address must be exactly 4 bytes");

            return BEREncoder.EncodeTLV((byte)SnmpDataType.IpAddress, addressBytes);
        }

        public static implicit operator IPAddress(IpAddress snmpIpAddress)
        {
            return snmpIpAddress.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}