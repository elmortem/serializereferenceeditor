namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public readonly struct SRFileScanResult
	{
		public readonly string Path;
		public readonly bool Modified;
		public readonly string NewContent;
		public readonly SRFileVerdict Verdict;

		public SRFileScanResult(string path, bool modified, string newContent, SRFileVerdict verdict)
		{
			Path = path;
			Modified = modified;
			NewContent = newContent;
			Verdict = verdict;
		}
	}
}
