using nSNMP.Abstractions;
using Xunit;

namespace nSNMP.Abstractions.Tests
{
    public class CoreModelsTests
    {
        [Fact]
        public void SnmpVersion_ShouldHaveCorrectValues()
        {
            Assert.Equal(0, (int)SnmpVersion.V1);
            Assert.Equal(1, (int)SnmpVersion.V2c);
            Assert.Equal(3, (int)SnmpVersion.V3);
        }

        [Fact]
        public void ErrorStatus_ShouldHaveCorrectValues()
        {
            Assert.Equal(0, (int)ErrorStatus.NoError);
            Assert.Equal(1, (int)ErrorStatus.TooBig);
            Assert.Equal(2, (int)ErrorStatus.NoSuchName);
            Assert.Equal(5, (int)ErrorStatus.GenErr);
        }

        [Fact]
        public void SecurityLevel_ShouldHaveCorrectValues()
        {
            Assert.Equal(0, (int)SecurityLevel.NoAuthNoPriv);
            Assert.Equal(1, (int)SecurityLevel.AuthNoPriv);
            Assert.Equal(3, (int)SecurityLevel.AuthPriv);
        }

        [Fact]
        public void AuthProtocol_ShouldHaveCorrectValues()
        {
            Assert.Equal(0, (int)AuthProtocol.None);
            Assert.Equal(1, (int)AuthProtocol.MD5);
            Assert.Equal(2, (int)AuthProtocol.SHA1);
            Assert.Equal(4, (int)AuthProtocol.SHA256);
        }

        [Fact]
        public void PrivProtocol_ShouldHaveCorrectValues()
        {
            Assert.Equal(0, (int)PrivProtocol.None);
            Assert.Equal(1, (int)PrivProtocol.DES);
            Assert.Equal(3, (int)PrivProtocol.AES128);
            Assert.Equal(5, (int)PrivProtocol.AES256);
        }

        [Fact]
        public void PduType_ShouldHaveCorrectValues()
        {
            Assert.Equal(0xa0, (byte)PduType.Get);
            Assert.Equal(0xa1, (byte)PduType.GetNext);
            Assert.Equal(0xa2, (byte)PduType.GetResponse);
            Assert.Equal(0xa3, (byte)PduType.Set);
            Assert.Equal(0xa5, (byte)PduType.GetBulk);
            Assert.Equal(0xa7, (byte)PduType.TrapV2);
        }

        [Fact]
        public void MibAccess_ShouldHaveAllValues()
        {
            var values = Enum.GetValues<MibAccess>();
            Assert.Contains(MibAccess.Unknown, values);
            Assert.Contains(MibAccess.NotAccessible, values);
            Assert.Contains(MibAccess.ReadOnly, values);
            Assert.Contains(MibAccess.ReadWrite, values);
        }

        [Fact]
        public void MibStatus_ShouldHaveAllValues()
        {
            var values = Enum.GetValues<MibStatus>();
            Assert.Contains(MibStatus.Unknown, values);
            Assert.Contains(MibStatus.Current, values);
            Assert.Contains(MibStatus.Deprecated, values);
            Assert.Contains(MibStatus.Obsolete, values);
        }
    }
}