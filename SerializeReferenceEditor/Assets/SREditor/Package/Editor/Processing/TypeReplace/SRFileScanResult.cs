namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public readonly struct SRFileScanResult
	{
		public readonly string Path;
		public readonly bool Modified;
		public readonly string NewContent;

		public SRFileScanResult(string path, bool modified, string newContent)
		{
			Path = path;
			Modified = modified;
			NewContent = newContent;
		}
	}
}
