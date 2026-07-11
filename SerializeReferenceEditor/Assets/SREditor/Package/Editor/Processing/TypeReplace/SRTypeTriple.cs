using System;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public readonly struct SRTypeTriple : IEquatable<SRTypeTriple>
	{
		public readonly string ClassName;
		public readonly string Namespace;
		public readonly string Assembly;

		public SRTypeTriple(string className, string ns, string assembly)
		{
			ClassName = className ?? string.Empty;
			Namespace = ns ?? string.Empty;
			Assembly = assembly ?? string.Empty;
		}

		public bool Equals(SRTypeTriple other)
		{
			return ClassName == other.ClassName
				&& Namespace == other.Namespace
				&& Assembly == other.Assembly;
		}

		public override bool Equals(object obj)
		{
			return obj is SRTypeTriple other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ClassName, Namespace, Assembly);
		}
	}
}
