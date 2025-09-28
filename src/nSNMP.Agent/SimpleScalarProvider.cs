using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Agent
{
    /// <summary>
    /// Simple implementation of IScalarProvider for single values
    /// </summary>
    public class SimpleScalarProvider : IScalarProvider
    {
        private readonly ObjectIdentifier _oid;
        private IDataType? _value;
        private readonly bool _readOnly;

        public SimpleScalarProvider(ObjectIdentifier oid, IDataType? initialValue = null, bool readOnly = true)
        {
            _oid = oid ?? throw new ArgumentNullException(nameof(oid));
            _value = initialValue;
            _readOnly = readOnly;
        }

        public IDataType? GetValue(ObjectIdentifier oid)
        {
            return CanHandle(oid) ? _value : null;
        }

        public bool SetValue(ObjectIdentifier oid, IDataType value)
        {
            if (_readOnly || !CanHandle(oid))
                return false;

            _value = value;
            return true;
        }

        public bool CanHandle(ObjectIdentifier oid)
        {
            return _oid.Equals(oid);
        }

        public ObjectIdentifier? GetNextOid(ObjectIdentifier oid)
        {
            // For exact match, no next OID
            if (_oid.Equals(oid))
                return null;

            // If the requested OID is before ours, return our OID
            if (_oid.CompareTo(oid) > 0)
                return _oid;

            // Otherwise, no next OID from this provider
            return null;
        }
    }
}