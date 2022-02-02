using System;

namespace ServiceStack.Common.Support
{
	internal class AssemblyTypeDefinition
	{
		private const char TypeDefinitionSeperator = ',';
		private const int TypeNameIndex = 0;
		private const int AssemblyNameIndex = 1;

		public AssemblyTypeDefinition(string typeDefinition)
		{
			if (string.IsNullOrEmpty(typeDefinition))
			{
				throw new ArgumentNullException();
			}
			var parts = typeDefinition.Split(TypeDefinitionSeperator);
			TypeName = parts[TypeNameIndex].Trim();
			AssemblyName = (parts.Length > AssemblyNameIndex) ? parts[AssemblyNameIndex].Trim() : null;
		}

		public string TypeName { get; set; }

		public string AssemblyName { get; set; }
	}
}