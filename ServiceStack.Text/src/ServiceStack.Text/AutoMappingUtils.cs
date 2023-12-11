// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.Text;

namespace ServiceStack;

[DataContract(Namespace = "http://schemas.servicestack.net/types")]
public class CustomHttpResult { }

/// <summary>
/// Customize ServiceStack AutoMapping Behavior
/// </summary>
public static class AutoMapping
{
    /// <summary>
    /// Register Type to Type AutoMapping converter 
    /// </summary>
    public static void RegisterConverter<From, To>(Func<From, To> converter)
    {
        JsConfig.InitStatics();
        AutoMappingUtils.converters[Tuple.Create(typeof(From), typeof(To))] = x => converter((From)x);
    }

    /// <summary>
    /// Ignore Type to Type Mapping (including collections containing them) 
    /// </summary>
    public static void IgnoreMapping<From, To>() => IgnoreMapping(typeof(From), typeof(To));

    /// <summary>
    /// Ignore Type to Type Mapping (including collections containing them) 
    /// </summary>
    public static void IgnoreMapping(Type fromType, Type toType)
    {
        JsConfig.InitStatics();
        AutoMappingUtils.ignoreMappings[Tuple.Create(fromType, toType)] = true;
    }

    public static void RegisterPopulator<Target, Source>(Action<Target, Source> populator)
    {
        JsConfig.InitStatics();
        AutoMappingUtils.populators[Tuple.Create(typeof(Target), typeof(Source))] = (a,b) => populator((Target)a,(Source)b);
    }
}

public static class AutoMappingUtils
{
    internal static readonly ConcurrentDictionary<Tuple<Type, Type>, GetMemberDelegate> converters
        = new ConcurrentDictionary<Tuple<Type, Type>, GetMemberDelegate>();

    internal static readonly ConcurrentDictionary<Tuple<Type, Type>, PopulateMemberDelegate> populators
        = new ConcurrentDictionary<Tuple<Type, Type>, PopulateMemberDelegate>();

    internal static readonly ConcurrentDictionary<Tuple<Type, Type>,bool> ignoreMappings
        = new ConcurrentDictionary<Tuple<Type, Type>,bool>();

    public static void Reset()
    {
        converters.Clear();
        populators.Clear();
        ignoreMappings.Clear();
        AssignmentDefinitionCache.Clear();
    }

    public static bool ShouldIgnoreMapping(Type fromType, Type toType) =>
        ignoreMappings.ContainsKey(Tuple.Create(fromType, toType));

    public static GetMemberDelegate GetConverter(Type fromType, Type toType)
    {
        if (converters.IsEmpty)
            return null;

        var key = Tuple.Create(fromType, toType);
        return converters.TryGetValue(key, out var converter)
            ? converter
            : null;
    }

    public static PopulateMemberDelegate GetPopulator(Type targetType, Type sourceType)
    {
        if (populators.IsEmpty)
            return null;

        var key = Tuple.Create(targetType, sourceType);
        return populators.TryGetValue(key, out var populator)
            ? populator
            : null;
    }

    public static T ConvertTo<T>(this object from, T defaultValue) =>
        from == null || (from is string s && s == string.Empty)
            ? defaultValue
            : from.ConvertTo<T>();

    public static T ConvertTo<T>(this object from) => from.ConvertTo<T>(skipConverters:false);
    public static T ConvertTo<T>(this object from, bool skipConverters)
    {
        if (from == null)
            return default;

        if (from is T t)
            return t;

        return (T)ConvertTo(from, typeof(T), skipConverters);
    }

    public static T CreateCopy<T>(this T from)
    {
        if (from == null)
            return from;
        var type = from.GetType();

        if (from is ICloneable clone)
            return (T)clone.Clone();

        if (typeof(T).IsValueType)
            return (T)ChangeValueType(from, type);

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var listResult = TranslateListWithElements.TryTranslateCollections(from.GetType(), type, from);
            return (T)listResult;
        }

        var to = type.CreateInstance<T>();
        return to.PopulateWith(from);
    }

    public static To ThenDo<To>(this To to, Action<To> fn)
    {
        fn(to);
        return to;
    }

    public static object ConvertTo(this object from, Type toType) => from.ConvertTo(toType, skipConverters: false);
    public static object ConvertTo(this object from, Type toType, bool skipConverters)
    {
        if (from == null)
            return null;

        var fromType = from.GetType();
        if (ShouldIgnoreMapping(fromType, toType))
            return null;

        if (!skipConverters)
        {
            var converter = GetConverter(fromType, toType);
            if (converter != null)
                return converter(from);
        }

        if (fromType == toType || toType == typeof(object))
            return from;

        if (fromType.IsValueType || toType.IsValueType)
            return ChangeValueType(from, toType);

        var mi = GetImplicitCastMethod(fromType, toType);
        if (mi != null)
            return mi.Invoke(null, new[] { from });

        if (from is string str)
            return TypeSerializer.DeserializeFromString(str, toType);
        if (from is ReadOnlyMemory<char> rom)
            return TypeSerializer.DeserializeFromSpan(toType, rom.Span);

        if (toType == typeof(string))
            return from.ToJsv();
            
        if (typeof(IEnumerable).IsAssignableFrom(toType))
        {
            var listResult = TryConvertCollections(fromType, toType, from);
            return listResult;
        }

        if (from is IEnumerable<KeyValuePair<string, object>> objDict)
            return objDict.FromObjectDictionary(toType);

        if (from is IEnumerable<KeyValuePair<string, string>> strDict)
            return strDict.ToObjectDictionary().FromObjectDictionary(toType);

        var to = toType.CreateInstance();
        return to.PopulateWith(from);
    }

    public static MethodInfo GetImplicitCastMethod(Type fromType, Type toType)
    {
        foreach (var mi in fromType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (mi.Name == "op_Implicit" && mi.ReturnType == toType &&
                mi.GetParameters().FirstOrDefault()?.ParameterType == fromType)
            {
                return mi;
            }
        }
        foreach (var mi in toType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (mi.Name == "op_Implicit" && mi.ReturnType == toType &&
                mi.GetParameters().FirstOrDefault()?.ParameterType == fromType)
            {
                return mi;
            }
        }
        return null;
    }
        
    public static MethodInfo GetExplicitCastMethod(Type fromType, Type toType)
    {
        foreach (var mi in toType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (mi.Name == "op_Explicit" && mi.ReturnType == toType &&
                mi.GetParameters().FirstOrDefault()?.ParameterType == fromType)
            {
                return mi;
            }
        }
        foreach (var mi in fromType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (mi.Name == "op_Explicit" && mi.ReturnType == toType &&
                mi.GetParameters().FirstOrDefault()?.ParameterType == fromType)
            {
                return mi;
            }
        }
        return null;
    }
        
    public static object ChangeValueType(object from, Type toType)
    {
        if (from == null)
            return toType.GetDefaultValue();

        var s = from as string;
        var fromType = from.GetType();
        if (!fromType.IsEnum && !toType.IsEnum)
        {
            var toString = toType == typeof(string);
            if (toType == typeof(char) && s != null)
                return s.Length > 0 ? (object) s[0] : null;
            if (toString && from is char c)
                return c.ToString();
            if (toType == typeof(TimeSpan) && from is long ticks)
                return new TimeSpan(ticks);
            if (toType == typeof(long) && from is TimeSpan time)
                return time.Ticks;

            var destNumberType = DynamicNumber.GetNumber(toType);
            if (destNumberType != null)
            {
                if (s != null && s == string.Empty)
                    return destNumberType.DefaultValue;
                    
                var value = destNumberType.ConvertFrom(from);
                if (value != null)
                {
                    return toType == typeof(char) 
                        ? value.ToString()[0] 
                        : value;
                }
            }

            if (toString)
            {
                var srcNumberType = DynamicNumber.GetNumber(from.GetType());
                if (srcNumberType != null)
                    return srcNumberType.ToString(from);
            }
        }

        var mi = GetImplicitCastMethod(fromType, toType);
        if (mi != null)
            return mi.Invoke(null, new[] { from });

        mi = GetExplicitCastMethod(fromType, toType);
        if (mi != null)
            return mi.Invoke(null, new[] { from });

        if (toType.IsEnum)
        {
            var enumValueStr = s.LastRightPart('.');
#if NET6_0_OR_GREATER
            return Enum.TryParse(toType, enumValueStr, out var ret)
                ? ret
                : TypeSerializer.DeserializeFromString(enumValueStr, toType);
#else
            try
            {
                return Enum.Parse(toType, enumValueStr);
            }
            catch (Exception)
            {
                return TypeSerializer.DeserializeFromString(enumValueStr, toType);
            }
#endif
        }

        if (s != null)
            return TypeSerializer.DeserializeFromString(s, toType);

        if (toType == typeof(string))
            return from.ToJsv();

        if (toType.HasInterface(typeof(IConvertible)))
        {
            return Convert.ChangeType(from, toType, provider: null);
        }

        var fromKvpType = fromType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));
        if (fromKvpType != null)
        {
            var fromProps = TypeProperties.Get(fromKvpType);
            var fromKey = fromProps.GetPublicGetter("Key")(from);
            var fromValue = fromProps.GetPublicGetter("Value")(from);

            var toKvpType = toType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));
            if (toKvpType != null)
            {
                    
                var toKvpArgs = toKvpType.GetGenericArguments();
                var toCtor = toKvpType.GetConstructor(toKvpArgs);
                var to = toCtor.Invoke(new[] { fromKey.ConvertTo(toKvpArgs[0]), fromValue.ConvertTo(toKvpArgs[1]) });
                return to;
            }

            if (typeof(IDictionary).IsAssignableFrom(toType))
            {
                var genericDef = toType.GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
                var toArgs = genericDef.GetGenericArguments();
                var toKeyType = toArgs[0];
                var toValueType = toArgs[1];
                  
                var to = (IDictionary)toType.CreateInstance();
                to["Key"] = fromKey.ConvertTo(toKeyType);
                to["Value"] = fromValue.ConvertTo(toValueType);
                return to;
            }
        }

        return TypeSerializer.DeserializeFromString(from.ToJsv(), toType);
    }

    public static object ChangeTo(this string strValue, Type type)
    {
        if (type.IsValueType && !type.IsEnum && type.HasInterface(typeof(IConvertible)))
        {
            try
            {
                return Convert.ChangeType(strValue, type, provider: null);
            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteError(ex);
            }
        }
        return TypeSerializer.DeserializeFromString(strValue, type);
    }

    private static readonly Dictionary<Type, List<string>> TypePropertyNamesMap = new();

    public static List<string> GetPropertyNames(this Type type)
    {
        lock (TypePropertyNamesMap)
        {
            if (!TypePropertyNamesMap.TryGetValue(type, out var propertyNames))
            {
                propertyNames = type.Properties().ToList().ConvertAll(x => x.Name);
                TypePropertyNamesMap[type] = propertyNames;
            }
            return propertyNames;
        }
    }

    public static string GetAssemblyPath(this Type source)
    {
        return PclExport.Instance.GetAssemblyPath(source);
    }

    public static bool IsDebugBuild(this Assembly assembly)
    {
        return PclExport.Instance.IsDebugBuild(assembly);
    }

    /// <summary>
    /// Populate an object with Example data.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static object PopulateWith(object obj)
    {
        if (obj == null) return null;
        var isHttpResult = obj.GetType().GetInterfaces().Any(x => x.Name == "IHttpResult"); // No coupling FTW!
        if (isHttpResult)
        {
            obj = new CustomHttpResult();
        }

        var type = obj.GetType();
        if (type.IsArray || type.IsValueType || type.IsGenericType)
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
        if (obj is string) return obj; // prevents it from dropping into the char[] Chars property.
        var type = obj.GetType();

        var members = type.GetPublicMembers();
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
        return obj;
    }

    private static Dictionary<Type, object> DefaultValueTypes = new();

    public static object GetDefaultValue(this Type type)
    {
        if (!type.IsValueType) 
            return null;

        if (DefaultValueTypes.TryGetValue(type, out var defaultValue))
            return defaultValue;

        defaultValue = Activator.CreateInstance(type);

        Dictionary<Type, object> snapshot, newCache;
        do
        {
            snapshot = DefaultValueTypes;
            newCache = new Dictionary<Type, object>(DefaultValueTypes) { [type] = defaultValue };

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref DefaultValueTypes, newCache, snapshot), snapshot));

        return defaultValue;
    }

    public static bool IsDefaultValue(object value) => IsDefaultValue(value, value?.GetType());
    public static bool IsDefaultValue(object value, Type valueType) => value == null 
                                                                       || (valueType.IsValueType && value.Equals(valueType.GetDefaultValue()));

    private static readonly ConcurrentDictionary<string, AssignmentDefinition> AssignmentDefinitionCache
        = new ConcurrentDictionary<string, AssignmentDefinition>();

    internal static AssignmentDefinition GetAssignmentDefinition(Type toType, Type fromType)
    {
        var cacheKey = CreateCacheKey(fromType, toType);

        return AssignmentDefinitionCache.GetOrAdd(cacheKey, delegate
        {
            var definition = new AssignmentDefinition
            {
                ToType = toType,
                FromType = fromType,
            };

            var readMap = GetMembers(fromType, isReadable: true);
            var writeMap = GetMembers(toType, isReadable: false);

            foreach (var assignmentMember in readMap)
            {
                if (writeMap.TryGetValue(assignmentMember.Key, out var writeMember))
                {
                    definition.AddMatch(assignmentMember.Key, assignmentMember.Value, writeMember);
                }
            }

            return definition;
        });
    }

    internal static string CreateCacheKey(Type fromType, Type toType)
    {
        var cacheKey = fromType.FullName + ">" + toType.FullName;
        return cacheKey;
    }

    private static Dictionary<string, AssignmentMember> GetMembers(Type type, bool isReadable)
    {
        var map = new Dictionary<string, AssignmentMember>();

        var members = type.GetAllPublicMembers();
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
                    if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(nonPublic:true) != null)
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
        }
        return map;
    }

    public static To PopulateWith<To, From>(this To to, From from)
    {
        if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

        var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

        assignmentDefinition.Populate(to, from);

        return to;
    }

    public static To PopulateWithNonDefaultValues<To, From>(this To to, From from)
    {
        if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

        var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

        assignmentDefinition.PopulateWithNonDefaultValues(to, from);

        return to;
    }

    public static To PopulateFromPropertiesWithAttribute<To, From>(this To to, From from,
        Type attributeType)
    {
        if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

        var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

        assignmentDefinition.PopulateFromPropertiesWithAttribute(to, from, attributeType);

        return to;
    }

    public static To PopulateFromPropertiesWithoutAttribute<To, From>(this To to, From from,
        Type attributeType)
    {
        if (Equals(to, default(To)) || Equals(from, default(From))) return default(To);

        var assignmentDefinition = GetAssignmentDefinition(to.GetType(), from.GetType());

        assignmentDefinition.PopulateFromPropertiesWithoutAttribute(to, from, attributeType);

        return to;
    }

    public static void SetProperty(this PropertyInfo propertyInfo, object obj, object value)
    {
        if (!propertyInfo.CanWrite)
        {
            Tracer.Instance.WriteWarning("Attempted to set read only property '{0}'", propertyInfo.Name);
            return;
        }

        var propertySetMethodInfo = propertyInfo.GetSetMethod(nonPublic:true);
        if (propertySetMethodInfo != null)
        {
            propertySetMethodInfo.Invoke(obj, new[] { value });
        }
    }

    public static object GetProperty(this PropertyInfo propertyInfo, object obj)
    {
        if (propertyInfo == null || !propertyInfo.CanRead)
            return null;

        var getMethod = propertyInfo.GetGetMethod(nonPublic:true);
        return getMethod != null ? getMethod.Invoke(obj, TypeConstants.EmptyObjectArray) : null;
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
                SetProperty(propertyInfo, obj, value);
            }
        }
        catch (Exception ex)
        {
            var name = (fieldInfo != null) ? fieldInfo.Name : propertyInfo.Name;
            Tracer.Instance.WriteDebug("Could not set member: {0}. Error: {1}", name, ex.Message);
        }
    }

    public static bool IsUnsettableValue(FieldInfo fieldInfo, PropertyInfo propertyInfo)
    {
        // Properties on non-user defined classes should not be set
        // Currently we define those properties as properties declared on
        // types defined in mscorlib

        if (propertyInfo != null && propertyInfo.ReflectedType != null)
        {
            return PclExport.Instance.InSameAssembly(propertyInfo.DeclaringType, typeof(object));
        }

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

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type.IsAbstract)
            return null;

        // If we have hit our recursion limit for this type, then return null
        recursionInfo.TryGetValue(type, out var recurseLevel);
        if (recurseLevel > MaxRecursionLevelForDefaultValues) return null;

        recursionInfo[type] = recurseLevel + 1; // increase recursion level for this type
        try // use a try/finally block to make sure we decrease the recursion level for this type no matter which code path we take,
        {

            //when using KeyValuePair<TKey, TValue>, TKey must be non-default to stuff in a Dictionary
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var genericTypes = type.GetGenericArguments();
                var valueType = Activator.CreateInstance(type, CreateDefaultValue(genericTypes[0], recursionInfo), CreateDefaultValue(genericTypes[1], recursionInfo));
                return PopulateObjectInternal(valueType, recursionInfo);
            }

            if (type.IsValueType)
            {
                return type.CreateInstance();
            }

            if (type.IsArray)
            {
                return PopulateArray(type, recursionInfo);
            }

            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            var hasEmptyConstructor = constructorInfo != null;

            if (hasEmptyConstructor)
            {
                var value = constructorInfo.Invoke(TypeConstants.EmptyObjectArray);

                var genericCollectionType = PclExport.Instance.GetGenericCollectionType(type);
                if (genericCollectionType != null)
                {
                    SetGenericCollection(genericCollectionType, value, recursionInfo);
                }

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

    public static void SetGenericCollection(Type realizedListType, object genericObj, Dictionary<Type, int> recursionInfo)
    {
        var args = realizedListType.GetGenericArguments();
        if (args.Length != 1)
        {
            Tracer.Instance.WriteError("Found a generic list that does not take one generic argument: {0}", realizedListType);

            return;
        }

        var methodInfo = realizedListType.GetMethodInfo("Add");
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
        if (toType.IsInterface)
        {
            var interfaceList = fromType.GetInterfaces().ToList();
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
            while (!areSameTypes && (baseType = fromType.BaseType) != null);

            if (areSameTypes) return true;
        }

        return false;
    }

    public static IEnumerable<KeyValuePair<PropertyInfo, T>> GetPropertyAttributes<T>(Type fromType)
    {
        var attributeType = typeof(T);
        var baseType = fromType;
        do
        {
            var propertyInfos = baseType.AllProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var attributes = propertyInfo.GetCustomAttributes(attributeType, true);
                foreach (var attribute in attributes)
                {
                    yield return new KeyValuePair<PropertyInfo, T>(propertyInfo, (T)(object)attribute);
                }
            }
        }
        while ((baseType = baseType.BaseType) != null);
    }
        
    public static object TryConvertCollections(Type fromType, Type toType, object fromValue)
    {
        if (fromValue == null)
            return null;
            
        if (fromValue is IEnumerable values)
        {
            var toEnumObjs = toType == typeof(IEnumerable<object>);
            if (typeof(IList).IsAssignableFrom(toType) || toEnumObjs)
            {
                var to = (IList) (toType.IsArray || toEnumObjs ? new List<object>() : toType.CreateInstance());
                var elType = toType.GetCollectionType();
                foreach (var item in values)
                {
                    to.Add(elType != null ? item.ConvertTo(elType) : item);
                }
                if (elType != null && toType.IsArray)
                {
                    var arr = Array.CreateInstance(elType, to.Count);
                    to.CopyTo(arr, 0);
                    return arr;
                }
                    
                return to;
            }
                
            if (fromValue is IDictionary d)
            {
                var obj = toType.CreateInstance();
                switch (obj)
                {
                    case List<KeyValuePair<string, string>> toList: {
                        foreach (var key in d.Keys)
                        {
                            toList.Add(new KeyValuePair<string, string>(key.ConvertTo<string>(), d[key].ConvertTo<string>()));
                        }
                        return toList;
                    }
                    case List<KeyValuePair<string, object>> toObjList: {
                        foreach (var key in d.Keys)
                        {
                            toObjList.Add(new KeyValuePair<string, object>(key.ConvertTo<string>(), d[key]));
                        }
                        return toObjList;
                    }
                    case IDictionary toDict: {
                        if (toType.GetKeyValuePairsTypes(out var toKeyType, out var toValueType))
                        {
                            foreach (var key in d.Keys)
                            {
                                var toKey = toKeyType != null
                                    ? key.ConvertTo(toKeyType)
                                    : key;
                                var toValue = d[key].ConvertTo(toValueType);
                                toDict[toKey] = toValue;
                            }
                            return toDict;
                        }
                        else
                        {
                            var from = fromValue.ToObjectDictionary();
                            var to = from.FromObjectDictionary(toType);
                            return to;
                        }
                    }
                }
            }
                
            var genericDef = fromType.GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (genericDef != null)
            {
                var genericEnumType = genericDef.GetGenericArguments()[0];
                var genericKvps = genericEnumType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));
                if (genericKvps != null)
                {
                    // Improve perf with Specialized handling of common KVP combinations 
                    var obj = toType.CreateInstance();
                    if (fromValue is IEnumerable<KeyValuePair<string, string>> sKvps)
                    {
                        switch (obj) {
                            case IDictionary toDict: {
                                toType.GetKeyValuePairsTypes(out var toKeyType, out var toValueType);
                                foreach (var entry in sKvps)
                                {
                                    var toKey = toKeyType != null
                                        ? entry.Key.ConvertTo(toKeyType)
                                        : entry.Key;
                                    toDict[toKey] = toValueType != null
                                        ? entry.Value.ConvertTo(toValueType)
                                        : entry.Value;
                                }
                                return toDict;
                            }
                            case List<KeyValuePair<string, string>> toList: {
                                foreach (var entry in sKvps)
                                {
                                    toList.Add(new KeyValuePair<string, string>(entry.Key, entry.Value));
                                }
                                return toList;
                            }
                            case List<KeyValuePair<string, object>> toObjList: {
                                foreach (var entry in sKvps)
                                {
                                    toObjList.Add(new KeyValuePair<string, object>(entry.Key, entry.Value));
                                }
                                return toObjList;
                            }
                        }
                    }
                    else if (fromValue is IEnumerable<KeyValuePair<string, object>> oKvps)
                    {
                        switch (obj) {
                            case IDictionary toDict:
                            {
                                toType.GetKeyValuePairsTypes(out var toKeyType, out var toValueType);
                                foreach (var entry in oKvps)
                                {
                                    var toKey = entry.Key.ConvertTo<string>();
                                    toDict[toKey] = toValueType != null
                                        ? entry.Value.ConvertTo(toValueType)
                                        : entry.Value;
                                }
                                return toDict;
                            }
                            case List<KeyValuePair<string, string>> toList: {
                                foreach (var entry in oKvps)
                                {
                                    toList.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.ConvertTo<string>()));
                                }
                                return toList;
                            }
                            case List<KeyValuePair<string, object>> toObjList: {
                                foreach (var entry in oKvps)
                                {
                                    toObjList.Add(new KeyValuePair<string, object>(entry.Key, entry.Value));
                                }
                                return toObjList;
                            }
                        }
                    }
                        
                        
                    // Fallback for handling any KVP combo
                    var toKvpDefType = toType.GetKeyValuePairsTypeDef();
                    switch (obj) {
                        case IDictionary toDict:
                        {
                            var keyProp = TypeProperties.Get(toKvpDefType).GetPublicGetter("Key");
                            var valueProp = TypeProperties.Get(toKvpDefType).GetPublicGetter("Value");
                                
                            foreach (var entry in values)
                            {
                                var toKvp = entry.ConvertTo(toKvpDefType);
                                var toKey = keyProp(toKvp);
                                var toValue = valueProp(toKvp);
                                toDict[toKey] = toValue;
                            }
                            return toDict;
                        }
                        case List<KeyValuePair<string, string>> toStringList: {
                            foreach (var entry in values)
                            {
                                var toEntry = entry.ConvertTo(toKvpDefType);
                                toStringList.Add((KeyValuePair<string, string>) toEntry);
                            }
                            return toStringList;
                        }
                        case List<KeyValuePair<string, object>> toObjList: {
                            foreach (var entry in values)
                            {
                                var toEntry = entry.ConvertTo(toKvpDefType);
                                toObjList.Add((KeyValuePair<string, object>) toEntry);
                            }
                            return toObjList;
                        }
                        case IEnumerable toList:
                        {
                            var addMethod = toType.GetMethod(nameof(IList.Add), new[] {toKvpDefType});
                            if (addMethod != null)
                            {
                                foreach (var entry in values)
                                {
                                    var toEntry = entry.ConvertTo(toKvpDefType);
                                    addMethod.Invoke(toList, new[] { toEntry });
                                }
                                return toList;
                            }
                            break;
                        }
                    }
                }
            }

            var fromElementType = fromType.GetCollectionType();
            var toElementType = toType.GetCollectionType();

            if (fromElementType != null && toElementType != null && fromElementType != toElementType && 
                !(typeof(IDictionary).IsAssignableFrom(fromElementType) || typeof(IDictionary).IsAssignableFrom(toElementType)))
            {
                var to = new List<object>();
                foreach (var item in values)
                {
                    var toItem = item.ConvertTo(toElementType);
                    to.Add(toItem);
                }
                var ret = TranslateListWithElements.TryTranslateCollections(to.GetType(), toType, to);
                return ret ?? fromValue;
            }
        }
        else if (fromType.IsClass && 
                 (typeof(IDictionary).IsAssignableFrom(toType) || 
                  typeof(IEnumerable<KeyValuePair<string,object>>).IsAssignableFrom(toType) || 
                  typeof(IEnumerable<KeyValuePair<string,string>>).IsAssignableFrom(toType)))
        {
            var fromDict = fromValue.ToObjectDictionary();
            return TryConvertCollections(fromType.GetType(), toType, fromDict);
        }

        var listResult = TranslateListWithElements.TryTranslateCollections(fromType, toType, fromValue);
        return listResult ?? fromValue;
    }
}

public class AssignmentEntry
{
    public string Name;
    public AssignmentMember From;
    public AssignmentMember To;
    public GetMemberDelegate GetValueFn;
    public SetMemberDelegate SetValueFn;
    public GetMemberDelegate ConvertValueFn;

    public AssignmentEntry(string name, AssignmentMember @from, AssignmentMember to)
    {
        Name = name;
        From = @from;
        To = to;

        GetValueFn = From.CreateGetter();
        SetValueFn = To.CreateSetter();
        ConvertValueFn = TypeConverter.CreateTypeConverter(From.Type, To.Type);
    }
}

public class AssignmentMember
{
    public AssignmentMember(Type type, PropertyInfo propertyInfo)
    {
        Type = type;
        PropertyInfo = propertyInfo;
    }

    public AssignmentMember(Type type, FieldInfo fieldInfo)
    {
        Type = type;
        FieldInfo = fieldInfo;
    }

    public AssignmentMember(Type type, MethodInfo methodInfo)
    {
        Type = type;
        MethodInfo = methodInfo;
    }

    public Type Type;
    public PropertyInfo PropertyInfo;
    public FieldInfo FieldInfo;
    public MethodInfo MethodInfo;

    public GetMemberDelegate CreateGetter()
    {
        if (PropertyInfo != null)
            return PropertyInfo.CreateGetter();
        if (FieldInfo != null)
            return FieldInfo.CreateGetter();
        return (GetMemberDelegate) MethodInfo?.CreateDelegate(typeof(GetMemberDelegate));
    }

    public SetMemberDelegate CreateSetter()
    {
        if (PropertyInfo != null)
            return PropertyInfo.CreateSetter();
        if (FieldInfo != null)
            return FieldInfo.CreateSetter();
        return (SetMemberDelegate) MethodInfo?.MakeDelegate(typeof(SetMemberDelegate));
    }
}

internal class AssignmentDefinition
{
    public AssignmentDefinition()
    {
        this.AssignmentMemberMap = new Dictionary<string, AssignmentEntry>();
    }

    public Type FromType { get; set; }
    public Type ToType { get; set; }

    public Dictionary<string, AssignmentEntry> AssignmentMemberMap { get; set; }

    public void AddMatch(string name, AssignmentMember readMember, AssignmentMember writeMember)
    {
        if (AutoMappingUtils.ShouldIgnoreMapping(readMember.Type,writeMember.Type))
            return;

        // Ignore mapping collections if Element Types are ignored
        if (typeof(IEnumerable).IsAssignableFrom(readMember.Type) && typeof(IEnumerable).IsAssignableFrom(writeMember.Type))
        {
            var fromGenericDef = readMember.Type.GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
            var toGenericDef = writeMember.Type.GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
            if (fromGenericDef != null && toGenericDef != null)
            {
                // Check if to/from Key or Value Types are ignored   
                var fromArgs = fromGenericDef.GetGenericArguments();
                var toArgs = toGenericDef.GetGenericArguments();
                if (AutoMappingUtils.ShouldIgnoreMapping(fromArgs[0],toArgs[0]))
                    return;
                if (AutoMappingUtils.ShouldIgnoreMapping(fromArgs[1],toArgs[1]))
                    return;
            }
            else if (readMember.Type != typeof(string) && writeMember.Type != typeof(string))
            {
                var elFromType = readMember.Type.GetCollectionType();
                var elToType = writeMember.Type.GetCollectionType();

                if (AutoMappingUtils.ShouldIgnoreMapping(elFromType,elToType))
                    return;
            }
        }

        this.AssignmentMemberMap[name] = new AssignmentEntry(name, readMember, writeMember);
    }

    public void PopulateFromPropertiesWithAttribute(object to, object from, Type attributeType)
    {
        var hasAttributePredicate = (Func<PropertyInfo, bool>)
            (x => x.AllAttributes(attributeType).Length > 0);
        Populate(to, from, hasAttributePredicate, null);
    }

    public void PopulateFromPropertiesWithoutAttribute(object to, object from, Type attributeType)
    {
        var hasAttributePredicate = (Func<PropertyInfo, bool>)
            (x => x.AllAttributes(attributeType).Length == 0);
        Populate(to, from, hasAttributePredicate, null);
    }

    public void PopulateWithNonDefaultValues(object to, object from)
    {
        var nonDefaultPredicate = (Func<object, Type, bool>)((x, t) =>
                x != null && !Equals(x, t.GetDefaultValue())
            );

        Populate(to, from, null, nonDefaultPredicate);
    }

    public void Populate(object to, object from)
    {
        Populate(to, from, null, null);
    }

    public void Populate(object to, object from,
        Func<PropertyInfo, bool> propertyInfoPredicate,
        Func<object, Type, bool> valuePredicate)
    {
        foreach (var assignmentEntryMap in AssignmentMemberMap)
        {
            var assignmentEntry = assignmentEntryMap.Value;
            var fromMember = assignmentEntry.From;
            var toMember = assignmentEntry.To;

            if (fromMember.PropertyInfo != null && propertyInfoPredicate != null)
            {
                if (!propertyInfoPredicate(fromMember.PropertyInfo)) 
                    continue;
            }

            var fromType = fromMember.Type;
            var toType = toMember.Type;
            try
            {
                    
                var fromValue = assignmentEntry.GetValueFn(from);

                if (valuePredicate != null
                    && (fromType == toType
                        || Nullable.GetUnderlyingType(fromType) == toType)) // don't short-circuit nullable <-> non-null values
                {
                    if (!valuePredicate(fromValue, fromMember.PropertyInfo.PropertyType)) 
                        continue;
                }
                    
                if (assignmentEntry.ConvertValueFn != null)
                {
                    fromValue = assignmentEntry.ConvertValueFn(fromValue);
                }

                var setterFn = assignmentEntry.SetValueFn;
                setterFn(to, fromValue);
            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteWarning("Error trying to set properties {0}.{1} > {2}.{3}:\n{4}",
                    FromType.FullName, fromType.Name,
                    ToType.FullName, toType.Name, ex);
            }
        }

        var populator = AutoMappingUtils.GetPopulator(to.GetType(), from.GetType());
        populator?.Invoke(to, from);
    }
}

public delegate object GetMemberDelegate(object instance);
public delegate object GetMemberDelegate<T>(T instance);

public delegate void PopulateMemberDelegate(object target, object source);

public delegate void SetMemberDelegate(object instance, object value);
public delegate void SetMemberDelegate<T>(T instance, object value);
public delegate void SetMemberRefDelegate(ref object instance, object propertyValue);
public delegate void SetMemberRefDelegate<T>(ref T instance, object value);

internal static class TypeConverter
{
    public static GetMemberDelegate CreateTypeConverter(Type fromType, Type toType)
    {
        if (fromType == toType)
            return null;
            
        var converter = AutoMappingUtils.GetConverter(fromType, toType);
        if (converter != null)
            return converter;

        if (fromType == typeof(string))
            return fromValue => TypeSerializer.DeserializeFromString((string)fromValue, toType);

        if (toType == typeof(string))
            return o => TypeSerializer.SerializeToString(o).StripQuotes();
            
        var underlyingToType = Nullable.GetUnderlyingType(toType) ?? toType;
        var underlyingFromType = Nullable.GetUnderlyingType(fromType) ?? fromType;

        if (underlyingToType.IsEnum)
        {
            if (underlyingFromType.IsEnum || fromType == typeof(string))
                return fromValue => Enum.Parse(underlyingToType, fromValue.ToString(), ignoreCase: true);

            if (underlyingFromType.IsIntegerType())
                return fromValue => Enum.ToObject(underlyingToType, fromValue);
        }
        else if (underlyingFromType.IsEnum)
        {
            if (underlyingToType.IsIntegerType())
                return fromValue => Convert.ChangeType(fromValue, underlyingToType, null);
        }
        else if (typeof(IEnumerable).IsAssignableFrom(fromType) && underlyingToType != typeof(string))
        {
            return fromValue => AutoMappingUtils.TryConvertCollections(fromType, underlyingToType, fromValue);
        }
        else if (underlyingToType.IsValueType)
        {
            return fromValue =>
            {
                if (fromValue == null && toType.IsNullableType())
                    return null;
                return AutoMappingUtils.ChangeValueType(fromValue, underlyingToType);
            };
        }
        else 
        {
            return fromValue =>
            {
                if (fromValue == null)
                    return fromValue;
                if (toType == typeof(string))
                    return fromValue.ToJsv();

                var toValue = toType.CreateInstance();
                toValue.PopulateWith(fromValue);
                return toValue;
            };
        }

        return null;
    }
}