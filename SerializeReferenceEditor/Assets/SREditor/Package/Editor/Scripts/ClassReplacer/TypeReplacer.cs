using System.IO;
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

			string newClassName;
			string newNamespace = string.Empty;
			string newAssembly = string.Empty;
			
			if (newTypePattern.Contains(","))
			{
				var parts = newTypePattern.Split(new[] { ',' }, 2);
				newAssembly = parts[0].Trim();
				var fullType = parts[1].Trim();
				var typeParts = fullType.Split('.');
				newClassName = typeParts[^1];
				if (typeParts.Length > 1)
				{
					newNamespace = string.Join(".", typeParts, 0, typeParts.Length - 1);
				}
			}
			else
			{
				var typeParts = newTypePattern.Split('.');
				newClassName = typeParts[^1];
				if (typeParts.Length > 1)
				{
					newNamespace = string.Join(".", typeParts, 0, typeParts.Length - 1);
				}
			}

			string oldClassName;
			string oldNamespace;
			string oldAssembly;
			
			if (oldTypePattern.Contains(","))
			{
				var parts = oldTypePattern.Split(new[] { ',' }, 2);
				oldAssembly = parts[0].Trim();
				var fullType = parts[1].Trim();
				var typeParts = fullType.Split('.');
				oldClassName = typeParts[^1];
				if (typeParts.Length > 1)
				{
					oldNamespace = string.Join(".", typeParts, 0, typeParts.Length - 1);
				}
				else
				{
					oldNamespace = newNamespace;
				}
			}
			else
			{
				var typeParts = oldTypePattern.Split('.');
				oldClassName = typeParts[^1];
				if (typeParts.Length > 1)
				{
					oldNamespace = string.Join(".", typeParts, 0, typeParts.Length - 1);
				}
				else
				{
					oldNamespace = newNamespace;
				}
				oldAssembly = newAssembly;
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
						var asm = m.Groups[3].Success ? m.Groups[3].Value : string.Empty;
						
						if (className != oldClassName)
							return m.Value;
							
						if (oldNamespace != newNamespace && ns != oldNamespace)
							return m.Value;
						
						if (oldAssembly != newAssembly && asm != oldAssembly)
							return m.Value;

						var resultAssembly = string.IsNullOrEmpty(newAssembly) ? "Assembly-CSharp" : newAssembly;
						return $"type: {{ class: {newClassName}, ns: {newNamespace}, asm: {resultAssembly} }}";
					}
				);

				if (referencesSection.Value != newReferencesSection)
				{
					content = content.Replace(referencesSection.Value, newReferencesSection);
					wasModified = true;
				}
			}

			var typePattern = $@"type:\s*{{\s*class:\s*{oldClassName},\s*ns:\s*{oldNamespace}(?:,\s*asm:\s*{oldAssembly})?}}";
			if (System.Text.RegularExpressions.Regex.IsMatch(content, typePattern))
			{
				var resultAssembly = string.IsNullOrEmpty(newAssembly) ? "Assembly-CSharp" : newAssembly;
				var replacement = $"type: {{ class: {newClassName}, ns: {newNamespace}, asm: {resultAssembly} }}";
				content = System.Text.RegularExpressions.Regex.Replace(content, typePattern, replacement);
				wasModified = true;
			}

			var managedReferencesPattern = @"managedReferences\[\d+\]:\s*(\w+)\s+([\w.]+(?:\.[\w.]+)*)";
			var managedReferencesMatches = System.Text.RegularExpressions.Regex.Matches(content, managedReferencesPattern);
			
			foreach (System.Text.RegularExpressions.Match match in managedReferencesMatches)
			{
				var assembly = match.Groups[1].Value;
				var fullType = match.Groups[2].Value;
				var typeParts = fullType.Split('.');
				var className = typeParts[^1];
				var ns = typeParts.Length > 1 ? string.Join(".", typeParts, 0, typeParts.Length - 1) : string.Empty;

				if (className != oldClassName)
					continue;

				if (oldNamespace != newNamespace && ns != oldNamespace)
					continue;

				if (oldAssembly != newAssembly && assembly != oldAssembly)
					continue;

				var resultAssembly = string.IsNullOrEmpty(newAssembly) ? assembly : newAssembly;
				var newFullType = string.IsNullOrEmpty(newNamespace) ? newClassName : $"{newNamespace}.{newClassName}";
				var newReference = $"managedReferences[{match.Groups[0].Value.Split('[')[1].Split(']')[0]}]: {resultAssembly} {newFullType}";
				content = content.Replace(match.Value, newReference);
				wasModified = true;
			}

			if (wasModified)
			{
				File.WriteAllText(path, content);
			}

			return wasModified;
		}
	}
}
