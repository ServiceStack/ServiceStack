using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack.Common.Services.Support.Config
{
    /// <summary>
    /// Creates an instance of a Type from a string value
    /// </summary>
    public class StringConverter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StringConverter));

        const string PARSE_METHOD = "Parse";
        const char ITEM_SEPERATOR = ',';
        const char KEY_VALUE_SEPERATOR = ':';
 
        private readonly Type type;
        private ConstructorInfo typeConstructor;
        private MethodInfo parseMethod;
        private Type[] genericCollectionArgumentTypes;
 
        /// <summary>
        /// Initializes a new instance of the <see cref="StringConverter"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public StringConverter(Type type)
        {
            this.type = type;
            Load();
        }

        /// <summary>
        /// Sets up this instance, binding the artefacts that does the string conversion.
        /// </summary>
        private void Load()
        {
            if (type.IsEnum || type == typeof(string))
            {
                return;
            }

            // Get the static Parse(string) method on the type supplied
            parseMethod = type.GetMethod(PARSE_METHOD, BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string) }, null);

            if (parseMethod == null)
            {
                typeConstructor = GetTypeStringConstructor(type);
                if (typeConstructor == null)
                {
                    Type[] interfaces = type.FindInterfaces ((t, critera) => 
                        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);
                    var isGenericCollection = interfaces.Length > 0;
                    if (isGenericCollection)
                    {
                        //genericType = GetGenericType(type);
                        typeConstructor = type.GetConstructor(Type.EmptyTypes);
                        if (typeConstructor != null)
                        {
                            genericCollectionArgumentTypes = interfaces[0].GetGenericArguments();
                            return;
                        }
                    }
                    throw new NotSupportedException(string.Format("Cannot create type {0} from a string.", type.Name));
                }
            }
        }

        public static Type GetGenericType(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType) return type;
                type = type.BaseType;
            }
            return type;
        }

        /// <summary>
        /// Determines whether the specified type is convertible from string.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is convertible from string; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanCreateFromString(Type type)
        {
            //True if Enum or string
            if (type.IsEnum || type == typeof(string))
            {
                return true;
            }

            // Get the static Parse(string) method on the type supplied
            var parseMethodInfo = type.GetMethod(PARSE_METHOD, BindingFlags.Public | BindingFlags.Static, null,
                new [] { typeof(string) }, null);
            if (parseMethodInfo != null)
            {
                return true;
            }

            var hasStringConstructor = GetTypeStringConstructor(type) != null;

            return !hasStringConstructor;
        }

        /// <summary>
        /// Get the type(string) constructor if exists
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static ConstructorInfo GetTypeStringConstructor(Type type)
        {
            foreach (var ci in type.GetConstructors())
            {
                var paramInfos = ci.GetParameters();
                bool matchFound = (paramInfos.Length == 1 && paramInfos[0].ParameterType == typeof(string));
                if (matchFound)
                {
                    return ci;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public object Parse(string value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }
            if (parseMethod != null)
            {
                return parseMethod.Invoke(null, new object[] { value });
            }
            if (genericCollectionArgumentTypes != null)
            {
                var isDictionary = type.FindInterfaces((t, critera) =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null).Length == 1;
                if (isDictionary)
                {
                    return CreateDictionaryFromTextValue(value);
                }
                var isList = this.type.FindInterfaces((t, critera) =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null).Length == 1;
                if (isList)
                {
                    return CreateListFromTextValue(value);
                }
            }
            if (typeConstructor != null)
            {
                typeConstructor.Invoke(null, new object[] { value });
            }
            throw new NotSupportedException(string.Format("Could not parse text value '{0}' to create type '{1}'",
                value, type));
        }

        public object CreateListFromTextValue(string text)
        {
            var textValues = text.Split(ITEM_SEPERATOR);
            var list = typeConstructor.Invoke(new object[]{});
            var valueTypeConverter = new StringConverter(genericCollectionArgumentTypes[0]);
            foreach (var textValue in textValues)
            {
                var value = valueTypeConverter.Parse(textValue);
                SetGenericCollection(list, new[] { value });
            }
            return list;
        }

        public object CreateDictionaryFromTextValue(string textValue)
        {
            const int KEY_INDEX = 0;
            const int VALUE_INDEX = 1;
            var map = typeConstructor.Invoke(new object[] { });
            var keyTypeConverter = new StringConverter(genericCollectionArgumentTypes[KEY_INDEX]);
            var valueTypeConverter = new StringConverter(genericCollectionArgumentTypes[VALUE_INDEX]);
            foreach (var item in textValue.Split(ITEM_SEPERATOR))
            {
                var keyValuePair = item.Split(KEY_VALUE_SEPERATOR);
                var keyValue = keyTypeConverter.Parse(keyValuePair[KEY_INDEX]);
                var value = valueTypeConverter.Parse(keyValuePair[VALUE_INDEX]);
                SetGenericCollection(map, new[] { keyValue, value });
            }
            return map;
        }

        private static void SetGenericCollection(object genericObj, object[] genericValues)
        {
            var methodInfo = genericObj.GetType().GetMethod("Add");
            if (methodInfo == null) return;
            try
            {
                methodInfo.Invoke(genericObj, genericValues);
            }
            catch (Exception ex)
            {
                log.WarnFormat("Could not set generic collection '{0}' with values '{1}'\n {2}",
                               genericObj.GetType().FullName, genericValues, ex.Message);
            }
        }
    }
}