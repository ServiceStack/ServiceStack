using System;
using System.Collections.Generic;
#if NETFX_CORE
using System.Collections.Concurrent;
#endif
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Common.Support;
using ServiceStack.Logging;
using ServiceStack.Net30.Collections.Concurrent;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Utils
{
    [DataContract(Namespace = "http://schemas.servicestack.net/types")]
    public class CustomHttpResult { }

    public class ReflectionUtils
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(ReflectionUtils));

        /// <summary>
        /// Populate an object with Example data.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object PopulateObject(object obj)
        {
            if (obj == null) return null;
            var httpResult = obj as IHttpResult;
            if (httpResult != null)
            {
                obj = new CustomHttpResult();
            }

            var type = obj.GetType();
#if NETFX_CORE
            if (type.GetTypeInfo().IsArray || type.GetTypeInfo().IsValueType || type.GetTypeInfo().IsGenericType)
#else
            if (type.IsArray || type.IsValueType || type.IsGenericType)
#endif
            {
                var value = CreateDefaultValue(type, new Dictionary<Type, int>(20));
                return value;
            }

            return PopulateObjectInternal(obj, new Dictionary<Type, int>(20));
        }

        /// <summary>
        /// Populates the object with example data.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recursionInfo">Tracks how deeply nested we are</param>
        /// <returns></returns>
        private static object PopulateObjectInternal(object obj, Dictionary<Type, int> recursionInfo)
        {
            if (obj == null) return null;
            if (obj is string) return obj; // prevents it from dropping into the char[] Chars property.  Sheesh
            var type = obj.GetType();

#if NETFX_CORE
            var fields = type.GetRuntimeFields().Where(p => p.IsPublic && !p.IsStatic);
            foreach (var fieldInfo in fields)
            {
                var value = CreateDefaultValue(fieldInfo.FieldType, recursionInfo);
                SetValue(fieldInfo, null, obj, value);
            }

            var properties = type.GetRuntimeProperties();
            foreach (var propertyInfo in properties)
            {
                var value = CreateDefaultValue(propertyInfo.PropertyType, recursionInfo);
                SetValue(null, propertyInfo, obj, value);
            }
#else
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var info in members)
            {
                var fieldInfo = info as FieldInfo;
                var propertyInfo = info as PropertyInfo;
                if (fieldInfo != null || propertyInfo != null)
                {
                    var memberType = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                    var value = CreateDefaultValue(memberType, recursionInfo);
                    SetValue(fieldInfo, propertyInfo, obj, value);
                }
            }
#endif
            return obj;
        }

        private static readonly Dictionary<Type, object> DefaultValueTypes 
            = new Dictionary<Type, object>();

        public static object GetDefaultValue(Type type)
        {
#if NETFX_CORE
            if (!type.GetTypeInfo().IsValueType) return null;
#else
            if (!type.IsValueType) return null;
#endif

            object defaultValue;
            lock (DefaultValueTypes)
            {
                if (!DefaultValueTypes.TryGetValue(type, out defaultValue))
                {
                    defaultValue = Activator.CreateInstance(type);
                    DefaultValueTypes[type] = defaultValue;
                }
            }

            return defaultValue;
        }

        private static readonly ConcurrentDictionary<string, AssignmentDefinition> AssignmentDefinitionCache 
            = new ConcurrentDictionary<string, AssignmentDefinition>();

        public static AssignmentDefinition GetAssignmentDefinition(Type toType, Type fromType)
        {
            var cacheKey = toType.FullName + "<" + fromType.FullName;

            return AssignmentDefinitionCache.GetOrAdd(cacheKey, delegate {

                var definition = new AssignmentDefinition {
                    ToType = toType,
                    FromType = fromType,
                };

                var readMap = GetMembers(fromType, isReadable: true);
                var writeMap = GetMembers(toType, isReadable: false);

                foreach (var assignmentMember in readMap)
                {
                    AssignmentMember writeMember;
                    if (writeMap.TryGetValue(assignmentMember.Key, out writeMember))
                    {
                        definition.AddMatch(assignmentMember.Key, assignmentMember.Value, writeMember);
                    }
                }

                return definition;
            });
        }

        private static Dictionary<string, AssignmentMember> GetMembers(Type type, bool isReadable)
        {
            var map = new Dictionary<string, AssignmentMember>();

#if NETFX_CORE
            var members = new List<MemberInfo>();
            members.AddRange(type.GetRuntimeFields().Where(p => p.IsPublic && !p.IsStatic));
            members.AddRange(type.GetRuntimeProperties());
#else
            var members = type.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
#endif
            foreach (var info in members)
            {
                if (info.DeclaringType == typeof(object)) continue;

                var propertyInfo = info as PropertyInfo;
                if (propertyInfo != null)
                {
                    if (isReadable)
                    {
                        if (propertyInfo.CanRead)
                        {
                            map[info.Name] = new AssignmentMember(propertyInfo.PropertyType, propertyInfo);
                            continue;
                        }
                    }
                    else
                    {
                        if (propertyInfo.CanWrite)
                        {
                            map[info.Name] = new AssignmentMember(propertyInfo.PropertyType, propertyInfo);
                            continue;
                        }
                    }
                }

                var fieldInfo = info as FieldInfo;
                if (fieldInfo != null)
                {
                    map[info.Name] = new AssignmentMember(fieldInfo.FieldType, fieldInfo);
                    continue;
                }

                var methodInfo = info as MethodInfo;
                if (methodInfo != null)
                {
                    var parameterInfos = methodInfo.GetParameters();
                    if (isReadable)
                    {
                        if (parameterInfos.Length == 0)
                        {
                            var name = info.Name.StartsWith("get_") ? info.Name.Substring(4) : info.Name;
                            if (!map.ContainsKey(name))
                            {
                                map[name] = new AssignmentMember(methodInfo.ReturnType, methodInfo);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (parameterInfos.Length == 1 && methodInfo.ReturnType == typeof(void))
                        {
                            var name = info.Name.StartsWith("set_") ? info.Name.Substring(4) : info.Name;
                            if (!map.ContainsKey(name))
                            {
                                map[name] = new AssignmentMember(parameterInfos[0].ParameterType, methodInfo);
                                continue;
                            }
                        }
                    }
                }
            }

            return map;
        }

        public static To PopulateObject<To, From>(To to, From from)
        {
            if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

            var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

            assignmentDefinition.Populate(to, from);

            return to;
        }

        public static To PopulateWithNonDefaultValues<To, From>(To to, From from)
        {
            if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

            var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

            assignmentDefinition.PopulateWithNonDefaultValues(to, from);

            return to;
        }

        public static To PopulateFromPropertiesWithAttribute<To, From>(To to, From from,
            Type attributeType)
        {
            if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

            var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

            assignmentDefinition.PopulateFromPropertiesWithAttribute(to, from, attributeType);

            return to;
        }

        public static void SetProperty(object obj, PropertyInfo propertyInfo, object value)
        {
            if (!propertyInfo.CanWrite)
            {
                Log.WarnFormat("Attempted to set read only property '{0}'", propertyInfo.Name);
                return;
            }
#if NETFX_CORE
            var propertySetMetodInfo = propertyInfo.SetMethod;
#else
            var propertySetMetodInfo = propertyInfo.GetSetMethod();
#endif
            if (propertySetMetodInfo != null)
            {
                propertySetMetodInfo.Invoke(obj, new[] { value });
            }
        }

        public static object GetProperty(object obj, PropertyInfo propertyInfo)
        {
            if (propertyInfo == null || !propertyInfo.CanRead)
                return null;

#if NETFX_CORE
            var getMethod = propertyInfo.GetMethod;
#else
            var getMethod = propertyInfo.GetGetMethod();
#endif
            return getMethod != null ? getMethod.Invoke(obj, new object[0]) : null;
        }

        public static void SetValue(FieldInfo fieldInfo, PropertyInfo propertyInfo, object obj, object value)
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
            }
            catch (Exception ex)
            {
                var name = (fieldInfo != null) ? fieldInfo.Name : propertyInfo.Name;
                Log.DebugFormat("Could not set member: {0}. Error: {1}", name, ex.Message);
            }
        }

        public static bool IsUnsettableValue(FieldInfo fieldInfo, PropertyInfo propertyInfo)
        {
#if NETFX_CORE
            if (propertyInfo != null)
            {
                // Properties on non-user defined classes should not be set
                // Currently we define those properties as properties declared on
                // types defined in mscorlib

                if (propertyInfo.DeclaringType.AssemblyQualifiedName.Equals(typeof(object).AssemblyQualifiedName))
                {
                    return true;
                }
            }
#else
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
#endif

            return false;
        }

        public static object[] CreateDefaultValues(IEnumerable<Type> types, Dictionary<Type, int> recursionInfo)
        {
            var values = new List<object>();
            foreach (var type in types)
            {
                values.Add(CreateDefaultValue(type, recursionInfo));
            }
            return values.ToArray();
        }

        private const int MaxRecursionLevelForDefaultValues = 2; // do not nest a single type more than this deep.

        public static object CreateDefaultValue(Type type, Dictionary<Type, int> recursionInfo)
        {
            if (type == typeof(string))
            {
                return type.Name;
            }

#if NETFX_CORE
            if (type.GetTypeInfo().IsEnum)
#else
            if (type.IsEnum)
#endif
            {
#if SILVERLIGHT4 || WINDOWS_PHONE
                return Enum.ToObject(type, 0);
#else
                return Enum.GetValues(type).GetValue(0);
#endif
            }

            // If we have hit our recursion limit for this type, then return null
            int recurseLevel; // will get set to 0 if TryGetValue() fails
            recursionInfo.TryGetValue(type, out recurseLevel);
            if (recurseLevel > MaxRecursionLevelForDefaultValues) return null;

            recursionInfo[type] = recurseLevel + 1; // increase recursion level for this type
            try // use a try/finally block to make sure we decrease the recursion level for this type no matter which code path we take,
            {

                //when using KeyValuePair<TKey, TValue>, TKey must be non-default to stuff in a Dictionary
#if NETFX_CORE
                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
#else
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
#endif
                {
#if NETFX_CORE
                    var genericTypes = type.GenericTypeArguments;
#else
                    var genericTypes = type.GetGenericArguments();
#endif
                    var valueType = Activator.CreateInstance(type, CreateDefaultValue(genericTypes[0], recursionInfo), CreateDefaultValue(genericTypes[1], recursionInfo));
                    return PopulateObjectInternal(valueType, recursionInfo);
                }

#if NETFX_CORE
                if (type.GetTypeInfo().IsValueType)
#else
                if (type.IsValueType)
#endif
                {
                    return type.CreateInstance();
                }

                if (type.IsArray)
                {
                    return PopulateArray(type, recursionInfo);
                }

#if NETFX_CORE
                var constructorInfo = type.GetTypeInfo().DeclaredConstructors.First(p => p.GetParameters().Count() == 0);
#else
                var constructorInfo = type.GetConstructor(Type.EmptyTypes);
#endif
                var hasEmptyConstructor = constructorInfo != null;

                if (hasEmptyConstructor)
                {
                    var value = constructorInfo.Invoke(new object[0]);

#if !SILVERLIGHT && !MONOTOUCH && !XBOX

                    var genericCollectionType = GetGenericCollectionType(type);
                    if (genericCollectionType != null)
                    {
                        SetGenericCollection(genericCollectionType, value, recursionInfo);
                    }
#endif

                    //when the object might have nested properties such as enums with non-0 values, etc
                    return PopulateObjectInternal(value, recursionInfo);
                }
                return null;
            }
            finally
            {
                recursionInfo[type] = recurseLevel;
            }
        }

        private static Type GetGenericCollectionType(Type type)
        {
#if NETFX_CORE
            var genericCollectionType =
                type.GetTypeInfo().ImplementedInterfaces
                    .FirstOrDefault(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof (ICollection<>));
#elif WINDOWS_PHONE
            var genericCollectionType =
                type.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof (ICollection<>));
#else
            var genericCollectionType = type.FindInterfaces((t, critera) =>
                t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(ICollection<>), null).FirstOrDefault();
#endif

            return genericCollectionType;
        }

        public static void SetGenericCollection(Type realisedListType, object genericObj, Dictionary<Type, int> recursionInfo)
        {
#if NETFX_CORE
            var args = realisedListType.GenericTypeArguments;
#else
            var args = realisedListType.GetGenericArguments();
#endif

            if (args.Length != 1)
            {
                Log.ErrorFormat("Found a generic list that does not take one generic argument: {0}", realisedListType);

                return;
            }

#if NETFX_CORE
            var methodInfo = realisedListType.GetRuntimeMethods().First(p => p.Name.Equals("Add"));
#else
            var methodInfo = realisedListType.GetMethod("Add");
#endif

            if (methodInfo != null)
            {
                var argValues = CreateDefaultValues(args, recursionInfo);

                methodInfo.Invoke(genericObj, argValues);
            }
        }

        public static Array PopulateArray(Type type, Dictionary<Type, int> recursionInfo)
        {
            var elementType = type.GetElementType();
            var objArray = Array.CreateInstance(elementType, 1);
            var objElementType = CreateDefaultValue(elementType, recursionInfo);
            objArray.SetValue(objElementType, 0);

            return objArray;
        }

        //TODO: replace with InAssignableFrom
        public static bool CanCast(Type toType, Type fromType)
        {
#if NETFX_CORE
            if (toType.GetTypeInfo().IsInterface)
#else
            if (toType.IsInterface)
#endif
            {
#if NETFX_CORE
                var interfaceList = fromType.GetTypeInfo().ImplementedInterfaces.ToList();
#else
                var interfaceList = fromType.GetInterfaces().ToList();
#endif
                if (interfaceList.Contains(toType)) return true;
            }
            else
            {
                Type baseType = fromType;
                bool areSameTypes;
                do
                {
                    areSameTypes = baseType == toType;
                }
#if NETFX_CORE
                while (!areSameTypes && (baseType = fromType.GetTypeInfo().BaseType) != null);
#else
                while (!areSameTypes && (baseType = fromType.BaseType) != null);
#endif

                if (areSameTypes) return true;
            }

            return false;
        }

        public static IEnumerable<KeyValuePair<PropertyInfo, T>> GetPropertyAttributes<T>(Type fromType) where T : Attribute
        {
            var attributeType = typeof(T);
            var baseType = fromType;
            do
            {
#if NETFX_CORE
                var propertyInfos = baseType.GetRuntimeProperties();
#else
                var propertyInfos = baseType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
#endif
                foreach (var propertyInfo in propertyInfos)
                {
                    var attributes = propertyInfo.GetCustomAttributes(attributeType, true);
                    foreach (T attribute in attributes)
                    {
                        yield return new KeyValuePair<PropertyInfo, T>(propertyInfo, attribute);
                    }
                }
            }
#if NETFX_CORE
            while ((baseType = baseType.GetTypeInfo().BaseType) != null);
#else
            while ((baseType = baseType.BaseType) != null);
#endif
        }
    }
}
