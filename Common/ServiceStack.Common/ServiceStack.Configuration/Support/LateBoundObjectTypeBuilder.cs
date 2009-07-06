using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;

namespace ServiceStack.Configuration.Support
{
	/// <summary>
	/// Hold all the objectType definition information required to be able to create a new instance of this objectType.
	/// The idea is to hold all the information required to create an instance of an object so a new instance of the object
	/// that this definition represent can be created by just supply the values.
	/// 
	/// It is also design for optimal performance.
	/// </summary>
	internal class LateBoundObjectTypeBuilder
	{
		private const string ERROR_NO_MATCHING_CONSTRUCTOR = "No matching constructor found for type '{0}'";
		private const string ERROR_NO_PROPERTY_EXISTS = "No property named '{0}' was found on type '{1}'";
		private const string ERROR_PROPERTY_TYPE_NOT_SUPPORTED = "setting property '{0}' on type '{1}' not supported";
		private const string ERROR_SETTING_PROPERTY = "Cannot set property '{0}' on type '{1}'";
		private const string ERROR_SETTING_REF_PROPERTY = "Cannot set ref property '{0}' on type '{1}'";
		private const string ERROR_CREATING_TYPE = "Error creating type '{0}'";
		private const string ERROR_TYPE_NOT_FOUND = "Could not find type '{0}'";
		private const string ERROR_REF_NOT_FOUND = "Could not find type definition identified by ref '{0}'";

		private readonly ObjectConfigurationTypeFactory factory;
		private readonly Type objectType;
		private ConstructorInfo constructorInfo;
		private readonly List<Type> constructorValueTypes;
		private readonly List<RefType> constructorRefTypes;

		private readonly List<PropertyInfo> properties;
		private readonly List<Type> propertyValueTypes;
		private readonly List<RefType> propertyRefTypes;

		internal LateBoundObjectTypeBuilder(ObjectConfigurationTypeFactory factory, ObjectConfigurationType objectTypeDefinition)
		{
			this.factory = factory;
			constructorInfo = null;
			objectType = AssemblyUtils.FindType(objectTypeDefinition.Type);
			if (objectType == null)
			{
				throw new TypeLoadException(string.Format(ERROR_TYPE_NOT_FOUND, objectTypeDefinition.Type));
			}
			constructorValueTypes = new List<Type>();
			constructorRefTypes = new List<RefType>();
			propertyValueTypes = new List<Type>();
			propertyRefTypes = new List<RefType>();
			properties = new List<PropertyInfo>();

			if (objectTypeDefinition.ConstructorArgs.Count > 0)
			{
				AppendConstructorDefinition(objectTypeDefinition.ConstructorArgs);
			}
			if (objectTypeDefinition.Properties.Count > 0)
			{
				AppendPropertyDefinition(objectTypeDefinition.Properties);
			}
		}

		/// <summary>
		/// Find the right constructor for the matching types
		/// </summary>
		/// <param name="constructorArgDefinitions"></param>
		private void AppendConstructorDefinition(IList<PropertyConfigurationType> constructorArgDefinitions)
		{
			foreach (ConstructorInfo ci in objectType.GetConstructors())
			{
				ParameterInfo[] constructorParams = ci.GetParameters();

				//We can only call a constructor that has the right number of arguments.
				bool possibleMatch = constructorParams.Length == constructorArgDefinitions.Count;
				if (possibleMatch)
				{
					for (int i = 0; i < constructorParams.Length; i++)
					{
						ParameterInfo constructorParam = constructorParams[i];
						PropertyConfigurationType constructorArgDefinition = constructorArgDefinitions[i];

						if (!string.IsNullOrEmpty(constructorArgDefinition.Ref))
						{
							RefType refType = GetRefType(constructorArgDefinition.Ref, constructorParam.ParameterType);
							constructorRefTypes.Add(refType);
							constructorValueTypes.Add(refType.GetType());
						}
						else if (StringConverterUtils.CanCreateFromString(constructorParam.ParameterType))
						{
							constructorValueTypes.Add(constructorParam.ParameterType);
						}
						else
						{
							break;
						}
					}

					bool matchingConstructorFound = constructorValueTypes.Count == constructorParams.Length;
					if (matchingConstructorFound)
					{
						constructorInfo = ci;
						return;
					}
				}
			}
			//if it got this far no matching constructor *that we can use* has been found
			throw new TypeLoadException(string.Format(ERROR_NO_MATCHING_CONSTRUCTOR, objectType.Name));
		}

		private RefType GetRefType(string definitionRef, Type paramType)
		{
			var refTypeDefinition = factory.GetObjectDefinition(definitionRef);
			if (refTypeDefinition == null)
			{
				throw new TypeLoadException(string.Format(ERROR_REF_NOT_FOUND, definitionRef));
			}

			var type = AssemblyUtils.FindType(refTypeDefinition.Type);
			if (type == null)
			{
				throw new TypeLoadException(string.Format(ERROR_TYPE_NOT_FOUND, refTypeDefinition.Type));
			}

			return ReflectionUtils.CanCast(paramType, type) ? new RefType(definitionRef, paramType) : null;
		}

		/// <summary>
		/// Append the property definition
		/// </summary>
		/// <param name="propertyDefinitions"></param>
		private void AppendPropertyDefinition(IList<PropertyConfigurationType> propertyDefinitions)
		{
			for (int i = 0; i < propertyDefinitions.Count; i++)
			{
				PropertyConfigurationType propertyDefinition = propertyDefinitions[i];
				PropertyInfo pi = objectType.GetProperty(propertyDefinition.Name);
				if (pi == null)
				{
					throw new TypeLoadException(
						string.Format(ERROR_NO_PROPERTY_EXISTS, propertyDefinition.Name, objectType.Name));
				}
				if (!string.IsNullOrEmpty(propertyDefinition.Ref))
				{
					var refType = GetRefType(propertyDefinition.Ref, pi.PropertyType);
					if (refType == null)
					{
						throw new TypeLoadException(
							string.Format(ERROR_SETTING_REF_PROPERTY, propertyDefinition.Name, objectType.Name));
					}
					propertyRefTypes.Add(refType);
					properties.Add(pi);
					propertyValueTypes.Add(refType.GetType());
				} 
				else if (StringConverterUtils.CanCreateFromString(pi.PropertyType))
				{
					properties.Add(pi);
					propertyValueTypes.Add(pi.PropertyType);
				}
				else
				{
					throw new TypeLoadException(
						string.Format(ERROR_PROPERTY_TYPE_NOT_SUPPORTED, propertyDefinition.Name, objectType.Name));
				}
			}
		}

		/// <summary>
		/// Use the values in the ObjectConfigurationType to create a new instance of this type
		/// </summary>
		/// <param name="objectTypeDefinition">The object type definition.</param>
		/// <param name="returnType">Type of the return.</param>
		/// <returns></returns>
		public object Create(ObjectConfigurationType objectTypeDefinition, Type returnType)
		{
			object[] constructorArgValues = GetConstructorArgValues(objectTypeDefinition);
			object[] propertyValues = GetPropertyValues(objectTypeDefinition);
			return Create(constructorArgValues, propertyValues);
		}

		private object[] GetConstructorArgValues(ObjectConfigurationType objectTypeDefinition)
		{
			var constructorArgValues = new object[constructorValueTypes.Count];
			int refTypesCount = 0;
			for (int i = 0; i < objectTypeDefinition.ConstructorArgs.Count; i++)
			{
				var constructorValueType = constructorValueTypes[i];
				object argValue;
				if (constructorValueType == typeof(RefType))
				{
					var refType = this.constructorRefTypes[refTypesCount++];
					argValue = factory.Create(refType.Name, refType.Type);
				}
				else
				{
					var constructorArgTextValue = objectTypeDefinition.ConstructorArgs[i].Value;
					argValue = StringConverterUtils.Parse(constructorArgTextValue, constructorValueType);
				}
				constructorArgValues[i] = argValue;
			}
			return constructorArgValues;
		}

		private object[] GetPropertyValues(ObjectConfigurationType objectTypeDefinition)
		{
			var propertyValues = new object[this.propertyValueTypes.Count];
			int refTypesCount = 0;
			for (int i = 0; i < objectTypeDefinition.Properties.Count; i++)
			{
				var propertyValueType = this.propertyValueTypes[i];
				object argValue;
				if (propertyValueType == typeof(RefType))
				{
					var refType = this.propertyRefTypes[refTypesCount++];
					argValue = factory.Create(refType.Name, refType.Type);
				}
				else
				{
					var propertyTextValue = objectTypeDefinition.Properties[i].Value;
					argValue = StringConverterUtils.Parse(propertyTextValue, propertyValueType);
				}
				propertyValues[i] = argValue;
			}
			return propertyValues;
		}

		/// <summary>
		/// Use the values supplied to create a new instance of this type
		/// </summary>
		/// <param name="constructorArgValues">The constructor arg values.</param>
		/// <param name="propertyValues">The property text values.</param>
		/// <returns></returns>
		public object Create(object[] constructorArgValues, object[] propertyValues)
		{
			try
			{
				object objectInstance;
				if (constructorInfo == null)
				{
					objectInstance = Activator.CreateInstance(objectType, new object[0]);
				}
				else
				{
					objectInstance = constructorInfo.Invoke(constructorArgValues);
				}

				for (int i = 0; i < propertyValues.Length; i++)
				{
					PropertyInfo pi = properties[i];
					try
					{
						pi.SetValue(objectInstance, propertyValues[i], null);
					}
					catch (Exception ex)
					{
						throw new TypeLoadException(string.Format(ERROR_SETTING_PROPERTY, pi.Name, objectType.Name), ex);
					}
				}
				return objectInstance;
			}
			catch (Exception ex)
			{
				throw new TypeLoadException(string.Format(ERROR_CREATING_TYPE, objectType.Name), ex);
			}
		}
	}
}