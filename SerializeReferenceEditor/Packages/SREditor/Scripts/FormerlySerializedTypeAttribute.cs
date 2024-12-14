using System;
using System.Linq;

namespace SerializeReferenceEditor
{
    /// <summary>
    /// Атрибут для автоматической замены старого типа на новый при десериализации.
    /// Работает аналогично FormerlySerializedAs, но для типов в SerializeReference полях.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FormerlySerializedTypeAttribute : Attribute
    {
        public string OldTypeName { get; }
        public string OldNamespace { get; }
        public string OldAssemblyName { get; }

        /// <summary>
        /// Указывает старое имя типа для автоматической замены при десериализации.
        /// Поддерживаемые форматы:
        /// 1. Assembly, Namespace.Type
        /// 2. Assembly,Namespace.Type
        /// 3. Namespace.Type
        /// 4. Type
        /// </summary>
        /// <param name="oldTypeFullName">Полное или частичное имя типа</param>
        public FormerlySerializedTypeAttribute(string oldTypeFullName)
        {
            if (string.IsNullOrEmpty(oldTypeFullName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(oldTypeFullName));

            // Разделяем assembly и остальную часть имени типа
            var assemblyAndType = oldTypeFullName.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string typeWithNamespace;
            
            if (assemblyAndType.Length > 1)
            {
                // Есть имя сборки
                OldAssemblyName = assemblyAndType[0].Trim();
                typeWithNamespace = assemblyAndType[1].Trim();
            }
            else
            {
                // Нет имени сборки
                OldAssemblyName = null;
                typeWithNamespace = assemblyAndType[0].Trim();
            }

            // Разделяем namespace и имя типа
            var parts = typeWithNamespace.Split('.');
            if (parts.Length <= 1)
            {
                // Только имя типа
                OldTypeName = parts[0];
                OldNamespace = "";
            }
            else
            {
                // Последняя часть - имя типа, всё остальное - namespace
                OldTypeName = parts[^1];
                OldNamespace = string.Join(".", parts.Take(parts.Length - 1));
            }
        }

        /// <summary>
        /// Проверяет, соответствует ли переданный тип старому типу
        /// </summary>
        public bool Matches(string assemblyName, string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return false;

            // Если указана сборка, она должна совпадать
            if (!string.IsNullOrEmpty(OldAssemblyName) && 
                !string.Equals(assemblyName, OldAssemblyName, StringComparison.OrdinalIgnoreCase))
                return false;

            // Разделяем переданное имя типа на namespace и имя
            var parts = typeFullName.Split('.');
            if (parts.Length <= 1)
                return string.Equals(parts[0], OldTypeName, StringComparison.OrdinalIgnoreCase);

            var typeName = parts[^1];
            var nameSpace = string.Join(".", parts.Take(parts.Length - 1));

            // Если namespace указан, он должен совпадать
            if (!string.IsNullOrEmpty(OldNamespace) && 
                !string.Equals(nameSpace, OldNamespace, StringComparison.OrdinalIgnoreCase))
                return false;

            // Проверяем совпадение имени типа
            return string.Equals(typeName, OldTypeName, StringComparison.OrdinalIgnoreCase);
        }
    }
}