using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using nSNMP.SMI.DataTypes.V1.Primitive;

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
        public static MibModule ParseMibFile(string content, string fileName = "")
        {
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
                        var mibObject = ParseObjectDefinition(objectName, objectDef);
                        mibObject.ModuleName = moduleName;
                        module.AddObject(mibObject);
                    }
                }
                catch (Exception ex)
                {
                    // Continue parsing other objects if one fails
                    Console.WriteLine($"Warning: Failed to parse object {match.Groups[1].Value}: {ex.Message}");
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
        private static MibObject ParseObjectDefinition(string name, string definition)
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
                    var oid = ParseOidDefinition(oidDef);
                    obj.Oid = oid;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse OID for {name}: {ex.Message}");
                }
            }

            return obj;
        }

        /// <summary>
        /// Parse OID definition like "iso org dod internet mgmt mib-2 system 1" or "1 3 6 1 2 1 1 1"
        /// </summary>
        private static ObjectIdentifier? ParseOidDefinition(string oidDef)
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

            // Handle named OIDs - this would need a more sophisticated parser
            // For now, return null for complex named OIDs
            return null;
        }

        /// <summary>
        /// Load a MIB file from disk
        /// </summary>
        public static MibModule LoadMibFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MIB file not found: {filePath}");

            var content = File.ReadAllText(filePath);
            return ParseMibFile(content, Path.GetFileName(filePath));
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