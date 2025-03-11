using System;
using System.Linq;

namespace SerializeReferenceEditor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FormerlySerializedTypeAttribute : Attribute
    {
#if UNITY_EDITOR
        private readonly string _oldTypeName;
        private string _oldNamespace;
        private string _oldAssemblyName;
        private bool _isInitialized;
        private readonly string _oldTypeFullName;

        public string OldTypeName => _oldTypeName;
        
        public string OldNamespace 
        {
            get
            {
                EnsureInitialized();
                return _oldNamespace;
            }
        }
        
        public string OldAssemblyName 
        {
            get
            {
                EnsureInitialized();
                return _oldAssemblyName;
            }
        }
        
        public FormerlySerializedTypeAttribute(string oldTypeFullName)
        {
            if (string.IsNullOrEmpty(oldTypeFullName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(oldTypeFullName));

            _oldTypeFullName = oldTypeFullName;
            
            var assemblyAndType = oldTypeFullName.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string typeWithNamespace;
            
            if (assemblyAndType.Length > 1)
            {
                _oldAssemblyName = assemblyAndType[0].Trim();
                typeWithNamespace = assemblyAndType[1].Trim();
            }
            else
            {
                typeWithNamespace = assemblyAndType[0].Trim();
            }

            var parts = typeWithNamespace.Split('.');
            if (parts.Length <= 1)
            {
                _oldTypeName = parts[0];
            }
            else
            {
                _oldTypeName = parts[^1];
                _oldNamespace = string.Join(".", parts.Take(parts.Length - 1));
            }
            
            _isInitialized = !string.IsNullOrEmpty(_oldAssemblyName) && !string.IsNullOrEmpty(_oldNamespace);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            var type = Services.SRFormerlyTypeCache.GetTypeForAttribute(this);
            if (type != null)
            {
                _oldAssemblyName ??= type.Assembly.GetName().Name ?? string.Empty;
                _oldNamespace ??= type.Namespace ?? string.Empty;
                _isInitialized = true;
            }
        }
#else
        public FormerlySerializedTypeAttribute(string oldTypeFullName)
        {
        }
#endif
    }
}