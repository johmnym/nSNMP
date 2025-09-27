using nSNMP.MIB;
using Xunit;

namespace nSNMP.MIB.Tests
{
    public class MibParserTests
    {
        [Fact]
        public void ParseMibFile_ParsesBasicObjectType()
        {
            var mibContent = @"
                TEST-MIB DEFINITIONS ::= BEGIN

                testObject OBJECT-TYPE
                    SYNTAX INTEGER
                    MAX-ACCESS read-only
                    STATUS current
                    DESCRIPTION ""A test object""
                    ::= { 1 3 6 1 4 1 12345 1 }

                END";

            var module = MibParser.ParseMibFile(mibContent, "TEST-MIB");

            Assert.Equal("TEST-MIB", module.Name);
            Assert.Single(module.Objects);

            var obj = module.Objects["testObject"];
            Assert.NotNull(obj);
            Assert.Equal("testObject", obj.Name);
            Assert.Equal("INTEGER", obj.Syntax);
            Assert.Equal(Access.ReadOnly, obj.Access);
            Assert.Equal(Status.Current, obj.Status);
            Assert.Equal("A test object", obj.Description);
        }

        [Fact]
        public void ParseMibFile_ParsesMultipleObjects()
        {
            var mibContent = @"
                MULTI-TEST-MIB DEFINITIONS ::= BEGIN

                object1 OBJECT-TYPE
                    SYNTAX INTEGER
                    MAX-ACCESS read-only
                    STATUS current
                    DESCRIPTION ""First object""
                    ::= { 1 1 }

                object2 OBJECT-TYPE
                    SYNTAX OCTET STRING
                    MAX-ACCESS read-write
                    STATUS deprecated
                    DESCRIPTION ""Second object""
                    ::= { 1 2 }

                END";

            var module = MibParser.ParseMibFile(mibContent, "MULTI-TEST-MIB");

            Assert.Equal(2, module.Objects.Count);

            var obj1 = module.Objects["object1"];
            Assert.Equal("INTEGER", obj1.Syntax);
            Assert.Equal(Access.ReadOnly, obj1.Access);

            var obj2 = module.Objects["object2"];
            Assert.Equal("OCTET STRING", obj2.Syntax);
            Assert.Equal(Access.ReadWrite, obj2.Access);
            Assert.Equal(Status.Deprecated, obj2.Status);
        }

        [Fact]
        public void ParseMibFile_ParsesTableWithIndex()
        {
            var mibContent = @"
                TABLE-TEST-MIB DEFINITIONS ::= BEGIN

                testTable OBJECT-TYPE
                    SYNTAX SEQUENCE OF TestEntry
                    MAX-ACCESS not-accessible
                    STATUS current
                    DESCRIPTION ""A test table""
                    ::= { 1 1 }

                testEntry OBJECT-TYPE
                    SYNTAX TestEntry
                    MAX-ACCESS not-accessible
                    STATUS current
                    DESCRIPTION ""An entry in the test table""
                    INDEX { testIndex }
                    ::= { testTable 1 }

                testIndex OBJECT-TYPE
                    SYNTAX INTEGER
                    MAX-ACCESS read-only
                    STATUS current
                    DESCRIPTION ""Table index""
                    ::= { testEntry 1 }

                END";

            var module = MibParser.ParseMibFile(mibContent, "TABLE-TEST-MIB");

            Assert.Equal(3, module.Objects.Count);

            var table = module.Objects["testTable"];
            Assert.True(table.IsTable);

            var entry = module.Objects["testEntry"];
            Assert.True(entry.IsTableEntry);
            Assert.Single(entry.Index ?? new List<string>());
            Assert.Equal("testIndex", entry.Index?[0]);

            var index = module.Objects["testIndex"];
            Assert.Equal("INTEGER", index.Syntax);
        }

        [Fact]
        public void ValidateModule_DetectsMissingFields()
        {
            var module = new MibModule("TEST");

            var invalidObj = new MibObject("invalid");
            // Don't set required fields like Syntax
            module.AddObject(invalidObj);

            var errors = MibParser.ValidateModule(module);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Missing SYNTAX"));
        }

        [Fact]
        public void ValidateModule_PassesValidObjects()
        {
            var module = new MibModule("TEST");

            var validObj = new MibObject("valid")
            {
                Syntax = "INTEGER",
                Access = Access.ReadOnly,
                Status = Status.Current
            };
            module.AddObject(validObj);

            var errors = MibParser.ValidateModule(module);

            // Should not have errors for properly defined objects
            Assert.DoesNotContain(errors, e => e.Contains("valid"));
        }
    }
}