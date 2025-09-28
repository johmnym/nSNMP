using System;
using System.Net;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.DataTypes.V1.Primitive
{
    public class NewDataTypesTests
    {
        [Fact]
        public void Counter32_RoundTrip_Works()
        {
            var original = Counter32.Create(4294967295);

            var encoded = original.ToBytes();
            var decoded = (Counter32)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Counter32_Zero_RoundTrip_Works()
        {
            var original = Counter32.Create(0);

            var encoded = original.ToBytes();
            var decoded = (Counter32)SMIDataFactory.Create(encoded);

            Assert.Equal(0u, decoded.Value);
        }

        [Fact]
        public void Gauge32_RoundTrip_Works()
        {
            var original = Gauge32.Create(1234567890);

            var encoded = original.ToBytes();
            var decoded = (Gauge32)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Counter64_RoundTrip_Works()
        {
            var original = Counter64.Create(18446744073709551615);

            var encoded = original.ToBytes();
            var decoded = (Counter64)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void TimeTicks_RoundTrip_Works()
        {
            var original = TimeTicks.Create(123456);

            var encoded = original.ToBytes();
            var decoded = (TimeTicks)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void TimeTicks_FromTimeSpan_Works()
        {
            var timeSpan = TimeSpan.FromSeconds(30.5); // 30.5 seconds = 3050 centiseconds
            var original = TimeTicks.Create(timeSpan);

            var encoded = original.ToBytes();
            var decoded = (TimeTicks)SMIDataFactory.Create(encoded);

            Assert.Equal(3050u, decoded.Value);
            Assert.Equal(timeSpan.TotalSeconds, decoded.TimeSpan.TotalSeconds, 2); // Allow for rounding
        }

        [Fact]
        public void IpAddress_RoundTrip_Works()
        {
            var ipAddr = IPAddress.Parse("192.168.1.1");
            var original = global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create(ipAddr);

            var encoded = original.ToBytes();
            var decoded = (global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value.ToString(), decoded.Value.ToString());
        }

        [Fact]
        public void IpAddress_FromString_Works()
        {
            var original = global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create("10.0.0.1");

            var encoded = original.ToBytes();
            var decoded = (global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress)SMIDataFactory.Create(encoded);

            Assert.Equal("10.0.0.1", decoded.Value.ToString());
        }

        [Fact]
        public void IpAddress_FromBytes_Works()
        {
            var original = global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create(172, 16, 0, 1);

            var encoded = original.ToBytes();
            var decoded = (global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress)SMIDataFactory.Create(encoded);

            Assert.Equal("172.16.0.1", decoded.Value.ToString());
        }

        [Fact]
        public void Opaque_RoundTrip_Works()
        {
            var originalData = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xAB, 0xCD };
            var original = Opaque.Create(originalData);

            var encoded = original.ToBytes();
            var decoded = (Opaque)SMIDataFactory.Create(encoded);

            Assert.Equal(originalData, decoded.Value);
        }

        [Fact]
        public void Opaque_Empty_RoundTrip_Works()
        {
            var original = Opaque.Create(Array.Empty<byte>());

            var encoded = original.ToBytes();
            var decoded = (Opaque)SMIDataFactory.Create(encoded);

            Assert.Empty(decoded.Value);
        }

        [Fact]
        public void TimeTicks_ToString_Formats_Correctly()
        {
            var ticks1 = TimeTicks.Create(TimeSpan.FromSeconds(30));
            var ticks2 = TimeTicks.Create(TimeSpan.FromMinutes(5));
            var ticks3 = TimeTicks.Create(TimeSpan.FromHours(2));
            var ticks4 = TimeTicks.Create(TimeSpan.FromDays(1) + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30));

            Assert.Contains("30.00s", ticks1.ToString());
            Assert.Contains("5m", ticks2.ToString());
            Assert.Contains("2h", ticks3.ToString());
            Assert.Contains("1d", ticks4.ToString());
        }

        [Fact]
        public void Opaque_ToString_ShowsHex()
        {
            var opaque = Opaque.Create(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

            Assert.Equal("0xDEADBEEF", opaque.ToString());
        }

        [Fact]
        public void Counter32_MaxValue_Works()
        {
            var original = Counter32.Create(uint.MaxValue);

            Assert.Equal(uint.MaxValue, original.Value);
        }

        [Fact]
        public void ImplicitConversions_Work()
        {
            uint counter32Val = Counter32.Create(123);
            uint gauge32Val = Gauge32.Create(456);
            ulong counter64Val = Counter64.Create(789);
            uint timeTicksVal = TimeTicks.Create(101112);
            IPAddress ipVal = global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create("127.0.0.1");
            byte[] opaqueVal = Opaque.Create(new byte[] { 1, 2, 3 });

            Assert.Equal(123u, counter32Val);
            Assert.Equal(456u, gauge32Val);
            Assert.Equal(789ul, counter64Val);
            Assert.Equal(101112u, timeTicksVal);
            Assert.Equal("127.0.0.1", ipVal.ToString());
            Assert.Equal(new byte[] { 1, 2, 3 }, opaqueVal);
        }
    }
}