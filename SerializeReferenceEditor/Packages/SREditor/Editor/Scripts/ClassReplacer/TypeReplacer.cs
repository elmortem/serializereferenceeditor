using System.IO;
using System.Linq;
using UnityEditor;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
    public static class TypeReplacer
    {
        public static bool ReplaceTypeInFile(string path, string oldTypePattern, string newTypePattern)
        {
            if (AssetDatabase.IsValidFolder(path))
                return false;
            
            string content = File.ReadAllText(path);
            bool wasModified = false;

            string oldClassName;
            string newClassName;
            if (oldTypePattern.Contains(","))
            {
                var parts = oldTypePattern.Split(new[] { ',' }, 2);
                oldClassName = parts[1].Trim().Split('.').Last();
            }
            else
            {
                oldClassName = oldTypePattern.Split('.').Last();
            }
            
            if (newTypePattern.Contains(","))
            {
                var parts = newTypePattern.Split(new[] { ',' }, 2);
                newClassName = parts[1].Trim().Split('.').Last();
            }
            else
            {
                newClassName = newTypePattern.Split('.').Last();
            }

            var referencesSection = System.Text.RegularExpressions.Regex.Match(content, @"references:\s*\n\s*version:\s*2\s*\n\s*RefIds:\s*(?:\n|.)*?(?=\n\s*\n|$)");
            if (referencesSection.Success)
            {
                string newReferencesSection = System.Text.RegularExpressions.Regex.Replace(
                    referencesSection.Value,
                    @"type:\s*{\s*class:\s*(\w+),\s*ns:\s*([\w.]+)(?:,\s*asm:\s*(\w+))?}",
                    m => 
                    {
                        var className = m.Groups[1].Value;
                        var ns = m.Groups[2].Value;
                        var fullTypeName = $"{ns}.{className}";
                        
                        if (m.Groups[3].Success)
                        {
                            var oldAsm = m.Groups[3].Value;
                            if (oldTypePattern.Contains(","))
                            {
                                var compareValue = $"{oldAsm}, {fullTypeName}";
                                if (compareValue != oldTypePattern)
                                {
                                    return m.Value;
                                }
                            }
                            else if (fullTypeName != oldTypePattern)
                            {
                                return m.Value;
                            }
                        }
                        else if (fullTypeName != oldTypePattern)
                        {
                            return m.Value;
                        }

                        string newAssembly = null;
                        string newTypeOnly = newTypePattern;
                        
                        if (newTypePattern.Contains(","))
                        {
                            var parts = newTypePattern.Split(new[] { ',' }, 2);
                            newAssembly = parts[0].Trim();
                            newTypeOnly = parts[1].Trim();
                        }
                        
                        var lastDot = newTypeOnly.LastIndexOf('.');
                        if (lastDot == -1) return m.Value;
                        var newClassNameFromType = newTypeOnly.Substring(lastDot + 1);
                        var newNs = newTypeOnly.Substring(0, lastDot);
                        
                        var asmString = string.IsNullOrEmpty(newAssembly) ? "Assembly-CSharp" : newAssembly;
                        return $"type: {{ class: {newClassNameFromType}, ns: {newNs}, asm: {asmString} }}";
                    }
                );

                if (referencesSection.Value != newReferencesSection)
                {
                    content = content.Replace(referencesSection.Value, newReferencesSection);
                    wasModified = true;
                }
            }

            var classPattern = $@"type:\s*{{\s*class:\s*{oldClassName}\s*,";
            if (System.Text.RegularExpressions.Regex.IsMatch(content, classPattern))
            {
                content = System.Text.RegularExpressions.Regex.Replace(content, classPattern, $"type: {{ class: {newClassName},");
                wasModified = true;
            }

            var managedReferencesPattern = @"managedReferences\[\d+\]:\s*(\w+)\s+(\w+(?:\.\w+)*)";
            var managedReferencesMatches = System.Text.RegularExpressions.Regex.Matches(content, managedReferencesPattern);
            
            foreach (System.Text.RegularExpressions.Match match in managedReferencesMatches)
            {
                var assembly = match.Groups[1].Value;
                var type = match.Groups[2].Value;
                var fullType = $"{assembly}, {type}";

                if (fullType == oldTypePattern || type == oldTypePattern)
                {
                    if (newTypePattern.Contains(","))
                    {
                        var parts = newTypePattern.Split(new[] { ',' }, 2);
                        assembly = parts[0].Trim();
                        type = parts[1].Trim();
                    }
                    else
                    {
                        type = newTypePattern;
                    }

                    var newReference = $"managedReferences[{match.Groups[0].Value.Split('[')[1].Split(']')[0]}]: {assembly} {type}";
                    content = content.Replace(match.Value, newReference);
                    wasModified = true;
                }
            }

            if (wasModified)
            {
                File.WriteAllText(path, content);
            }

            return wasModified;
        }
    }
}
