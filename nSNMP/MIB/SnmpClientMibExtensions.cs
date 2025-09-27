using nSNMP.Manager;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.MIB
{
    /// <summary>
    /// Extension methods to add MIB support to SNMP clients
    /// </summary>
    public static class SnmpClientMibExtensions
    {
        /// <summary>
        /// Get a value using MIB object name instead of OID
        /// </summary>
        public static async Task<VarBind[]> GetAsync(this SnmpClient client, params string[] objectNames)
        {
            var oids = new List<ObjectIdentifier>();

            foreach (var name in objectNames)
            {
                var oid = MibManager.Instance.NameToOid(name);
                if (oid == null)
                {
                    // Try to parse as OID if name resolution fails
                    try
                    {
                        oid = ObjectIdentifier.Create(name);
                    }
                    catch
                    {
                        throw new ArgumentException($"Cannot resolve object name or OID: {name}");
                    }
                }
                oids.Add(oid);
            }

            return await client.GetAsync(oids.ToArray());
        }

        /// <summary>
        /// Set values using MIB object names
        /// </summary>
        public static async Task<VarBind[]> SetAsync(this SnmpClient client, params (string name, object value)[] nameValuePairs)
        {
            var varBinds = new List<VarBind>();

            foreach (var (name, value) in nameValuePairs)
            {
                var oid = MibManager.Instance.NameToOid(name);
                if (oid == null)
                {
                    // Try to parse as OID if name resolution fails
                    try
                    {
                        oid = ObjectIdentifier.Create(name);
                    }
                    catch
                    {
                        throw new ArgumentException($"Cannot resolve object name or OID: {name}");
                    }
                }

                varBinds.Add(new VarBind(oid, ConvertValue(value)));
            }

            return await client.SetAsync(varBinds.ToArray());
        }

        /// <summary>
        /// Walk a subtree starting from a named object
        /// </summary>
        public static async IAsyncEnumerable<VarBind> WalkAsync(this SnmpClient client, string objectName)
        {
            var oid = MibManager.Instance.NameToOid(objectName);
            if (oid == null)
            {
                // Try to parse as OID if name resolution fails
                try
                {
                    oid = ObjectIdentifier.Create(objectName);
                }
                catch
                {
                    throw new ArgumentException($"Cannot resolve object name or OID: {objectName}");
                }
            }

            await foreach (var varBind in client.WalkAsync(oid))
            {
                yield return varBind;
            }
        }

        /// <summary>
        /// Get an object with human-readable information
        /// </summary>
        public static async Task<MibVarBind> GetMibObjectAsync(this SnmpClient client, string objectName)
        {
            var result = await client.GetAsync(objectName);
            var varBind = result.FirstOrDefault();

            if (varBind == null)
                throw new InvalidOperationException($"No result for object: {objectName}");

            return new MibVarBind(varBind);
        }

        /// <summary>
        /// Get multiple objects with MIB information
        /// </summary>
        public static async Task<MibVarBind[]> GetMibObjectsAsync(this SnmpClient client, params string[] objectNames)
        {
            var results = await client.GetAsync(objectNames);
            return results.Select(vb => new MibVarBind(vb)).ToArray();
        }

        /// <summary>
        /// Walk with MIB information
        /// </summary>
        public static async IAsyncEnumerable<MibVarBind> WalkMibAsync(this SnmpClient client, string objectName)
        {
            await foreach (var varBind in client.WalkAsync(objectName))
            {
                yield return new MibVarBind(varBind);
            }
        }

        /// <summary>
        /// Convert a .NET value to appropriate SNMP data type
        /// </summary>
        private static nSNMP.SMI.DataTypes.IDataType ConvertValue(object value)
        {
            return value switch
            {
                int i => nSNMP.SMI.DataTypes.V1.Primitive.Integer.Create(i),
                uint ui => nSNMP.SMI.DataTypes.V1.Primitive.Counter32.Create(ui),
                long l => nSNMP.SMI.DataTypes.V1.Primitive.Counter64.Create((ulong)l),
                string s => nSNMP.SMI.DataTypes.V1.Primitive.OctetString.Create(s),
                ObjectIdentifier oid => oid,
                nSNMP.SMI.DataTypes.IDataType dataType => dataType,
                _ => nSNMP.SMI.DataTypes.V1.Primitive.OctetString.Create(value.ToString() ?? "")
            };
        }
    }

    /// <summary>
    /// VarBind enhanced with MIB information
    /// </summary>
    public class MibVarBind
    {
        /// <summary>
        /// Original VarBind
        /// </summary>
        public VarBind VarBind { get; }

        /// <summary>
        /// MIB object definition (if available)
        /// </summary>
        public MibObject? MibObject { get; }

        /// <summary>
        /// Human-readable object name
        /// </summary>
        public string ObjectName { get; }

        /// <summary>
        /// Object description from MIB
        /// </summary>
        public string? Description => MibObject?.Description;

        /// <summary>
        /// Object syntax from MIB
        /// </summary>
        public string? Syntax => MibObject?.Syntax;

        /// <summary>
        /// Object access level from MIB
        /// </summary>
        public Access? Access => MibObject?.Access;

        /// <summary>
        /// Object status from MIB
        /// </summary>
        public Status? Status => MibObject?.Status;

        public MibVarBind(VarBind varBind)
        {
            VarBind = varBind ?? throw new ArgumentNullException(nameof(varBind));
            MibObject = MibManager.Instance.GetObject(varBind.Oid);
            ObjectName = MibManager.Instance.OidToName(varBind.Oid);
        }

        /// <summary>
        /// Get formatted string representation with MIB information
        /// </summary>
        public string ToDetailedString()
        {
            var details = new List<string>
            {
                $"Object: {ObjectName}",
                $"OID: {VarBind.Oid}",
                $"Value: {VarBind.Value}"
            };

            if (MibObject != null)
            {
                if (!string.IsNullOrEmpty(Syntax))
                    details.Add($"Syntax: {Syntax}");

                if (Access.HasValue)
                    details.Add($"Access: {Access}");

                if (Status.HasValue)
                    details.Add($"Status: {Status}");

                if (!string.IsNullOrEmpty(Description))
                    details.Add($"Description: {Description}");
            }

            return string.Join(Environment.NewLine, details);
        }

        public override string ToString()
        {
            return $"{ObjectName} = {VarBind.Value}";
        }
    }
}