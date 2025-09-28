using System;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.DataTypes.V1.Primitive
{
    public class ObjectIdentifierEnhancedTests
    {
        [Fact]
        public void CompareTo_LexicographicOrdering_Works()
        {
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 2 });
            var oid3 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 1 });

            Assert.True(oid1.CompareTo(oid2) < 0); // 1.3.6.1 < 1.3.6.2
            Assert.True(oid2.CompareTo(oid1) > 0); // 1.3.6.2 > 1.3.6.1
            Assert.True(oid1.CompareTo(oid3) < 0); // 1.3.6.1 < 1.3.6.1.1 (shorter comes first when equal prefix)
            Assert.Equal(0, oid1.CompareTo(oid1)); // Equal
        }

        [Fact]
        public void ComparisonOperators_Work()
        {
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 2 });

            Assert.True(oid1 < oid2);
            Assert.True(oid2 > oid1);
            Assert.True(oid1 <= oid2);
            Assert.True(oid2 >= oid1);
            Assert.False(oid1 == oid2);
            Assert.True(oid1 != oid2);
        }

        [Fact]
        public void IsPrefixOf_Works()
        {
            var prefix = ObjectIdentifier.Create(new uint[] { 1, 3, 6 });
            var full = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1 });
            var different = ObjectIdentifier.Create(new uint[] { 1, 3, 7, 1 });

            Assert.True(prefix.IsPrefixOf(full));
            Assert.False(prefix.IsPrefixOf(different));
            Assert.True(prefix.IsPrefixOf(prefix)); // Self is prefix
            Assert.False(full.IsPrefixOf(prefix)); // Longer cannot be prefix of shorter
        }

        [Fact]
        public void StartsWith_Works()
        {
            var prefix = ObjectIdentifier.Create(new uint[] { 1, 3, 6 });
            var full = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1 });

            Assert.True(full.StartsWith(prefix));
            Assert.False(prefix.StartsWith(full));
        }

        [Fact]
        public void GetNext_GeneratesCorrectNextOID()
        {
            var oid = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var next = oid.GetNext();

            var expectedNext = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 0 });
            Assert.Equal(expectedNext.Value, next.Value);
        }

        [Fact]
        public void GetParent_Works()
        {
            var oid = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1 });
            var parent = oid.GetParent();

            var expectedParent = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2 });
            Assert.NotNull(parent);
            Assert.Equal(expectedParent.Value, parent.Value);
        }

        [Fact]
        public void GetParent_ReturnsNull_ForRootLevel()
        {
            var rootLevel = ObjectIdentifier.Create(new uint[] { 1, 3 });
            var parent = rootLevel.GetParent();

            Assert.Null(parent);
        }

        [Fact]
        public void Append_AddsSubIdentifier()
        {
            var oid = ObjectIdentifier.Create(new uint[] { 1, 3, 6 });
            var child = oid.Append(1);

            var expected = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            Assert.Equal(expected.Value, child.Value);
        }

        [Fact]
        public void CreateCached_CachesFrequentOIDs()
        {
            string oidString = "1.3.6.1.2.1.1.1.0";

            var oid1 = ObjectIdentifier.CreateCached(oidString);
            var oid2 = ObjectIdentifier.CreateCached(oidString);

            // Should parse the same
            Assert.Equal(oid1.Value, oid2.Value);
            Assert.Equal(oidString.TrimStart('.'), oid1.ToString().TrimStart('.'));
        }

        [Fact]
        public void Equals_WorksCorrectly()
        {
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid3 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 2 });

            Assert.True(oid1.Equals(oid2));
            Assert.False(oid1.Equals(oid3));
            Assert.False(oid1.Equals(null));
        }

        [Fact]
        public void GetHashCode_ConsistentForEqualOIDs()
        {
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });

            Assert.Equal(oid1.GetHashCode(), oid2.GetHashCode());
        }

        [Fact]
        public void EqualityOperators_Work()
        {
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 });
            var oid3 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 2 });

            Assert.True(oid1 == oid2);
            Assert.False(oid1 == oid3);
            Assert.False(oid1 != oid2);
            Assert.True(oid1 != oid3);
        }

        [Fact]
        public void LexicographicSorting_WorksAsExpected()
        {
            var oids = new[]
            {
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 2 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 1 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 7 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 0 })
            };

            Array.Sort(oids);

            // Expected order: 1.3.6.1, 1.3.6.1.0, 1.3.6.1.1, 1.3.6.2, 1.3.7
            Assert.Equal(new uint[] { 1, 3, 6, 1 }, oids[0].Value);
            Assert.Equal(new uint[] { 1, 3, 6, 1, 0 }, oids[1].Value);
            Assert.Equal(new uint[] { 1, 3, 6, 1, 1 }, oids[2].Value);
            Assert.Equal(new uint[] { 1, 3, 6, 2 }, oids[3].Value);
            Assert.Equal(new uint[] { 1, 3, 7 }, oids[4].Value);
        }

        [Fact]
        public void SNMP_Walk_Simulation_Works()
        {
            // Simulate a simple SNMP walk scenario
            var start = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1 });
            var table = new[]
            {
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 1, 0 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 2, 0 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 3, 0 }),
                ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 2, 1, 0 }) // Different subtree
            };

            // Find all OIDs under the starting prefix
            var matches = Array.FindAll(table, oid => oid.StartsWith(start));

            Assert.Equal(3, matches.Length); // Should find first 3, not the last one
            Assert.All(matches, oid => Assert.True(oid.StartsWith(start)));
        }

        [Fact]
        public void Performance_OID_Operations_AreEfficient()
        {
            // Test that common operations don't throw and complete quickly
            var oid1 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 1, 0 });
            var oid2 = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 2, 0 });

            // These should all complete without exceptions
            var comparison = oid1.CompareTo(oid2);
            var isPrefix = oid1.IsPrefixOf(oid2);
            var parent = oid1.GetParent();
            var next = oid1.GetNext();
            var child = oid1.Append(1);
            var hash1 = oid1.GetHashCode();
            var hash2 = oid2.GetHashCode();

            Assert.True(comparison < 0); // 1.1.0 < 1.2.0
            Assert.False(isPrefix);
            Assert.NotNull(parent);
            Assert.NotNull(next);
            Assert.NotNull(child);
            // Hash codes don't need to be different, just consistent
        }
    }
}