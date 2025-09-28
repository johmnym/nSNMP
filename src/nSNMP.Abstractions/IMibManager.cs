using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Contract for MIB management operations
    /// </summary>
    public interface IMibManager
    {
        /// <summary>
        /// Loads a MIB from a file path
        /// </summary>
        /// <param name="filePath">Path to the MIB file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LoadMibAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a MIB from a stream
        /// </summary>
        /// <param name="stream">Stream containing MIB data</param>
        /// <param name="name">Name identifier for the MIB</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LoadMibAsync(Stream stream, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves an OID to its symbolic name
        /// </summary>
        /// <param name="oid">Object identifier</param>
        /// <returns>Symbolic name if found, null otherwise</returns>
        string? ResolveOidToName(string oid);

        /// <summary>
        /// Resolves a symbolic name to its OID
        /// </summary>
        /// <param name="name">Symbolic name</param>
        /// <returns>Object identifier if found, null otherwise</returns>
        string? ResolveNameToOid(string name);

        /// <summary>
        /// Gets MIB node information for an OID
        /// </summary>
        /// <param name="oid">Object identifier</param>
        /// <returns>MIB node information if found, null otherwise</returns>
        IMibNode? GetMibNode(string oid);

        /// <summary>
        /// Gets all child nodes of a given OID
        /// </summary>
        /// <param name="parentOid">Parent object identifier</param>
        /// <returns>Collection of child nodes</returns>
        IEnumerable<IMibNode> GetChildNodes(string parentOid);

        /// <summary>
        /// Searches for MIB nodes by name pattern
        /// </summary>
        /// <param name="pattern">Search pattern (supports wildcards)</param>
        /// <returns>Matching MIB nodes</returns>
        IEnumerable<IMibNode> SearchByName(string pattern);

        /// <summary>
        /// Gets all loaded MIB modules
        /// </summary>
        IReadOnlyList<string> LoadedModules { get; }

        /// <summary>
        /// Clears all loaded MIBs
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Represents a node in the MIB tree
    /// </summary>
    public interface IMibNode
    {
        /// <summary>
        /// Gets the object identifier
        /// </summary>
        string Oid { get; }

        /// <summary>
        /// Gets the symbolic name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the syntax type
        /// </summary>
        string? Syntax { get; }

        /// <summary>
        /// Gets the access level
        /// </summary>
        MibAccess Access { get; }

        /// <summary>
        /// Gets the status
        /// </summary>
        MibStatus Status { get; }

        /// <summary>
        /// Gets the parent node
        /// </summary>
        IMibNode? Parent { get; }

        /// <summary>
        /// Gets the child nodes
        /// </summary>
        IReadOnlyList<IMibNode> Children { get; }

        /// <summary>
        /// Gets the MIB module this node belongs to
        /// </summary>
        string Module { get; }
    }

    /// <summary>
    /// MIB access levels
    /// </summary>
    public enum MibAccess
    {
        Unknown,
        NotAccessible,
        AccessibleForNotify,
        ReadOnly,
        ReadWrite,
        ReadCreate
    }

    /// <summary>
    /// MIB status values
    /// </summary>
    public enum MibStatus
    {
        Unknown,
        Current,
        Deprecated,
        Obsolete
    }
}