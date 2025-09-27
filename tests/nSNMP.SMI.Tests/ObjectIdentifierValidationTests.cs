using System;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.SMI.Tests
{
    public class ObjectIdentifierValidationTests
    {
        [Fact]
        public void Create_ThrowsOnNullArray()
        {
            uint[]? nullArray = null;

            Assert.Throws<ArgumentNullException>(() => ObjectIdentifier.Create(nullArray!));
        }

        [Fact]
        public void Create_ThrowsOnEmptyArray()
        {
            uint[] emptyArray = new uint[0];

            var ex = Assert.Throws<ArgumentException>(() => ObjectIdentifier.Create(emptyArray));
            Assert.Contains("at least one component", ex.Message);
        }

        [Fact]
        public void Create_ThrowsOnSingleElementArray()
        {
            uint[] singleElement = new uint[] { 1 };

            var ex = Assert.Throws<ArgumentException>(() => ObjectIdentifier.Create(singleElement));
            Assert.Contains("at least two components", ex.Message);
        }

        [Fact]
        public void Create_SucceedsWithTwoElements()
        {
            uint[] twoElements = new uint[] { 1, 3 };

            var oid = ObjectIdentifier.Create(twoElements);

            Assert.NotNull(oid);
            var value = oid.Value;
            Assert.Equal(2, value.Length);
            Assert.Equal(1u, value[0]);
            Assert.Equal(3u, value[1]);
        }

        [Fact]
        public void Create_SucceedsWithMultipleElements()
        {
            uint[] elements = new uint[] { 1, 3, 6, 1, 2, 1 };

            var oid = ObjectIdentifier.Create(elements);

            Assert.NotNull(oid);
            var value = oid.Value;
            Assert.Equal(6, value.Length);
            Assert.Equal(elements, value);
        }

        [Fact]
        public void CreateFromString_ThrowsOnNull()
        {
            string? nullString = null;

            Assert.Throws<ArgumentNullException>(() => ObjectIdentifier.Create(nullString!));
        }

        [Fact]
        public void CreateFromString_ParsesDottedNotation()
        {
            string oidString = "1.3.6.1.2.1";

            var oid = ObjectIdentifier.Create(oidString);

            Assert.NotNull(oid);
            var value = oid.Value;
            Assert.Equal(6, value.Length);
            Assert.Equal(new uint[] { 1, 3, 6, 1, 2, 1 }, value);
        }

        [Fact]
        public void CreateFromString_HandlesLeadingDot()
        {
            string oidString = ".1.3.6.1.2.1";

            var oid = ObjectIdentifier.Create(oidString);

            Assert.NotNull(oid);
            var value = oid.Value;
            Assert.Equal(6, value.Length);
            Assert.Equal(new uint[] { 1, 3, 6, 1, 2, 1 }, value);
        }

        [Fact]
        public void ToString_ReturnsCorrectDottedNotation()
        {
            var oid = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1 });

            var result = oid.ToString();

            Assert.Equal(".1.3.6.1.2.1", result);
        }
    }
}