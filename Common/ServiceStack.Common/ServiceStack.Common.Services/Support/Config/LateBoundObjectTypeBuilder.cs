using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Services.Utils;

namespace ServiceStack.Common.Services.Support.Config
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
        //to convert to a type from a string value use the static parse method on the type you are trying to convert to.
        private const string ERROR_NO_MATCHING_CONSTRUCTOR = "No matching constructor found for type {0}";
        private const string ERROR_NO_PROPERTY_EXISTS = "No property named {0} was found on type {1}";
        private const string ERROR_PROPERTY_TYPE_NOT_SUPPORTED = "setting property {0} on type {1} not supported";
        private const string ERROR_SETTING_PROPERTY = "Cannot set property {0} on type {1}";
        private const string ERROR_CREATING_TYPE = "Error creating type {0}";
        private const string ERROR_TYPE_NOT_FOUND = "Could not find type: {0}";

        private readonly Type objectType;
        private ConstructorInfo constructorInfo;
        private readonly List<StringConverter> constructorValueConverters;

        private readonly List<PropertyInfo> properties;
        private readonly List<StringConverter> propertyValueConverters;

        internal LateBoundObjectTypeBuilder(ObjectConfigurationType objectTypeDefinition)
        {
            constructorInfo = null;
            objectType = AssemblyUtils.FindType(objectTypeDefinition.Type);
            if (objectType == null)
            {
                throw new TypeLoadException(string.Format(ERROR_TYPE_NOT_FOUND, objectTypeDefinition.Type));
            }
            constructorValueConverters = new List<StringConverter>();
            propertyValueConverters = new List<StringConverter>();
            properties = new List<PropertyInfo>();

            if (objectTypeDefinition.ConstructorArgs.Count > 0)
            {
                AppendConstructorDefinition(objectTypeDefinition.ConstructorArgs);
            }
            if (objectTypeDefinition.Properties.Count > 0)
            {
                AppendPropertyDefnition(objectTypeDefinition.Properties);
            }
        }

        /// <summary>
        /// Dummy method to use if property or arg type is a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Parse(string value)
        {
            return value;
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

                        //The type of each constructor argument will need to know how to create itself from a string value.
                        //We do this by calling the static 'Parse(string)' method on the type of each argument

                        if (!string.IsNullOrEmpty(constructorArgDefinition.Ref))
                        {
                            throw new NotImplementedException(
                                string.Format("{0} does not yet support creating objects by reference",
                                objectType.GetType().FullName));
                            //TODO: implement if required
                        }
                        else
                        {
                            if (StringConverter.CanCreateFromString(constructorParam.ParameterType))
                            {
                                constructorValueConverters.Add(new StringConverter(constructorParam.ParameterType));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    bool matchingConstructorFound = constructorValueConverters.Count == constructorParams.Length;
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

        /// <summary>
        /// Append the property definition
        /// </summary>
        /// <param name="propertyDefinitions"></param>
        private void AppendPropertyDefnition(IList<PropertyConfigurationType> propertyDefinitions)
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
                if (!StringConverter.CanCreateFromString(pi.PropertyType))
                {
                    throw new TypeLoadException(
                        string.Format(ERROR_PROPERTY_TYPE_NOT_SUPPORTED, propertyDefinition.Name, objectType.Name));
                }
                properties.Add(pi);
                propertyValueConverters.Add(new StringConverter(pi.PropertyType));
            }
        }

        /// <summary>
        /// Use the values in the ObjectConfigurationType to create a new instance of this type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectTypeDefinition"></param>
        /// <returns></returns>
        public T Create<T>(ObjectConfigurationType objectTypeDefinition)
        {
            string[] constructorArgTextValues = new string[objectTypeDefinition.ConstructorArgs.Count];
            for (int i = 0; i < objectTypeDefinition.ConstructorArgs.Count; i++)
            {
                constructorArgTextValues[i] = objectTypeDefinition.ConstructorArgs[i].Value;
            }
            string[] propertyTextValues = new string[objectTypeDefinition.Properties.Count];
            for (int i = 0; i < objectTypeDefinition.Properties.Count; i++)
            {
                propertyTextValues[i] = objectTypeDefinition.Properties[i].Value;
            }
            return Create<T>(constructorArgTextValues, propertyTextValues);
        }

        /// <summary>
        /// Use the values supplied to create a new instance of this type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorArgTextValues"></param>
        /// <param name="propertyTextValues"></param>
        /// <returns></returns>
        public T Create<T>(string[] constructorArgTextValues, string[] propertyTextValues)
        {
            try
            {
                object[] constructorArgValues = new object[constructorArgTextValues.Length];
                for (int i = 0; i < constructorArgTextValues.Length; i++)
                {
                    constructorArgValues[i] = constructorValueConverters[i].Parse(constructorArgTextValues[i]);
                }
                T objectInstance;
                if (constructorInfo == null)
                {
                    objectInstance = (T)Activator.CreateInstance(objectType, new object[0]);
                }
                else
                {
                    objectInstance = (T)constructorInfo.Invoke(constructorArgValues);
                }

                for (int i = 0; i < propertyTextValues.Length; i++)
                {
                    PropertyInfo pi = properties[i];
                    try
                    {
                        object value = propertyValueConverters[i].Parse(propertyTextValues[i]);
                        pi.SetValue(objectInstance, value, null);
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