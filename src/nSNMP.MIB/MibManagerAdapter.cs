using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;

namespace nSNMP.MIB
{
    /// <summary>
    /// Adapter that implements IMibManager interface for the existing MibManager
    /// </summary>
    public class MibManagerAdapter : IMibManager
    {
        private readonly MibManager _mibManager;
        private readonly List<string> _loadedModules;

        public MibManagerAdapter(MibManager? mibManager = null)
        {
            _mibManager = mibManager ?? MibManager.Instance;
            _loadedModules = new List<string>();
        }

        public IReadOnlyList<string> LoadedModules => _loadedModules.AsReadOnly();

        public async Task LoadMibAsync(string filePath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                var module = _mibManager.LoadMibFile(filePath);
                if (!_loadedModules.Contains(module.Name))
                {
                    _loadedModules.Add(module.Name);
                }
            }, cancellationToken);
        }

        public async Task LoadMibAsync(Stream stream, string name, CancellationToken cancellationToken = default)
        {
            // Create a temporary file from the stream
            var tempFile = Path.GetTempFileName();
            try
            {
                using var fileStream = File.Create(tempFile);
                await stream.CopyToAsync(fileStream, cancellationToken);

                await LoadMibAsync(tempFile, cancellationToken);

                if (!_loadedModules.Contains(name))
                {
                    _loadedModules.Add(name);
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        public string? ResolveOidToName(string oid)
        {
            var result = _mibManager.OidToName(oid);
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public string? ResolveNameToOid(string name)
        {
            var oid = _mibManager.NameToOid(name);
            return oid?.ToString();
        }

        public IMibNode? GetMibNode(string oid)
        {
            var mibObject = _mibManager.GetObject(oid);
            return mibObject != null ? new MibNodeAdapter(mibObject) : null;
        }

        public IEnumerable<IMibNode> GetChildNodes(string parentOid)
        {
            // This would need to be implemented based on the MibTree structure
            // For now, return empty collection
            return Enumerable.Empty<IMibNode>();
        }

        public IEnumerable<IMibNode> SearchByName(string pattern)
        {
            var results = _mibManager.SearchObjects(pattern);
            return results.Select(obj => new MibNodeAdapter(obj));
        }

        public void Clear()
        {
            _mibManager.Clear();
            _loadedModules.Clear();
        }
    }

    /// <summary>
    /// Adapter that implements IMibNode interface for the existing MibObject
    /// </summary>
    internal class MibNodeAdapter : IMibNode
    {
        private readonly MibObject _mibObject;

        public MibNodeAdapter(MibObject mibObject)
        {
            _mibObject = mibObject;
        }

        public string Oid => _mibObject.Oid?.ToString() ?? string.Empty;
        public string Name => _mibObject.Name;
        public string? Description => _mibObject.Description;
        public string? Syntax => _mibObject.Syntax;

        public MibAccess Access => _mibObject.Access switch
        {
            nSNMP.MIB.Access.NotAccessible => MibAccess.NotAccessible,
            nSNMP.MIB.Access.AccessibleForNotify => MibAccess.AccessibleForNotify,
            nSNMP.MIB.Access.ReadOnly => MibAccess.ReadOnly,
            nSNMP.MIB.Access.ReadWrite => MibAccess.ReadWrite,
            nSNMP.MIB.Access.ReadCreate => MibAccess.ReadCreate,
            _ => MibAccess.Unknown
        };

        public MibStatus Status => _mibObject.Status switch
        {
            nSNMP.MIB.Status.Current => MibStatus.Current,
            nSNMP.MIB.Status.Deprecated => MibStatus.Deprecated,
            nSNMP.MIB.Status.Obsolete => MibStatus.Obsolete,
            _ => MibStatus.Unknown
        };

        public IMibNode? Parent => null; // Would need parent tracking in MibObject

        public IReadOnlyList<IMibNode> Children => Array.Empty<IMibNode>(); // Would need children tracking

        public string Module => _mibObject.ModuleName ?? string.Empty;
    }
}