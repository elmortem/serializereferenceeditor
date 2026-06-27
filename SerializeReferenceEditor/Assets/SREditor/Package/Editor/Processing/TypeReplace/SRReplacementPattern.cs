namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public readonly struct SRReplacementPattern
	{
		public readonly string OldClassName;
		public readonly string OldNamespace;
		public readonly string OldAssembly;
		public readonly string NewClassName;
		public readonly string NewNamespace;
		public readonly string NewAssembly;

		public SRReplacementPattern(string oldClassName, string oldNamespace, string oldAssembly, string newClassName, string newNamespace, string newAssembly)
		{
			OldClassName = oldClassName;
			OldNamespace = oldNamespace;
			OldAssembly = oldAssembly;
			NewClassName = newClassName;
			NewNamespace = newNamespace;
			NewAssembly = newAssembly;
		}

		public static SRReplacementPattern Parse(string oldTypePattern, string newTypePattern)
		{
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

			return new SRReplacementPattern(oldClassName, oldNamespace, oldAssembly, newClassName, newNamespace, newAssembly);
		}
	}
}
