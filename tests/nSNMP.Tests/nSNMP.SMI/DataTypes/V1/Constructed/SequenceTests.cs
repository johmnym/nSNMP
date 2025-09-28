using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Constructed;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.DataTypes.V1.Constructed
{
    public class SequenceTests
    {
        [Fact]
        public void CanSplitMessageData()
        {
            byte[] data = SnmpMessageFactory.CreateMessage();

            const int numberOfElementsInData = 3;

            var sequence = (Sequence)SMIDataFactory.Create(data);

            Assert.Equal(numberOfElementsInData, sequence.Elements.Count);
        }        
        
        [Fact]
        public void CanSplitSequenceData()
        {
            byte[] data = SnmpMessageFactory.CreateSequence();

            const int numberOfElementsInData = 2;

            var sequence = (Sequence)SMIDataFactory.Create(data);

            Assert.Equal(numberOfElementsInData, sequence.Elements.Count);
        }
    }
}
