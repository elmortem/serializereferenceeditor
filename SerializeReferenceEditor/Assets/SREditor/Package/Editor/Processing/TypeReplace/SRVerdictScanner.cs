using System;
using System.Collections.Generic;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class SRVerdictScanner
	{
		public static SRFileVerdict Analyze(string content)
		{
			bool hasManagedReferences = false;
			bool hasDuplicateRids = false;
			bool hasUnparsableTypes = false;
			var triples = new HashSet<SRTypeTriple>();
			var usageCounts = new Dictionary<long, int>();
			bool inReferences = false;

			int pos = 0;
			int length = content.Length;

			while (pos < length)
			{
				int lineEnd = content.IndexOf('\n', pos);
				if (lineEnd < 0)
				{
					lineEnd = length;
				}

				int trimmedEnd = lineEnd;
				if (trimmedEnd > pos && content[trimmedEnd - 1] == '\r')
				{
					trimmedEnd--;
				}

				var line = content.AsSpan(pos, trimmedEnd - pos);

				if (line.StartsWith("--- ".AsSpan()))
				{
					inReferences = false;
					usageCounts.Clear();
				}
				else if (line.SequenceEqual("  references:".AsSpan()))
				{
					inReferences = true;
					hasManagedReferences = true;
				}
				else
				{
					AnalyzeLine(line, inReferences, usageCounts, triples,
						ref hasManagedReferences, ref hasDuplicateRids, ref hasUnparsableTypes);
				}

				pos = lineEnd + 1;
			}

			return new SRFileVerdict(
				hasManagedReferences,
				hasDuplicateRids,
				hasUnparsableTypes,
				triples.Count > 0 ? new List<SRTypeTriple>(triples) : null);
		}

		private static void AnalyzeLine(System.ReadOnlySpan<char> line, bool inReferences,
			Dictionary<long, int> usageCounts, HashSet<SRTypeTriple> triples,
			ref bool hasManagedReferences, ref bool hasDuplicateRids, ref bool hasUnparsableTypes)
		{
			int ridIndex = line.IndexOf("rid: ".AsSpan());
			if (ridIndex > 0 && (line[ridIndex - 1] == ' ' || line[ridIndex - 1] == '{'))
			{
				hasManagedReferences = true;

				bool isDefinition = inReferences && line.StartsWith("    - rid: ".AsSpan());
				if (!isDefinition)
				{
					var numberSpan = line.Slice(ridIndex + 5).Trim();
					int end = 0;
					while (end < numberSpan.Length && (char.IsDigit(numberSpan[end]) || numberSpan[end] == '-'))
					{
						end++;
					}

					numberSpan = numberSpan.Slice(0, end);
					if (long.TryParse(numberSpan, out long rid) && rid >= 0)
					{
						usageCounts.TryGetValue(rid, out int count);
						count++;
						usageCounts[rid] = count;
						if (count >= 2)
						{
							hasDuplicateRids = true;
						}
					}
				}

				return;
			}

			if (inReferences)
			{
				int typeIndex = line.IndexOf("type: {".AsSpan());
				if (typeIndex >= 0)
				{
					if (TryParseTypeTriple(line.Slice(typeIndex + 7), out var triple))
					{
						triples.Add(triple);
					}
					else
					{
						hasUnparsableTypes = true;
					}

					return;
				}
			}

			int mrIndex = line.IndexOf("managedReferences[".AsSpan());
			if (mrIndex >= 0)
			{
				hasManagedReferences = true;
				if (TryParseManagedReferenceLine(line.Slice(mrIndex), out var triple))
				{
					triples.Add(triple);
				}
				else
				{
					hasUnparsableTypes = true;
				}
			}
		}

		private static bool TryParseTypeTriple(System.ReadOnlySpan<char> span, out SRTypeTriple triple)
		{
			triple = default;

			int closeIndex = span.IndexOf('}');
			if (closeIndex < 0)
			{
				return false;
			}

			span = span.Slice(0, closeIndex);

			if (!TryTakeValue(ref span, "class:", out var className))
			{
				return false;
			}

			if (!TryTakeValue(ref span, "ns:", out var ns))
			{
				return false;
			}

			if (!TryTakeValue(ref span, "asm:", out var asm))
			{
				return false;
			}

			triple = new SRTypeTriple(className, ns, asm);
			return true;
		}

		private static bool TryTakeValue(ref System.ReadOnlySpan<char> span, string key, out string value)
		{
			value = null;

			int keyIndex = span.IndexOf(key.AsSpan());
			if (keyIndex < 0)
			{
				return false;
			}

			var rest = span.Slice(keyIndex + key.Length);
			int commaIndex = rest.IndexOf(',');
			var raw = commaIndex >= 0 ? rest.Slice(0, commaIndex) : rest;
			value = raw.Trim().ToString();
			span = commaIndex >= 0 ? rest.Slice(commaIndex + 1) : System.ReadOnlySpan<char>.Empty;
			return true;
		}

		private static bool TryParseManagedReferenceLine(System.ReadOnlySpan<char> span, out SRTypeTriple triple)
		{
			triple = default;

			int colonIndex = span.IndexOf(':');
			if (colonIndex < 0)
			{
				return false;
			}

			var rest = span.Slice(colonIndex + 1).Trim();
			int spaceIndex = rest.IndexOf(' ');
			if (spaceIndex <= 0)
			{
				return false;
			}

			var assembly = rest.Slice(0, spaceIndex).ToString();
			var fullType = rest.Slice(spaceIndex + 1).Trim().ToString();
			if (fullType.Length == 0)
			{
				return false;
			}

			int lastDot = fullType.LastIndexOf('.');
			var className = lastDot >= 0 ? fullType.Substring(lastDot + 1) : fullType;
			var ns = lastDot >= 0 ? fullType.Substring(0, lastDot) : string.Empty;

			triple = new SRTypeTriple(className, ns, assembly);
			return true;
		}
	}
}
