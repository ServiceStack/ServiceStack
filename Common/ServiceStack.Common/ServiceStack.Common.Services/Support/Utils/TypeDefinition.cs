using System;

namespace ServiceStack.Common.Services.Support.Utils
{
    internal class TypeDefinition
    {
        private const char TYPE_DEFINITION_SEPERATOR = ',';
        private const int TYPE_NAME_INDEX = 0;
        private const int ASSEMBLY_NAME_INDEX = 1;

        public TypeDefinition(string typeDefinition)
        {
            if (string.IsNullOrEmpty(typeDefinition))
            {
                throw new ArgumentNullException();
            }
            var parts = typeDefinition.Split(TYPE_DEFINITION_SEPERATOR);
            TypeName = parts[TYPE_NAME_INDEX].Trim();
            AssemblyName = (parts.Length > ASSEMBLY_NAME_INDEX) ? parts[ASSEMBLY_NAME_INDEX].Trim() : null;
        }

        public string TypeName { get; set; }

        public string AssemblyName { get; set; }
    }
}