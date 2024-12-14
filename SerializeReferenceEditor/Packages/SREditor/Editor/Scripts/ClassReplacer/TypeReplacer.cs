using System.IO;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
    public static class TypeReplacer
    {
        public static bool ReplaceTypeInFile(string path, string oldTypePattern, string newTypePattern)
        {
            string content = File.ReadAllText(path);
            bool wasModified = false;

            var referencesSection = System.Text.RegularExpressions.Regex.Match(content, @"references:\s*\n\s*version:\s*2\s*\n\s*RefIds:\s*(?:\n|.)*?(?=\n\s*\n|$)");
            if (referencesSection.Success)
            {
                string oldContent = content;
                string newReferencesSection = System.Text.RegularExpressions.Regex.Replace(
                    referencesSection.Value,
                    @"type:\s*{\s*class:\s*(\w+),\s*ns:\s*(\w+)(?:,\s*asm:\s*(\w+))?}",
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
                                if ($"{oldAsm}, {fullTypeName}" != oldTypePattern)
                                {
                                    return m.Value;
                                }
                            }
                            else
                            {
                                if (fullTypeName != oldTypePattern)
                                {
                                    return m.Value;
                                }
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
                        var newClassName = newTypeOnly.Substring(lastDot + 1);
                        var newNs = newTypeOnly.Substring(0, lastDot);
                        
                        var asmString = string.IsNullOrEmpty(newAssembly) ? "Assembly-CSharp" : newAssembly;
                        return $"type: {{ class: {newClassName}, ns: {newNs}, asm: {asmString} }}";
                    }
                );

                if (referencesSection.Value != newReferencesSection)
                {
                    content = content.Replace(referencesSection.Value, newReferencesSection);
                    wasModified = true;
                }
            }

            var managedReferencesPattern = @"managedReferences\[\d+\]:\s*(\w+)\s+(\w+(?:\.\w+)*)";
            var managedReferencesMatches = System.Text.RegularExpressions.Regex.Matches(content, managedReferencesPattern);
            if (managedReferencesMatches.Count > 0)
            {
                foreach (System.Text.RegularExpressions.Match match in managedReferencesMatches)
                {
                    var assembly = match.Groups[1].Value;
                    var type = match.Groups[2].Value;
                    var fullType = $"{assembly}, {type}";

                    if (fullType == oldTypePattern || type == oldTypePattern)
                    {
                        var newType = newTypePattern;
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
            }

            if (wasModified)
            {
                File.WriteAllText(path, content);
            }

            return wasModified;
        }
    }
}
