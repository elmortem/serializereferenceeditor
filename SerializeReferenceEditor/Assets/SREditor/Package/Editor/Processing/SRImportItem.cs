namespace SerializeReferenceEditor.Editor.Processing
{
	public readonly struct SRImportItem
	{
		public readonly string Path;
		public readonly long SizeBytes;

		public SRImportItem(string path, long sizeBytes)
		{
			Path = path;
			SizeBytes = sizeBytes;
		}
	}
}
