using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public record ObjectIdentifier(byte[] Data) : PrimitiveDataType(Data), IComparable<ObjectIdentifier>
    {
        public uint[] Value
        {
            get
            {
                if (Data == null) return Array.Empty<uint>();
                var oid = new List<uint>();
                oid.Add((uint) Data[0] / 40);
                oid.Add((uint) Data[0] % 40);

                uint buffer = 0;

                for (var i = 1; i < Data.Length; i++)
                {
                    if ((Data[i] & 0x80) == 0)
                    {
                        oid.Add(Data[i] + (buffer << 7));
                        buffer = 0;
                    }
                    else
                    {
                        buffer <<= 7;
                        buffer += (uint)(Data[i] & 0x7F);
                    }
                }

                return oid.ToArray();
            }
        }

        public static uint[] ConvertToUIntArray(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            string trimStart = data.TrimStart('.');

            var parts = trimStart.Split(new[] { '.' });

            var result = new List<uint>();
            
            foreach (var s in parts)
            {
                uint temp;
                
                if (uint.TryParse(s, out temp))
                {
                    result.Add(temp);
                }
                else
                {
                    throw new ArgumentException(string.Format("Parameter {0} is out of 32 bit unsigned integer range", s), "data");
                }
            }

            return result.ToArray();
        }

        public static ObjectIdentifier Create(string oid)
        {
            uint[] array = ConvertToUIntArray(oid);

            return Create(array);
        }

        public static ObjectIdentifier Create(uint[] oid)
        {
            if (oid == null)
            {
                throw new ArgumentNullException(nameof(oid));
            }

            if (oid.Length == 0)
            {
                throw new ArgumentException("Object identifier must have at least one component", nameof(oid));
            }

            if (oid.Length == 1)
            {
                throw new ArgumentException("Object identifier must have at least two components", nameof(oid));
            }

            var temp = new List<byte>();

            var first = (byte)((40 * oid[0]) + oid[1]);

            temp.Add(first);

            for (var i = 2; i < oid.Length; i++)
            {
                temp.AddRange(ConvertToBytes(oid[i]));
            }

            return new ObjectIdentifier(temp.ToArray());
        }

        private static IEnumerable<byte> ConvertToBytes(uint subIdentifier)
        {
            var result = new List<byte> { (byte)(subIdentifier & 0x7F) };
            
            while ((subIdentifier = subIdentifier >> 7) > 0)
            {
                result.Add((byte)((subIdentifier & 0x7F) | 0x80));
            }

            result.Reverse();
            
            return result;
        }

        public override string ToString()
        {
            var oid = Value;

            if (oid == null)
            {
                throw new ArgumentNullException();
            }

            var result = new StringBuilder();
            
            foreach (uint section in oid)
            {
                result.Append(".").Append(section.ToString(CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        public override byte[] ToBytes()
        {
            var valueBytes = BEREncoder.EncodeOID(Value);
            return BEREncoder.EncodeTLV((byte)SnmpDataType.ObjectIdentifier, valueBytes);
        }

        /// <summary>
        /// Performs lexicographic comparison of OIDs (RFC 3416 section 4.1.1)
        /// </summary>
        public int CompareTo(ObjectIdentifier? other)
        {
            if (other == null) return 1;

            var thisValue = Value;
            var otherValue = other.Value;

            int minLength = Math.Min(thisValue.Length, otherValue.Length);

            for (int i = 0; i < minLength; i++)
            {
                int comparison = thisValue[i].CompareTo(otherValue[i]);
                if (comparison != 0)
                    return comparison;
            }

            // If all compared elements are equal, shorter OID comes first
            return thisValue.Length.CompareTo(otherValue.Length);
        }

        /// <summary>
        /// Returns true if this OID is a prefix of the specified OID
        /// </summary>
        public bool IsPrefixOf(ObjectIdentifier other)
        {
            if (other == null) return false;

            var thisValue = Value;
            var otherValue = other.Value;

            if (thisValue.Length > otherValue.Length) return false;

            for (int i = 0; i < thisValue.Length; i++)
            {
                if (thisValue[i] != otherValue[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if this OID starts with the specified prefix
        /// </summary>
        public bool StartsWith(ObjectIdentifier prefix)
        {
            return prefix.IsPrefixOf(this);
        }

        /// <summary>
        /// Gets the next OID in lexicographic order (for SNMP GetNext operations)
        /// </summary>
        public ObjectIdentifier GetNext()
        {
            var thisValue = Value;
            var nextValue = new uint[thisValue.Length + 1];
            Array.Copy(thisValue, nextValue, thisValue.Length);
            nextValue[thisValue.Length] = 0; // Append .0 to get next OID

            return Create(nextValue);
        }

        /// <summary>
        /// Gets the parent OID by removing the last sub-identifier
        /// </summary>
        public ObjectIdentifier? GetParent()
        {
            var thisValue = Value;
            if (thisValue.Length <= 2) return null; // Cannot go above root level

            var parentValue = new uint[thisValue.Length - 1];
            Array.Copy(thisValue, parentValue, parentValue.Length);

            return Create(parentValue);
        }

        /// <summary>
        /// Appends a sub-identifier to create a child OID
        /// </summary>
        public ObjectIdentifier Append(uint subId)
        {
            var thisValue = Value;
            var childValue = new uint[thisValue.Length + 1];
            Array.Copy(thisValue, childValue, thisValue.Length);
            childValue[thisValue.Length] = subId;

            return Create(childValue);
        }

        /// <summary>
        /// Optimized string parsing with caching for common OIDs
        /// </summary>
        private static readonly Dictionary<string, uint[]> _oidCache = new();

        public static ObjectIdentifier CreateCached(string oid)
        {
            if (_oidCache.TryGetValue(oid, out uint[]? cached))
            {
                return Create(cached);
            }

            uint[] array = ConvertToUIntArray(oid);

            // Cache frequently used OIDs (limit cache size)
            if (_oidCache.Count < 1000)
            {
                _oidCache[oid] = array;
            }

            return Create(array);
        }

        /// <summary>
        /// Equality comparison optimized for OID operations
        /// </summary>
        public virtual bool Equals(ObjectIdentifier? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            var thisValue = Value;
            var otherValue = other.Value;

            if (thisValue.Length != otherValue.Length) return false;

            for (int i = 0; i < thisValue.Length; i++)
            {
                if (thisValue[i] != otherValue[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var value = Value;
            var hash = new HashCode();

            foreach (var subId in value)
            {
                hash.Add(subId);
            }

            return hash.ToHashCode();
        }

        // Comparison operators for convenience
        public static bool operator <(ObjectIdentifier left, ObjectIdentifier right)
            => left.CompareTo(right) < 0;

        public static bool operator >(ObjectIdentifier left, ObjectIdentifier right)
            => left.CompareTo(right) > 0;

        public static bool operator <=(ObjectIdentifier left, ObjectIdentifier right)
            => left.CompareTo(right) <= 0;

        public static bool operator >=(ObjectIdentifier left, ObjectIdentifier right)
            => left.CompareTo(right) >= 0;
    }
}
