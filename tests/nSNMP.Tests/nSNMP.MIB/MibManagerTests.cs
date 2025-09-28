using nSNMP.MIB;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.nSNMP.MIB
{
    public class MibManagerTests
    {
        [Fact]
        public void MibManager_InitializesWithStandardObjects()
        {
            var manager = new MibManager();

            Assert.NotNull(manager.Tree);
            Assert.True(manager.Modules.Count > 0);

            // Should have standard MIB-2 objects
            var sysDescr = manager.GetObject("sysDescr");
            Assert.NotNull(sysDescr);
            Assert.Equal("1.3.6.1.2.1.1.1.0", sysDescr.GetOidPath());
        }

        [Fact]
        public void OidToName_ResolvesStandardObjects()
        {
            var manager = new MibManager();

            var name = manager.OidToName("1.3.6.1.2.1.1.1.0");
            Assert.Equal("sysDescr", name);

            var name2 = manager.OidToName("1.3.6.1.2.1.1.3.0");
            Assert.Equal("sysUpTime", name2);
        }

        [Fact]
        public void NameToOid_ResolvesStandardObjects()
        {
            var manager = new MibManager();

            var oid = manager.NameToOid("sysDescr");
            Assert.NotNull(oid);
            var oidString = oid.ToString();
            Assert.Equal("1.3.6.1.2.1.1.1.0", oidString.StartsWith(".") ? oidString.Substring(1) : oidString);

            var oid2 = manager.NameToOid("sysUpTime");
            Assert.NotNull(oid2);
            var oidString2 = oid2.ToString();
            Assert.Equal("1.3.6.1.2.1.1.3.0", oidString2.StartsWith(".") ? oidString2.Substring(1) : oidString2);
        }

        [Fact]
        public void GetStats_ReturnsValidStatistics()
        {
            var manager = new MibManager();
            var stats = manager.GetStats();

            Assert.True(stats.TotalObjects > 0);
            Assert.True(stats.TotalModules > 0);
            Assert.True(stats.ObjectsWithOids > 0);
        }

        [Fact]
        public void SearchObjects_FindsMatchingObjects()
        {
            var manager = new MibManager();

            var results = manager.SearchObjects("sys.*").ToList();
            Assert.True(results.Count > 0);
            Assert.True(results.All(obj => obj.Name.StartsWith("sys", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void GetNextOid_ReturnsCorrectNextOid()
        {
            var manager = new MibManager();

            var currentOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var nextOid = manager.GetNextOid(currentOid);

            Assert.NotNull(nextOid);
            Assert.NotEqual(currentOid.ToString(), nextOid.ToString());
        }

        [Fact]
        public void GetSubtree_ReturnsCorrectObjects()
        {
            var manager = new MibManager();

            var systemOid = ObjectIdentifier.Create("1.3.6.1.2.1.1");
            var subtree = manager.GetSubtree(systemOid).ToList();

            Assert.True(subtree.Count > 0);
            Assert.True(subtree.All(obj => {
                var oidString = obj.Oid?.ToString();
                var normalizedOid = oidString?.StartsWith(".") == true ? oidString.Substring(1) : oidString;
                return normalizedOid?.StartsWith("1.3.6.1.2.1.1.") == true;
            }));
        }

        [Fact]
        public void MibObject_PropertiesSetCorrectly()
        {
            var obj = new MibObject("testObject");

            Assert.Equal("testObject", obj.Name);
            Assert.Equal(Access.NotAccessible, obj.Access);
            Assert.Equal(Status.Current, obj.Status);
            Assert.Empty(obj.Children);
        }

        [Fact]
        public void MibObject_TreeStructure_WorksCorrectly()
        {
            var parent = new MibObject("parent");
            var child = new MibObject("child");

            parent.AddChild(child);

            Assert.Equal(parent, child.Parent);
            Assert.Contains(child, parent.Children);
        }
    }
}