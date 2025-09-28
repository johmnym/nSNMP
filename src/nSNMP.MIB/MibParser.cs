using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.Logging;

namespace nSNMP.MIB
{
    /// <summary>
    /// Simple MIB file parser for basic SMIv2 subset
    /// </summary>
    public class MibParser
    {
        private static readonly Regex ModuleIdentityRegex = new(@"(\w+(?:-\w+)*)\s+(?:MODULE-IDENTITY|DEFINITIONS)");
        private static readonly Regex ObjectTypeRegex = new(@"(\w+)\s+OBJECT-TYPE");
        private static readonly Regex SyntaxRegex = new(@"SYNTAX\s+(.+?)(?=\s+(?:UNITS|MAX-ACCESS|STATUS|DESCRIPTION|REFERENCE|INDEX|AUGMENTS|DEFVAL|::=))");
        private static readonly Regex AccessRegex = new(@"MAX-ACCESS\s+(\w+(?:-\w+)*)");
        private static readonly Regex StatusRegex = new(@"STATUS\s+(\w+)");
        private static readonly Regex DescriptionRegex = new(@"DESCRIPTION\s+""([^""]+)""");
        private static readonly Regex OidRegex = new(@"::=\s*\{\s*([\w\s\(\)\d\.]+)\s*\}");
        private static readonly Regex IndexRegex = new(@"INDEX\s*\{\s*([^}]+)\s*\}");

        /// <summary>
        /// Parse a MIB file from text content
        /// </summary>
        public static MibModule ParseMibFile(string content, string fileName = "", nSNMP.Logging.ISnmpLogger? logger = null)
        {
            logger ??= NullSnmpLogger.Instance;
            var lines = content.Split('\n').Select(l => l.Trim()).ToArray();
            var cleanContent = PreprocessContent(content);

            // Extract module name
            var moduleMatch = ModuleIdentityRegex.Match(cleanContent);
            var moduleName = moduleMatch.Success ? moduleMatch.Groups[1].Value : Path.GetFileNameWithoutExtension(fileName);

            var module = new MibModule(moduleName);

            // Parse all OBJECT-TYPE definitions
            var objectMatches = ObjectTypeRegex.Matches(cleanContent);

            foreach (Match match in objectMatches)
            {
                try
                {
                    var objectName = match.Groups[1].Value;
                    var objectDef = ExtractObjectDefinition(cleanContent, match.Index);

                    if (objectDef != null)
                    {
                        var mibObject = ParseObjectDefinition(objectName, objectDef, logger);
                        mibObject.ModuleName = moduleName;
                        module.AddObject(mibObject);
                    }
                }
                catch (Exception ex)
                {
                    // Continue parsing other objects if one fails
                    logger.LogAgent("MibParser", $"Failed to parse object {match.Groups[1].Value}: {ex.Message}");
                }
            }

            return module;
        }

        /// <summary>
        /// Preprocess MIB content to remove comments and normalize whitespace
        /// </summary>
        private static string PreprocessContent(string content)
        {
            // Remove comments (-- to end of line)
            content = Regex.Replace(content, @"--.*?$", "", RegexOptions.Multiline);

            // Normalize whitespace but preserve some structure
            content = Regex.Replace(content, @"\r\n|\r|\n", " ");
            content = Regex.Replace(content, @"\s+", " ");

            return content.Trim();
        }

        /// <summary>
        /// Extract the complete definition of an object from the content
        /// </summary>
        private static string? ExtractObjectDefinition(string content, int startIndex)
        {
            // Find the end of the object definition (next OBJECT-TYPE or end of content)
            var nextObjectMatch = ObjectTypeRegex.Match(content, startIndex + 1);
            var endIndex = nextObjectMatch.Success ? nextObjectMatch.Index : content.Length;

            var definition = content.Substring(startIndex, endIndex - startIndex);

            // Find the OID assignment (::= { ... })
            var oidMatch = OidRegex.Match(definition);
            if (oidMatch.Success)
            {
                return definition.Substring(0, oidMatch.Index + oidMatch.Length);
            }
            else
            {
                // Try to find the OID pattern in the entire remaining content
                var fullOidMatch = OidRegex.Match(content, startIndex);
                if (fullOidMatch.Success)
                {
                    var fullDefinitionEnd = fullOidMatch.Index + fullOidMatch.Length;
                    var fullDefinition = content.Substring(startIndex, fullDefinitionEnd - startIndex);
                    return fullDefinition;
                }
                return null;
            }
        }

        /// <summary>
        /// Parse an individual object definition
        /// </summary>
        private static MibObject ParseObjectDefinition(string name, string definition, nSNMP.Logging.ISnmpLogger logger)
        {
            var obj = new MibObject(name);

            // Parse SYNTAX
            var syntaxMatch = SyntaxRegex.Match(definition);
            if (syntaxMatch.Success)
            {
                obj.Syntax = syntaxMatch.Groups[1].Value.Trim();
            }

            // Parse MAX-ACCESS
            var accessMatch = AccessRegex.Match(definition);
            if (accessMatch.Success)
            {
                if (Enum.TryParse<Access>(accessMatch.Groups[1].Value.Replace("-", ""), true, out var access))
                {
                    obj.Access = access;
                }
            }

            // Parse STATUS
            var statusMatch = StatusRegex.Match(definition);
            if (statusMatch.Success)
            {
                if (Enum.TryParse<Status>(statusMatch.Groups[1].Value, true, out var status))
                {
                    obj.Status = status;
                }
            }

            // Parse DESCRIPTION
            var descMatch = DescriptionRegex.Match(definition);
            if (descMatch.Success)
            {
                obj.Description = descMatch.Groups[1].Value;
            }

            // Parse INDEX
            var indexMatch = IndexRegex.Match(definition);
            if (indexMatch.Success)
            {
                var indexFields = indexMatch.Groups[1].Value
                    .Split(',')
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();
                obj.Index = indexFields;
            }

            // Parse OID
            var oidMatch = OidRegex.Match(definition);
            if (oidMatch.Success)
            {
                try
                {
                    var oidDef = oidMatch.Groups[1].Value.Trim();
                    var oid = ParseOidDefinition(oidDef, logger);
                    obj.Oid = oid;
                }
                catch (Exception ex)
                {
                    logger.LogAgent("MibParser", $"Failed to parse OID for {name}: {ex.Message}");
                }
            }

            return obj;
        }

        /// <summary>
        /// Parse OID definition like "iso org dod internet mgmt mib-2 system 1" or "1 3 6 1 2 1 1 1"
        /// </summary>
        private static ObjectIdentifier? ParseOidDefinition(string oidDef, nSNMP.Logging.ISnmpLogger logger)
        {
            // Handle simple numeric OIDs
            if (Regex.IsMatch(oidDef, @"^[\d\s\.]+$"))
            {
                var numbers = oidDef.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                if (numbers.Any())
                {
                    return ObjectIdentifier.Create(string.Join(".", numbers));
                }
            }

            // Handle named OIDs with symbolic references
            return ResolveSymbolicOid(oidDef, logger);
        }

        /// <summary>
        /// Resolve symbolic OID definitions using common standard mappings
        /// </summary>
        private static ObjectIdentifier? ResolveSymbolicOid(string oidDef, nSNMP.Logging.ISnmpLogger logger)
        {
            // Create mapping of common symbolic names to OID values
            var symbolicMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Standard ISO/ITU hierarchy
                { "iso", "1" },
                { "org", "1.3" },
                { "dod", "1.3.6" },
                { "internet", "1.3.6.1" },
                { "directory", "1.3.6.1.1" },
                { "mgmt", "1.3.6.1.2" },
                { "mib-2", "1.3.6.1.2.1" },
                { "experimental", "1.3.6.1.3" },
                { "private", "1.3.6.1.4" },
                { "enterprises", "1.3.6.1.4.1" },

                // Common MIB-II groups
                { "system", "1.3.6.1.2.1.1" },
                { "interfaces", "1.3.6.1.2.1.2" },
                { "at", "1.3.6.1.2.1.3" },
                { "ip", "1.3.6.1.2.1.4" },
                { "icmp", "1.3.6.1.2.1.5" },
                { "tcp", "1.3.6.1.2.1.6" },
                { "udp", "1.3.6.1.2.1.7" },
                { "egp", "1.3.6.1.2.1.8" },
                { "transmission", "1.3.6.1.2.1.10" },
                { "snmp", "1.3.6.1.2.1.11" }
            };

            try
            {
                // Parse the OID definition
                var parts = oidDef.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var resolvedParts = new List<string>();

                foreach (var part in parts)
                {
                    var cleanPart = part.Trim().Trim('{', '}', '(', ')');

                    if (symbolicMappings.ContainsKey(cleanPart))
                    {
                        // Replace symbolic name with numeric OID
                        resolvedParts.Add(symbolicMappings[cleanPart]);
                    }
                    else if (uint.TryParse(cleanPart, out _))
                    {
                        // Keep numeric values as-is
                        resolvedParts.Add(cleanPart);
                    }
                    // Skip unknown symbolic names for now
                }

                if (resolvedParts.Count > 0)
                {
                    // For relative OIDs like "{ system 1 }", concatenate the parts
                    if (resolvedParts.Count > 1)
                    {
                        var baseOid = resolvedParts[0];
                        var additionalParts = resolvedParts.Skip(1);
                        var fullOid = baseOid + "." + string.Join(".", additionalParts);
                        return ObjectIdentifier.Create(fullOid);
                    }
                    else
                    {
                        return ObjectIdentifier.Create(resolvedParts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't crash
                logger.LogAgent("MibParser", $"Failed to resolve symbolic OID '{oidDef}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Load a MIB file from disk
        /// </summary>
        public static MibModule LoadMibFile(string filePath, nSNMP.Logging.ISnmpLogger? logger = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MIB file not found: {filePath}");

            var content = File.ReadAllText(filePath);
            return ParseMibFile(content, Path.GetFileName(filePath), logger);
        }

        /// <summary>
        /// Validate a parsed MIB module
        /// </summary>
        public static List<string> ValidateModule(MibModule module)
        {
            var errors = new List<string>();

            foreach (var obj in module.Objects.Values)
            {
                // Check for missing required fields
                if (string.IsNullOrEmpty(obj.Syntax))
                    errors.Add($"Object {obj.Name}: Missing SYNTAX");

                if (obj.Access == Access.NotAccessible && obj.Status == Status.Current)
                    errors.Add($"Object {obj.Name}: Current object should have valid access");

                // Validate OID format
                if (obj.Oid != null)
                {
                    try
                    {
                        // Validate OID is well-formed
                        var oidStr = obj.Oid.ToString();
                        if (string.IsNullOrEmpty(oidStr) || !oidStr.Contains('.'))
                            errors.Add($"Object {obj.Name}: Invalid OID format");
                    }
                    catch
                    {
                        errors.Add($"Object {obj.Name}: Invalid OID");
                    }
                }
            }

            return errors;
        }
    }
}