using System.Collections.Generic;
using nSNMP.Message;
using Xunit;

namespace nSNMP.Tests.nSNMP.Message
{
    public class MessageFactoryTests
    {
        [Fact]
        public void CanCreateMessage()
        {
            var factory = new MessageFactory();

            SnmpMessage message = factory.CreateGetRequest()
                .WithVersion(SnmpVersion.V1)
                .WithCommunity("public")
                .WithVarbind(Varbind.Create(".1.3.6.1.4.1.55"))
                .WithVarbind(Varbind.Create(".1.3.6.1.4.1.56"))
                .Message();

            Assert.Equal("public", message.CommunityString.Value);
        }

        [Fact]
        public void Test()
        {
            var list = new List<int>(3);

            list.Insert(2, 2);
        }
    }
}
