namespace SerializeReferenceEditor.Editor.SRActions
{
    public static class SRClipboard
    {
        public static object ManagedReferenceValue { get; set; }

        public static bool HasValue => ManagedReferenceValue != null;
    }
}
