using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack.Common.Utils
{
    public class ReflectionUtils
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReflectionUtils));
        
        /// <summary>
        /// Populate an object with Example data.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object PopulateObject(object obj)
        {
            if (obj == null) return null;

            var members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var info in members)
            {
                var fieldInfo = info as FieldInfo;
                var propertyInfo = info as PropertyInfo;
                if (fieldInfo != null || propertyInfo != null)
                {
                    var memberType = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                    var value = CreateDefaultValue(memberType);
                    SetValue(fieldInfo, propertyInfo, obj, value);
                }
            }
            return obj;
        }

        /// <summary>
        /// Populate an instance of the type with Example data.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object PopulateType(Type type)
        {
            var obj = Activator.CreateInstance(type);
            return PopulateObject(obj);
        }

        public static void SetProperty(object obj, PropertyInfo propertyInfo, object value)
        {
            if (!propertyInfo.CanWrite)
            {
                log.WarnFormat("Attempted to set read only property '{0}'", propertyInfo.Name);
                return;
            }
            var propertySetMetodInfo = propertyInfo.GetSetMethod();
            if (propertySetMetodInfo != null)
            {
                propertySetMetodInfo.Invoke(obj, new[] { value });
            }
        }

        private static void SetValue(FieldInfo fieldInfo, PropertyInfo propertyInfo, object obj, object value)
        {
            try
            {
                if (IsUnsettableValue(fieldInfo, propertyInfo)) return;
                if (fieldInfo != null && !fieldInfo.IsLiteral)
                {
                    fieldInfo.SetValue(obj, value);
                }
                else 
                {
                    SetProperty(obj, propertyInfo, value);
                }
                PopulateObject(value);
            }
            catch (Exception ex)
            {
                var name = (fieldInfo != null) ? fieldInfo.Name : propertyInfo.Name;
                log.DebugFormat("Could not set member: {0}. Error: {1}", name, ex.Message);
            }
        }

        private static bool IsUnsettableValue(FieldInfo fieldInfo, PropertyInfo propertyInfo)
        {
            if (propertyInfo != null && propertyInfo.ReflectedType != null)
            {
                // Properties on non-user defined classes should not be set
                // Currently we define those properties as properties declared on
                // types defined in mscorlib

                if (propertyInfo.DeclaringType.Assembly == typeof(object).Assembly)
                {
                    return true;
                }
            }

            return false;
        }

        private static object[] CreateDefaultValues(IEnumerable<Type> types)
        {
            var values = new List<object>();
            foreach (var type in types)
            {
                values.Add(CreateDefaultValue(type));
            }
            return values.ToArray();
        }

        private static object CreateDefaultValue(Type type)
        {
            if (type == typeof(string))
            {
                return type.Name;
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            if (type.IsArray)
            {
                return PopulateArray(type);
            }

            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            var hasEmptyConstructor = constructorInfo != null;

            if (hasEmptyConstructor)
            {
                var value = constructorInfo.Invoke(new object[0]);
                
                Type[] interfaces = type.FindInterfaces ((t, critera) => 
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)
                    , null);

                bool isGenericCollection = interfaces.Length > 0;

                if (isGenericCollection)
                {
                    SetGenericCollection(interfaces[0], type, value);
                }

                return value;
            }
            return null;
        }

        private static void SetGenericCollection(Type realisedListType, Type type, object genericObj)
        {
            var args = realisedListType.GetGenericArguments();

            if (args.Length != 1)
            {
                log.ErrorFormat("Found a generic list that does not take one generic argument: {0}", realisedListType);

                return;
            }

            var methodInfo = type.GetMethod("Add");

            if (methodInfo != null)
            {
                var argValues = CreateDefaultValues(args);

                methodInfo.Invoke(genericObj, argValues);

                log.DebugFormat("Added value '{0}' to type '{1}", argValues, genericObj.GetType());
            }
        }

        private static Array PopulateArray(Type type)
        {
            var objArray = Array.CreateInstance(type, 1);
            var elementType = objArray.GetType().GetElementType();
            var objElementType = PopulateType(elementType);
            objArray.SetValue(objElementType, 0);
            PopulateObject(objElementType);
            return objArray;
        }
    }
}