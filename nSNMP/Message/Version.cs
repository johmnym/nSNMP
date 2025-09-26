﻿using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Message
{
    public class Version : Integer
    {
        public Version(byte[] data) : base(data)
        {
        }

        public SnmpVersion SnmpVersion { get { return (SnmpVersion) Value; } }

        public static Version Create(Integer integer)
        {
            return new Version(integer.Data ?? Array.Empty<byte>());
        }

        public new static Version Create(int value)
        {
            byte[] data = Encode(value);

            return new Version(data);
        }
    }
}