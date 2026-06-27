using System.Text.RegularExpressions;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class TypeReplaceHelper
	{
		public static string ApplyReplacement(string content, SRReplacementPattern pattern, out bool wasModified)
		{
			wasModified = false;

			var oldClassName = pattern.OldClassName;
			var oldNamespace = pattern.OldNamespace;
			var oldAssembly = pattern.OldAssembly;
			var newClassName = pattern.NewClassName;
			var newNamespace = pattern.NewNamespace;
			var newAssembly = pattern.NewAssembly;

			var referencesSection = Regex.Match(content, @"references:\s*\n\s*version:\s*2\s*\n\s*RefIds:\s*(?:\n|.)*?(?=\n\s*\n|$)");
			if (referencesSection.Success)
			{
				string newReferencesSection = Regex.Replace(
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
			if (Regex.IsMatch(content, typePattern))
			{
				var resultAssembly = string.IsNullOrEmpty(newAssembly) ? "Assembly-CSharp" : newAssembly;
				var replacement = $"type: {{ class: {newClassName}, ns: {newNamespace}, asm: {resultAssembly} }}";
				content = Regex.Replace(content, typePattern, replacement);
				wasModified = true;
			}

			var managedReferencesPattern = @"managedReferences\[\d+\]:\s*(\w+)\s+([\w.]+(?:\.[\w.]+)*)";
			var managedReferencesMatches = Regex.Matches(content, managedReferencesPattern);

			foreach (Match match in managedReferencesMatches)
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

			return content;
		}
	}
}
