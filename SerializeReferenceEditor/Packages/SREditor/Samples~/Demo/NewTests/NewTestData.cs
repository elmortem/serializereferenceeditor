using System;
using SerializeReferenceEditor;

namespace Demo.NewTests
{
    [Serializable, SRName("New Test")]
    [FormerlySerializedType("SRDemo, Demo.OldTestData")]
    public class NewTestData : BaseTestData
    {
        public int Value;
        public float FloatValue;
        public bool BoolValue;
    }
}