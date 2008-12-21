using System;

namespace ServiceStack.Messaging.ActiveMq.Support.Utils
{
    internal class TypeDefinition
    {
        private const char TYPE_DEFINITION_SEPERATOR = ',';
        private const int TYPE_NAME_INDEX = 0;
        private const int ASSEMBLY_NAME_INDEX = 1;
        private string typeName;
        private string assemblyName;

        public TypeDefinition(string typeDefinition)
        {
            if (string.IsNullOrEmpty(typeDefinition))
            {
                throw new ArgumentNullException();
            }
            string[] parts = typeDefinition.Split(TYPE_DEFINITION_SEPERATOR);
            typeName = parts[TYPE_NAME_INDEX].Trim();
            assemblyName = (parts.Length > ASSEMBLY_NAME_INDEX) ? parts[ASSEMBLY_NAME_INDEX].Trim() : null;
        }

        public string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        public string AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }
    }
}