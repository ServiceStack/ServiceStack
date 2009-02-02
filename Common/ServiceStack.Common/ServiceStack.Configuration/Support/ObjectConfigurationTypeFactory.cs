using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;

namespace ServiceStack.Configuration.Support
{
	/// <summary>
	/// Constructs a type based upon the definition contained in ObjectConfigurationType
	/// </summary>
	internal class ObjectConfigurationTypeFactory : IObjectFactory
	{
		private Dictionary<string, Type> objectTypes;
		private readonly Dictionary<string, ObjectConfigurationType> objectTypeConfigs;
		private readonly Dictionary<string, LateBoundObjectTypeBuilder> cachedTypeBuilders;

		public ObjectConfigurationTypeFactory(Dictionary<string, ObjectConfigurationType> objectTypes)
		{
			this.objectTypeConfigs = objectTypes;
			cachedTypeBuilders = new Dictionary<string, LateBoundObjectTypeBuilder>();
		}

		public Dictionary<string, Type> ObjectTypes
		{
			get
			{
				if (objectTypes == null)
				{
					objectTypes = new Dictionary<string, Type>();
					foreach (var type in objectTypeConfigs)
					{
						var resolvedType = AssemblyUtils.FindType(type.Value.Type);
						objectTypes.Add(type.Key, resolvedType);
					}
				}
				return objectTypes;
			}
		}

		private LateBoundObjectTypeBuilder GetTypeBuilder(ObjectConfigurationType objectTypeDefinition)
		{
			if (!cachedTypeBuilders.ContainsKey(objectTypeDefinition.Name))
			{
				cachedTypeBuilders[objectTypeDefinition.Name] = new LateBoundObjectTypeBuilder(this, objectTypeDefinition);
			}
			return cachedTypeBuilders[objectTypeDefinition.Name];
		}

		public T Create<T>()
		{
			var toType = typeof(T);
			if (objectTypes == null)
			{
				//TODO: implement properly so it navigates all config types for the bestmatch (like registered providers)
				return (T)Create(toType.Name, toType);
			}
			var matchingTypes = objectTypes.Where(objectType => ReflectionUtils.CanCast(toType, objectType.Value)).ToList();
			if (matchingTypes.Count == 0)
			{
				return default(T);
			}
			if (matchingTypes.Count > 1)
			{
				throw new AmbiguousMatchException(
					string.Format("There are '{0}' possible matches available for type '{1}'. You must reference ambiguous matches by name.",
						matchingTypes.Count, toType.FullName));
			}
			return Create<T>(matchingTypes[0].Key);
		}

		public T Create<T>(string objectName)
		{
			return (T)Create(objectName, typeof(T));
		}

		public object Create(string objectName, Type returnType)
		{
			var objectTypeDefinition = objectTypeConfigs[objectName];
			var typeBuilder = GetTypeBuilder(objectTypeDefinition);
			return typeBuilder.Create(objectTypeDefinition, returnType);
		}

		public bool Contains(string objectName)
		{
			return objectTypeConfigs.ContainsKey(objectName);
		}

		internal ObjectConfigurationType GetObjectDefinition(string objectName)
		{
			return objectTypeConfigs.ContainsKey(objectName) ? objectTypeConfigs[objectName] : null;
		}
	}
}