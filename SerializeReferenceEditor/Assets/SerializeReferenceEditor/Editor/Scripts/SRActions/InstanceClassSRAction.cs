using System;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class InstanceClassSRAction : BaseSRAction
    {
        private readonly SRAttribute _srAttribute;
        private readonly string _type;

        public InstanceClassSRAction(SerializedProperty currentProperty, SerializedProperty parentProperty, SRAttribute srAttribute, string type)
            : base(currentProperty, parentProperty)
        {
            _srAttribute = srAttribute;
            _type = type;
        }

        protected override void DoApply()
        {
            var typeInfo = _srAttribute.TypeInfoByPath(_type);
            if(typeInfo == null)
            {
                Debug.LogErrorFormat("Type '{0}' not found.", _type);
                return;
            }

            Undo.RegisterCompleteObjectUndo(Property.serializedObject.targetObject, "Create instance of " + typeInfo.Type);
            Undo.FlushUndoRecordObjects();

            var instance = Activator.CreateInstance(typeInfo.Type);
            _srAttribute.OnCreate(instance);

            Property.managedReferenceValue = instance;
            Property.serializedObject.ApplyModifiedProperties();
        }
    }
}