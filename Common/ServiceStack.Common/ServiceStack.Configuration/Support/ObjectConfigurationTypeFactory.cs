using System.Collections.Generic;

namespace ServiceStack.Configuration.Support
{
	/// <summary>
	/// Constructs a type based upon the definition contained in ObjectConfigurationType
	/// </summary>
	internal class ObjectConfigurationTypeFactory : IObjectFactory
	{

		private readonly Dictionary<string, ObjectConfigurationType> objectTypes;
		private readonly Dictionary<string, LateBoundObjectTypeBuilder> cachedTypeBuilders;

		public ObjectConfigurationTypeFactory(Dictionary<string, ObjectConfigurationType> objectTypes)
		{
			this.objectTypes = objectTypes;
			cachedTypeBuilders = new Dictionary<string, LateBoundObjectTypeBuilder>();
		}

		private LateBoundObjectTypeBuilder GetTypeBuilder(ObjectConfigurationType objectTypeDefinition)
		{
			if (!cachedTypeBuilders.ContainsKey(objectTypeDefinition.Name))
			{
				cachedTypeBuilders[objectTypeDefinition.Name] = new LateBoundObjectTypeBuilder(objectTypeDefinition);
			}
			return cachedTypeBuilders[objectTypeDefinition.Name];
		}

		public T Create<T>(string objectName)
		{
			ObjectConfigurationType objectTypeDefinition = objectTypes[objectName];
			LateBoundObjectTypeBuilder typeBuilder = GetTypeBuilder(objectTypeDefinition);
			return typeBuilder.Create<T>(objectTypeDefinition);
		}

		public bool Contains(string objectName)
		{
			return objectTypes.ContainsKey(objectName);
		}
	}
}