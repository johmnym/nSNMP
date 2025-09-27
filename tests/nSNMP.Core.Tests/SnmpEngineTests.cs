using nSNMP.Agent;
using nSNMP.Security;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class SnmpEngineTests
    {
        [Fact]
        public void Constructor_GeneratesValidEngineId()
        {
            var engine = new SnmpEngine();

            Assert.NotNull(engine.EngineId);
            Assert.Equal(13, engine.EngineId.Length); // RFC 3411 compliant length
            Assert.NotEmpty(engine.EngineIdHex);
        }

        [Fact]
        public void Constructor_WithCustomEngineId_UsesProvidedId()
        {
            var customEngineId = new byte[] { 0x80, 0x00, 0x00, 0x01, 0x01, 0x02, 0x03, 0x04 };
            var engine = new SnmpEngine(customEngineId);

            Assert.Equal(customEngineId, engine.EngineId);
        }

        [Fact]
        public void EngineTime_ProgressesOverTime()
        {
            var engine = new SnmpEngine();
            var time1 = engine.EngineTime;

            System.Threading.Thread.Sleep(1100); // Sleep > 1 second

            var time2 = engine.EngineTime;
            Assert.True(time2 > time1);
        }

        [Fact]
        public void EngineBoots_DefaultsToZero()
        {
            var engine = new SnmpEngine();
            Assert.Equal(0, engine.EngineBoots);
        }

        [Fact]
        public void IncrementBoots_IncrementsBootsAndResetsTime()
        {
            var engine = new SnmpEngine(null, 5);
            System.Threading.Thread.Sleep(1100); // Sleep longer to ensure time progresses

            var initialTime = engine.EngineTime;
            Assert.True(initialTime >= 1); // Should be at least 1 second

            engine.IncrementBoots();

            Assert.Equal(6, engine.EngineBoots);
            // After incrementing boots, engine time should be very small (near 0)
            Assert.True(engine.EngineTime <= 1); // Allow for small timing variations
        }

        [Fact]
        public void GetParameters_ReturnsCurrentState()
        {
            var engine = new SnmpEngine();
            var parameters = engine.GetParameters();

            Assert.Equal(engine.EngineId, parameters.EngineId);
            Assert.Equal(engine.EngineBoots, parameters.EngineBoots);
            Assert.Equal(engine.EngineTime, parameters.EngineTime);
        }

        [Fact]
        public void IsTimeValid_WithMatchingBootsAndValidTime_ReturnsTrue()
        {
            var engine = new SnmpEngine();
            var currentTime = engine.EngineTime;
            var currentBoots = engine.EngineBoots;

            // Time within default window (150 seconds)
            Assert.True(engine.IsTimeValid(currentBoots, currentTime));
            Assert.True(engine.IsTimeValid(currentBoots, currentTime + 100));
            Assert.True(engine.IsTimeValid(currentBoots, currentTime - 100));
        }

        [Fact]
        public void IsTimeValid_WithNonMatchingBoots_ReturnsFalse()
        {
            var engine = new SnmpEngine();
            var currentTime = engine.EngineTime;
            var currentBoots = engine.EngineBoots;

            Assert.False(engine.IsTimeValid(currentBoots + 1, currentTime));
            Assert.False(engine.IsTimeValid(currentBoots - 1, currentTime));
        }

        [Fact]
        public void IsTimeValid_WithTimeOutsideWindow_ReturnsFalse()
        {
            var engine = new SnmpEngine();
            var currentTime = engine.EngineTime;
            var currentBoots = engine.EngineBoots;

            // Time outside default window (150 seconds)
            Assert.False(engine.IsTimeValid(currentBoots, currentTime + 200));
            Assert.False(engine.IsTimeValid(currentBoots, currentTime - 200));
        }

        [Fact]
        public void IsTimeValid_WithCustomWindow_RespectsWindow()
        {
            var engine = new SnmpEngine();
            var currentTime = engine.EngineTime;
            var currentBoots = engine.EngineBoots;

            // Custom window of 50 seconds
            Assert.True(engine.IsTimeValid(currentBoots, currentTime + 40, 50));
            Assert.False(engine.IsTimeValid(currentBoots, currentTime + 60, 50));
        }

        [Fact]
        public void ToString_ReturnsEngineInfo()
        {
            var engine = new SnmpEngine();
            var info = engine.ToString();

            Assert.Contains("Engine ID:", info);
            Assert.Contains("Boots:", info);
            Assert.Contains("Time:", info);
            Assert.Contains(engine.EngineIdHex, info);
        }

        [Fact]
        public void EngineIdHex_ReturnsValidHexString()
        {
            var engine = new SnmpEngine();
            var hex = engine.EngineIdHex;

            Assert.NotNull(hex);
            Assert.True(hex.Length > 0);
            Assert.True(hex.Length % 2 == 0); // Should be even (2 chars per byte)

            // Should be valid hex
            Assert.True(hex.All(c => "0123456789ABCDEF".Contains(c)));
        }
    }
}