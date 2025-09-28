using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Contract for SNMPv3 security operations
    /// </summary>
    public interface ISecurityProvider : IDisposable
    {
        /// <summary>
        /// Gets the security model identifier
        /// </summary>
        int SecurityModel { get; }

        /// <summary>
        /// Authenticates a message
        /// </summary>
        /// <param name="message">Message to authenticate</param>
        /// <param name="credentials">Security credentials</param>
        /// <returns>Authentication parameters</returns>
        byte[] Authenticate(byte[] message, ISecurityCredentials credentials);

        /// <summary>
        /// Verifies message authentication
        /// </summary>
        /// <param name="message">Message to verify</param>
        /// <param name="authParams">Authentication parameters</param>
        /// <param name="credentials">Security credentials</param>
        /// <returns>True if authentication is valid</returns>
        bool VerifyAuthentication(byte[] message, byte[] authParams, ISecurityCredentials credentials);

        /// <summary>
        /// Encrypts message data
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="credentials">Security credentials</param>
        /// <param name="engineBoots">Engine boots counter</param>
        /// <param name="engineTime">Engine time</param>
        /// <returns>Encrypted data and privacy parameters</returns>
        (byte[] EncryptedData, byte[] PrivacyParams) Encrypt(byte[] data, ISecurityCredentials credentials, int engineBoots, int engineTime);

        /// <summary>
        /// Decrypts message data
        /// </summary>
        /// <param name="encryptedData">Encrypted data</param>
        /// <param name="privacyParams">Privacy parameters</param>
        /// <param name="credentials">Security credentials</param>
        /// <param name="engineBoots">Engine boots counter</param>
        /// <param name="engineTime">Engine time</param>
        /// <returns>Decrypted data</returns>
        byte[] Decrypt(byte[] encryptedData, byte[] privacyParams, ISecurityCredentials credentials, int engineBoots, int engineTime);

        /// <summary>
        /// Discovers engine parameters from a remote agent
        /// </summary>
        /// <param name="endpoint">Agent endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Engine parameters</returns>
        Task<IEngineParameters> DiscoverEngineAsync(System.Net.IPEndPoint endpoint, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Security credentials for SNMPv3
    /// </summary>
    public interface ISecurityCredentials
    {
        /// <summary>
        /// Gets the username
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Gets the security level
        /// </summary>
        SecurityLevel SecurityLevel { get; }

        /// <summary>
        /// Gets the authentication protocol
        /// </summary>
        AuthProtocol AuthProtocol { get; }

        /// <summary>
        /// Gets the privacy protocol
        /// </summary>
        PrivProtocol PrivProtocol { get; }

        /// <summary>
        /// Gets the authentication key for the given engine ID
        /// </summary>
        /// <param name="engineId">Engine identifier</param>
        /// <returns>Authentication key</returns>
        byte[] GetAuthKey(byte[] engineId);

        /// <summary>
        /// Gets the privacy key for the given engine ID
        /// </summary>
        /// <param name="engineId">Engine identifier</param>
        /// <returns>Privacy key</returns>
        byte[] GetPrivKey(byte[] engineId);
    }

    /// <summary>
    /// Engine parameters for SNMPv3
    /// </summary>
    public interface IEngineParameters
    {
        /// <summary>
        /// Gets the engine identifier
        /// </summary>
        byte[] EngineId { get; }

        /// <summary>
        /// Gets the engine boots counter
        /// </summary>
        int EngineBoots { get; }

        /// <summary>
        /// Gets the engine time
        /// </summary>
        int EngineTime { get; }

        /// <summary>
        /// Gets the maximum message size
        /// </summary>
        int MaxMessageSize { get; }
    }

    /// <summary>
    /// Table data provider interface
    /// </summary>
    public interface ITableProvider
    {
        /// <summary>
        /// Gets all rows in the table
        /// </summary>
        /// <returns>Collection of table rows</returns>
        IEnumerable<ITableRow> GetRows();

        /// <summary>
        /// Gets a specific row by index
        /// </summary>
        /// <param name="index">Row index</param>
        /// <returns>Table row if found, null otherwise</returns>
        ITableRow? GetRow(string[] index);

        /// <summary>
        /// Gets the next row after the given index
        /// </summary>
        /// <param name="index">Current index</param>
        /// <returns>Next table row if found, null otherwise</returns>
        ITableRow? GetNextRow(string[] index);
    }

    /// <summary>
    /// Represents a row in an SNMP table
    /// </summary>
    public interface ITableRow
    {
        /// <summary>
        /// Gets the row index
        /// </summary>
        string[] Index { get; }

        /// <summary>
        /// Gets a column value by OID
        /// </summary>
        /// <param name="columnOid">Column object identifier</param>
        /// <returns>Column value if found, null otherwise</returns>
        IDataType? GetColumn(string columnOid);

        /// <summary>
        /// Gets all columns in the row
        /// </summary>
        IReadOnlyDictionary<string, IDataType> Columns { get; }
    }
}