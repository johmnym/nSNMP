using System;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Base interface for all SNMP data types
    /// </summary>
    public interface IDataType
    {
        /// <summary>
        /// Gets the value as an object
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Gets the BER/DER tag for this data type
        /// </summary>
        byte Tag { get; }

        /// <summary>
        /// Converts the data type to its BER/DER byte representation
        /// </summary>
        /// <returns>Byte array representation</returns>
        byte[] ToBytes();

        /// <summary>
        /// Gets the string representation of the value
        /// </summary>
        /// <returns>String representation</returns>
        string ToString();

        /// <summary>
        /// Gets whether this data type represents a null value
        /// </summary>
        bool IsNull { get; }
    }

    /// <summary>
    /// Generic interface for strongly-typed data types
    /// </summary>
    /// <typeparam name="T">The underlying value type</typeparam>
    public interface IDataType<T> : IDataType
    {
        /// <summary>
        /// Gets the strongly-typed value
        /// </summary>
        new T Value { get; }
    }

    /// <summary>
    /// Interface for variable bindings
    /// </summary>
    public interface IVarBind
    {
        /// <summary>
        /// Gets the object identifier
        /// </summary>
        string Oid { get; }

        /// <summary>
        /// Gets the data value
        /// </summary>
        IDataType Data { get; }

        /// <summary>
        /// Creates a new variable binding with the specified OID and data
        /// </summary>
        /// <param name="oid">Object identifier</param>
        /// <param name="data">Data value</param>
        /// <returns>New variable binding</returns>
        static IVarBind Create(string oid, IDataType data) => throw new NotImplementedException();
    }
}