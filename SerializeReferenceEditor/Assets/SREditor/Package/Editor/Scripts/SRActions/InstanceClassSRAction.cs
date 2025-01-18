using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class InstanceClassSRAction : BaseSRAction
    {
        private readonly TypeInfo[] _typeInfos;
        private readonly string _type;

        public InstanceClassSRAction(SerializedProperty currentProperty, SerializedProperty parentProperty, TypeInfo[] typeInfos, string type)
            : base(currentProperty, parentProperty)
        {
            _typeInfos = typeInfos;
            _type = type;
        }

        protected override void DoApply()
        {
            TypeInfo selectedTypeInfo = _typeInfos.FirstOrDefault(t => t.Path == _type);
            if (selectedTypeInfo == null)
            {
                Debug.LogErrorFormat("Type '{0}' not found.", _type);
                return;
            }

            Undo.RegisterCompleteObjectUndo(Property.serializedObject.targetObject, "Create instance of " + selectedTypeInfo.Type.Name);
            Undo.FlushUndoRecordObjects();

            var instance = Activator.CreateInstance(selectedTypeInfo.Type);
            Property.managedReferenceValue = instance;
            Property.serializedObject.ApplyModifiedProperties();
        }
    }
}