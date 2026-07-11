using System.Collections.Generic;
using System.IO;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class SRFileScanner
	{
		public static SRFileScanResult Scan(string path, IReadOnlyList<SRReplacementPattern> patterns)
		{
			if (string.IsNullOrEmpty(path))
			{
				return new SRFileScanResult(path, false, null, default);
			}

			if (!File.Exists(path))
			{
				return new SRFileScanResult(path, false, null, default);
			}

			string content;
			try
			{
				content = File.ReadAllText(path);
			}
			catch
			{
				return new SRFileScanResult(path, false, null, default);
			}

			bool modified = false;
			for (int i = 0; i < patterns.Count; i++)
			{
				content = TypeReplaceHelper.ApplyReplacement(content, patterns[i], out bool changed);
				if (changed)
				{
					modified = true;
				}
			}

			var verdict = SRVerdictScanner.Analyze(content);
			return new SRFileScanResult(path, modified, modified ? content : null, verdict);
		}
	}
}
