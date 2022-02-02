using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class PlatformExtensions
    {
        [Obsolete("Use type.IsInterface")]
        public static bool IsInterface(this Type type) => type.IsInterface;

        [Obsolete("Use type.IsArray")]
        public static bool IsArray(this Type type) => type.IsArray;

        [Obsolete("Use type.IsValueType")]
        public static bool IsValueType(this Type type) => type.IsValueType;

        [Obsolete("Use type.IsGenericType")]
        public static bool IsGeneric(this Type type) => type.IsGenericType;

        [Obsolete("Use type.BaseType")]
        public static Type BaseType(this Type type) => type.BaseType;

        [Obsolete("Use pi.ReflectedType")]
        public static Type ReflectedType(this PropertyInfo pi) => pi.ReflectedType;

        [Obsolete("Use fi.ReflectedType")]
        public static Type ReflectedType(this FieldInfo fi) => fi.ReflectedType;

        [Obsolete("Use type.GetGenericTypeDefinition()")]
        public static Type GenericTypeDefinition(this Type type) => type.GetGenericTypeDefinition();

        [Obsolete("Use type.GetInterfaces()")]
        public static Type[] GetTypeInterfaces(this Type type) => type.GetInterfaces();

        [Obsolete("Use type.GetGenericArguments()")]
        public static Type[] GetTypeGenericArguments(this Type type) => type.GetGenericArguments();

        [Obsolete("Use type.GetConstructor(Type.EmptyTypes)")]
        public static ConstructorInfo GetEmptyConstructor(this Type type) => type.GetConstructor(Type.EmptyTypes);

        [Obsolete("Use type.GetConstructors()")]
        public static IEnumerable<ConstructorInfo> GetAllConstructors(this Type type) => type.GetConstructors();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PropertyInfo[] GetTypesPublicProperties(this Type subType)
        {
            return subType.GetProperties(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public |
                BindingFlags.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PropertyInfo[] GetTypesProperties(this Type subType)
        {
            return subType.GetProperties(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        }

        [Obsolete("Use type.Assembly")]
        public static Assembly GetAssembly(this Type type) => type.Assembly;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo[] Fields(this Type type)
        {
            return type.GetFields(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyInfo[] Properties(this Type type)
        {
            return type.GetProperties(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo[] GetAllFields(this Type type) => type.IsInterface ? TypeConstants.EmptyFieldInfoArray : type.Fields();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo[] GetPublicFields(this Type type) => type.IsInterface 
            ? TypeConstants.EmptyFieldInfoArray 
            : type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberInfo[] GetPublicMembers(this Type type) => type.GetMembers(BindingFlags.Public | BindingFlags.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberInfo[] GetAllPublicMembers(this Type type) => 
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetStaticMethod(this Type type, string methodName) => 
            type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetInstanceMethod(this Type type, string methodName) => 
            type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [Obsolete("Use fn.Method")]
        public static MethodInfo Method(this Delegate fn) => fn.Method;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this Type type) => type.AllAttributes().Any(x => x.GetType() == typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeOf<T>(this Type type) => type.AllAttributes().Any(x => x is T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this PropertyInfo pi) => pi.AllAttributes().Any(x => x.GetType() == typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeOf<T>(this PropertyInfo pi) => pi.AllAttributesLazy().Any(x => x is T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this FieldInfo fi) => fi.AllAttributes().Any(x => x.GetType() == typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeOf<T>(this FieldInfo fi) => fi.AllAttributes().Any(x => x is T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this MethodInfo mi) => mi.AllAttributes().Any(x => x.GetType() == typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeOf<T>(this MethodInfo mi) => mi.AllAttributes().Any(x => x is T);

        private static readonly ConcurrentDictionary<Tuple<MemberInfo,Type>, bool> hasAttributeCache = new();
        public static bool HasAttributeCached<T>(this MemberInfo memberInfo)
        {
            var key = new Tuple<MemberInfo,Type>(memberInfo, typeof(T));
            if (hasAttributeCache.TryGetValue(key , out var hasAttr))
                return hasAttr;

            hasAttr = memberInfo is Type t 
                ? t.AllAttributes().Any(x => x.GetType() == typeof(T))
                : memberInfo is PropertyInfo pi
                ? pi.AllAttributes().Any(x => x.GetType() == typeof(T))
                : memberInfo is FieldInfo fi
                ? fi.AllAttributes().Any(x => x.GetType() == typeof(T))
                : memberInfo is MethodInfo mi
                ? mi.AllAttributes().Any(x => x.GetType() == typeof(T))
                : throw new NotSupportedException(memberInfo.GetType().Name);

            hasAttributeCache[key] = hasAttr;

            return hasAttr;
        }

        private static readonly ConcurrentDictionary<Tuple<MemberInfo,Type>, bool> hasAttributeOfCache = new ConcurrentDictionary<Tuple<MemberInfo,Type>, bool>();
        public static bool HasAttributeOfCached<T>(this MemberInfo memberInfo)
        {
            var key = new Tuple<MemberInfo,Type>(memberInfo, typeof(T));
            if (hasAttributeOfCache.TryGetValue(key , out var hasAttr))
                return hasAttr;

            hasAttr = memberInfo is Type t 
                ? t.AllAttributes().Any(x => x is T)
                : memberInfo is PropertyInfo pi
                ? pi.AllAttributes().Any(x => x is T)
                : memberInfo is FieldInfo fi
                ? fi.AllAttributes().Any(x => x is T)
                : memberInfo is MethodInfo mi
                ? mi.AllAttributes().Any(x => x is T)
                : throw new NotSupportedException(memberInfo.GetType().Name);

            hasAttributeOfCache[key] = hasAttr;

            return hasAttr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeNamed(this Type type, string name)
        {
            var normalizedAttr = name.Replace("Attribute", "").ToLower();
            return type.AllAttributes().Any(x => x.GetType().Name.Replace("Attribute", "").ToLower() == normalizedAttr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeNamed(this PropertyInfo pi, string name)
        {
            var normalizedAttr = name.Replace("Attribute", "").ToLower();
            return pi.AllAttributesLazy().Any(x => x.GetType().Name.Replace("Attribute", "").ToLower() == normalizedAttr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeNamed(this FieldInfo fi, string name)
        {
            var normalizedAttr = name.Replace("Attribute", "").ToLower();
            return fi.AllAttributes().Any(x => x.GetType().Name.Replace("Attribute", "").ToLower() == normalizedAttr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttributeNamed(this MemberInfo mi, string name)
        {
            var normalizedAttr = name.Replace("Attribute", "").ToLower();
            return mi.AllAttributes().Any(x => x.GetType().Name.Replace("Attribute", "").ToLower() == normalizedAttr);
        }

        const string DataContract = "DataContractAttribute";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDto(this Type type)
        {
            if (type == null)
                return false;

            return !Env.IsMono
                ? type.HasAttribute<DataContractAttribute>()
                : type.GetCustomAttributes(true).Any(x => x.GetType().Name == DataContract);
        }

        [Obsolete("Use pi.GetGetMethod(nonPublic)")]
        public static MethodInfo PropertyGetMethod(this PropertyInfo pi, bool nonPublic = false) => pi.GetGetMethod(nonPublic);

        [Obsolete("Use type.GetInterfaces()")]
        public static Type[] Interfaces(this Type type) => type.GetInterfaces();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyInfo[] AllProperties(this Type type) => 
            type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        //Should only register Runtime Attributes on StartUp, So using non-ThreadSafe Dictionary is OK
        static Dictionary<string, List<Attribute>> propertyAttributesMap = new();

        static Dictionary<Type, List<Attribute>> typeAttributesMap = new();

        public static void ClearRuntimeAttributes()
        {
            propertyAttributesMap = new Dictionary<string, List<Attribute>>();
            typeAttributesMap = new Dictionary<Type, List<Attribute>>();
        }

        internal static string UniqueKey(this PropertyInfo pi)
        {
            if (pi.DeclaringType == null)
                throw new ArgumentException("Property '{0}' has no DeclaringType".Fmt(pi.Name));

            return pi.DeclaringType.Namespace + "." + pi.DeclaringType.Name + "." + pi.Name;
        }

        public static Type AddAttributes(this Type type, params Attribute[] attrs)
        {
            if (!typeAttributesMap.TryGetValue(type, out var typeAttrs))
                typeAttributesMap[type] = typeAttrs = new List<Attribute>();

            typeAttrs.AddRange(attrs);
            return type;
        }

        /// <summary>
        /// Add a Property attribute at runtime. 
        /// <para>Not threadsafe, should only add attributes on Startup.</para>
        /// </summary>
        public static PropertyInfo AddAttributes(this PropertyInfo propertyInfo, params Attribute[] attrs)
        {
            var key = propertyInfo.UniqueKey();
            if (!propertyAttributesMap.TryGetValue(key, out var propertyAttrs))
                propertyAttributesMap[key] = propertyAttrs = new List<Attribute>();

            propertyAttrs.AddRange(attrs);

            return propertyInfo;
        }

        /// <summary>
        /// Add a Property attribute at runtime. 
        /// <para>Not threadsafe, should only add attributes on Startup.</para>
        /// </summary>
        public static PropertyInfo ReplaceAttribute(this PropertyInfo propertyInfo, Attribute attr)
        {
            var key = propertyInfo.UniqueKey();

            if (!propertyAttributesMap.TryGetValue(key, out var propertyAttrs))
                propertyAttributesMap[key] = propertyAttrs = new List<Attribute>();

            propertyAttrs.RemoveAll(x => x.GetType() == attr.GetType());

            propertyAttrs.Add(attr);

            return propertyInfo;
        }

        public static List<TAttr> GetAttributes<TAttr>(this PropertyInfo propertyInfo)
        {
            return !propertyAttributesMap.TryGetValue(propertyInfo.UniqueKey(), out var propertyAttrs)
                ? new List<TAttr>()
                : propertyAttrs.OfType<TAttr>().ToList();
        }

        public static List<Attribute> GetAttributes(this PropertyInfo propertyInfo)
        {
            return !propertyAttributesMap.TryGetValue(propertyInfo.UniqueKey(), out var propertyAttrs)
                ? new List<Attribute>()
                : propertyAttrs.ToList();
        }

        public static List<Attribute> GetAttributes(this PropertyInfo propertyInfo, Type attrType)
        {
            return !propertyAttributesMap.TryGetValue(propertyInfo.UniqueKey(), out var propertyAttrs)
                ? new List<Attribute>()
                : propertyAttrs.Where(x => attrType.IsInstanceOf(x.GetType())).ToList();
        }

        public static object[] AllAttributes(this PropertyInfo propertyInfo)
        {
            var attrs = propertyInfo.GetCustomAttributes(true);
            var runtimeAttrs = propertyInfo.GetAttributes();
            if (runtimeAttrs.Count == 0)
                return attrs;

            runtimeAttrs.AddRange(attrs.Cast<Attribute>());
            return runtimeAttrs.Cast<object>().ToArray();
        }

        public static IEnumerable<object> AllAttributesLazy(this PropertyInfo propertyInfo)
        {
            var attrs = propertyInfo.GetCustomAttributes(true);
            var runtimeAttrs = propertyInfo.GetAttributes();
            foreach (var attr in runtimeAttrs)
            {
                yield return attr;
            }
            foreach (var attr in attrs)
            {
                yield return attr;
            }
        }

        public static object[] AllAttributes(this PropertyInfo propertyInfo, Type attrType)
        {
            var attrs = propertyInfo.GetCustomAttributes(attrType, true);
            var runtimeAttrs = propertyInfo.GetAttributes(attrType);
            if (runtimeAttrs.Count == 0)
                return attrs;

            runtimeAttrs.AddRange(attrs.Cast<Attribute>());
            return runtimeAttrs.Cast<object>().ToArray();
        }

        public static IEnumerable<object> AllAttributesLazy(this PropertyInfo propertyInfo, Type attrType)
        {
            foreach (var attr in propertyInfo.GetAttributes(attrType))
            {
                yield return attr;
            }
            foreach (var attr in propertyInfo.GetCustomAttributes(attrType, true))
            {
                yield return attr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this ParameterInfo paramInfo) => paramInfo.GetCustomAttributes(true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this FieldInfo fieldInfo) => fieldInfo.GetCustomAttributes(true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this MemberInfo memberInfo) => memberInfo.GetCustomAttributes(true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this ParameterInfo paramInfo, Type attrType) => paramInfo.GetCustomAttributes(attrType, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this MemberInfo memberInfo, Type attrType)
        {
            var prop = memberInfo as PropertyInfo;
            return prop != null 
                ? prop.AllAttributes(attrType) 
                : memberInfo.GetCustomAttributes(attrType, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this FieldInfo fieldInfo, Type attrType) => fieldInfo.GetCustomAttributes(attrType, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this Type type) => type.GetCustomAttributes(true).Union(type.GetRuntimeAttributes()).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<object> AllAttributesLazy(this Type type)
        {
            foreach (var attr in type.GetRuntimeAttributes())
                yield return attr;
            foreach (var attr in type.GetCustomAttributes(true))
                yield return attr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this Type type, Type attrType) => 
            type.GetCustomAttributes(attrType, true).Union(type.GetRuntimeAttributes(attrType)).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object[] AllAttributes(this Assembly assembly) => assembly.GetCustomAttributes(true).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr[] AllAttributes<TAttr>(this ParameterInfo pi) => pi.AllAttributes(typeof(TAttr)).Cast<TAttr>().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr[] AllAttributes<TAttr>(this MemberInfo mi) => mi.AllAttributes(typeof(TAttr)).Cast<TAttr>().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr[] AllAttributes<TAttr>(this FieldInfo fi) => fi.AllAttributes(typeof(TAttr)).Cast<TAttr>().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr[] AllAttributes<TAttr>(this PropertyInfo pi) => pi.AllAttributes(typeof(TAttr)).Cast<TAttr>().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TAttr> AllAttributesLazy<TAttr>(this PropertyInfo pi) => pi.AllAttributesLazy(typeof(TAttr)).Cast<TAttr>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static IEnumerable<T> GetRuntimeAttributes<T>(this Type type) => typeAttributesMap.TryGetValue(type, out var attrs)
            ? attrs.OfType<T>()
            : new List<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static IEnumerable<Attribute> GetRuntimeAttributes(this Type type, Type attrType = null) => typeAttributesMap.TryGetValue(type, out var attrs)
            ? attrs.Where(x => attrType == null || attrType.IsInstanceOf(x.GetType()))
            : new List<Attribute>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr[] AllAttributes<TAttr>(this Type type)
        {
            return type.GetCustomAttributes(typeof(TAttr), true)
                .OfType<TAttr>()
                .Union(type.GetRuntimeAttributes<TAttr>())
                .ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TAttr> AllAttributesLazy<TAttr>(this Type type)
        {
            foreach (var attr in type.GetCustomAttributes(typeof(TAttr), true).OfType<TAttr>())
            {
                yield return attr;
            }
            foreach (var attr in type.GetRuntimeAttributes<TAttr>())
            {
                yield return attr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttr FirstAttribute<TAttr>(this Type type) where TAttr : class
        {
            return (TAttr)type.GetCustomAttributes(typeof(TAttr), true)
                       .FirstOrDefault()
                   ?? type.GetRuntimeAttributes<TAttr>().FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttribute FirstAttribute<TAttribute>(this MemberInfo memberInfo)
        {
            return memberInfo.AllAttributes<TAttribute>().FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttribute FirstAttribute<TAttribute>(this ParameterInfo paramInfo)
        {
            return paramInfo.AllAttributes<TAttribute>().FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo) => 
            propertyInfo.AllAttributesLazy<TAttribute>().FirstOrDefault();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type FirstGenericTypeDefinition(this Type type)
        {
            var genericType = type.FirstGenericType();
            return genericType != null ? genericType.GetGenericTypeDefinition() : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDynamic(this Assembly assembly) => ReflectionOptimizer.Instance.IsDynamic(assembly);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetStaticMethod(this Type type, string methodName, Type[] types)
        {
            return types == null
                ? type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, types, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetMethodInfo(this Type type, string methodName, Type[] types = null) => types == null
            ? type.GetMethod(methodName)
            : type.GetMethod(methodName, types);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object InvokeMethod(this Delegate fn, object instance, object[] parameters = null) => 
            fn.Method.Invoke(instance, parameters ?? new object[] { });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo GetPublicStaticField(this Type type, string fieldName) => 
            type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate MakeDelegate(this MethodInfo mi, Type delegateType, bool throwOnBindFailure = true) => 
            Delegate.CreateDelegate(delegateType, mi, throwOnBindFailure);

        [Obsolete("Use type.GetGenericArguments()")]
        public static Type[] GenericTypeArguments(this Type type) => type.GetGenericArguments();

        [Obsolete("Use type.GetConstructors()")]
        public static ConstructorInfo[] DeclaredConstructors(this Type type) => type.GetConstructors();

        [Obsolete("Use type.IsAssignableFrom(fromType)")]
        public static bool AssignableFrom(this Type type, Type fromType) => type.IsAssignableFrom(fromType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStandardClass(this Type type) => type.IsClass && !type.IsAbstract && !type.IsInterface;

        [Obsolete("Use type.IsAbstract")]
        public static bool IsAbstract(this Type type) => type.IsAbstract;

        [Obsolete("Use type.GetProperty(propertyName)")]
        public static PropertyInfo GetPropertyInfo(this Type type, string propertyName) => type.GetProperty(propertyName);

        [Obsolete("Use type.GetField(fieldName)")]
        public static FieldInfo GetFieldInfo(this Type type, string fieldName) => type.GetField(fieldName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldInfo[] GetWritableFields(this Type type) => 
            type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

        [Obsolete("Use pi.GetSetMethod(nonPublic)")]
        public static MethodInfo SetMethod(this PropertyInfo pi, bool nonPublic = true) => 
            pi.GetSetMethod(nonPublic);

        [Obsolete("Use pi.GetGetMethod(nonPublic)")]
        public static MethodInfo GetMethodInfo(this PropertyInfo pi, bool nonPublic = true) => 
            pi.GetGetMethod(nonPublic);

        [Obsolete("Use type.IsInstanceOfType(instance)")]
        public static bool InstanceOfType(this Type type, object instance) => type.IsInstanceOfType(instance);

        [Obsolete("Use type.IsAssignableFrom(fromType)")]
        public static bool IsAssignableFromType(this Type type, Type fromType) => type.IsAssignableFrom(fromType);

        [Obsolete("Use type.IsClass")]
        public static bool IsClass(this Type type) => type.IsClass;

        [Obsolete("Use type.IsEnum")]
        public static bool IsEnum(this Type type) => type.IsEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnumFlags(this Type type) => type.IsEnum && type.FirstAttribute<FlagsAttribute>() != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnderlyingEnum(this Type type) => type.IsEnum || type.UnderlyingSystemType.IsEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo[] GetInstanceMethods(this Type type) => 
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [Obsolete("Use type.GetMethods()")]
        public static MethodInfo[] GetMethodInfos(this Type type) => type.GetMethods();

        [Obsolete("Use type.GetProperties()")]
        public static PropertyInfo[] GetPropertyInfos(this Type type) => type.GetProperties();

        [Obsolete("Use type.IsGenericTypeDefinition")]
        public static bool IsGenericTypeDefinition(this Type type) => type.IsGenericTypeDefinition;

        [Obsolete("Use type.IsGenericType")]
        public static bool IsGenericType(this Type type) => type.IsGenericType;

        [Obsolete("Use type.ContainsGenericParameters")]
        public static bool ContainsGenericParameters(this Type type) => type.ContainsGenericParameters;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDeclaringTypeName(this Type type)
        {
            if (type.DeclaringType != null)
                return type.DeclaringType.Name;

            if (type.ReflectedType != null)
                return type.ReflectedType.Name;

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDeclaringTypeName(this MemberInfo mi)
        {
            if (mi.DeclaringType != null)
                return mi.DeclaringType.Name;

            return mi.ReflectedType.Name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, methodInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType, object target)
        {
            return Delegate.CreateDelegate(delegateType, target, methodInfo);
        }

        [Obsolete("Use type.GetElementType()")]
        public static Type ElementType(this Type type) => type.GetElementType();

        public static Type GetCollectionType(this Type type)
        {
            return type.GetElementType()
               ?? type.GetGenericArguments().LastOrDefault() //new[] { str }.Select(x => new Type()) => WhereSelectArrayIterator<string,Type>
               ?? (type.BaseType != null && type.BaseType != typeof(object) ? type.BaseType.GetCollectionType() : null); //e.g. ArrayOfString : List<string>
        }

        static Dictionary<string, Type> GenericTypeCache = new Dictionary<string, Type>();

        public static Type GetCachedGenericType(this Type type, params Type[] argTypes)
        {
            if (!type.IsGenericTypeDefinition)
                throw new ArgumentException(type.FullName + " is not a Generic Type Definition");

            if (argTypes == null)
                argTypes = TypeConstants.EmptyTypeArray;

            var sb = StringBuilderThreadStatic.Allocate()
                .Append(type.FullName);

            foreach (var argType in argTypes)
            {
                sb.Append('|')
                    .Append(argType.FullName);
            }

            var key = StringBuilderThreadStatic.ReturnAndFree(sb);

            if (GenericTypeCache.TryGetValue(key, out var genericType))
                return genericType;

            genericType = type.MakeGenericType(argTypes);

            Dictionary<string, Type> snapshot, newCache;
            do
            {
                snapshot = GenericTypeCache;
                newCache = new Dictionary<string, Type>(GenericTypeCache) {
                    [key] = genericType
                };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref GenericTypeCache, newCache, snapshot), snapshot));

            return genericType;
        }

        private static readonly ConcurrentDictionary<Type, ObjectDictionaryDefinition> toObjectMapCache =
            new ConcurrentDictionary<Type, ObjectDictionaryDefinition>();

        internal class ObjectDictionaryDefinition
        {
            public Type Type;
            public readonly List<ObjectDictionaryFieldDefinition> Fields = new List<ObjectDictionaryFieldDefinition>();
            public readonly Dictionary<string, ObjectDictionaryFieldDefinition> FieldsMap = new Dictionary<string, ObjectDictionaryFieldDefinition>();

            public void Add(string name, ObjectDictionaryFieldDefinition fieldDef)
            {
                Fields.Add(fieldDef);
                FieldsMap[name] = fieldDef;
            }
        }

        internal class ObjectDictionaryFieldDefinition
        {
            public string Name;
            public Type Type;

            public GetMemberDelegate GetValueFn;
            public SetMemberDelegate SetValueFn;

            public Type ConvertType;
            public GetMemberDelegate ConvertValueFn;

            public void SetValue(object instance, object value)
            {
                if (SetValueFn == null)
                    return;

                if (Type != typeof(object))
                {
                    if (value is IEnumerable<KeyValuePair<string, object>> dictionary)
                    {
                        value = dictionary.FromObjectDictionary(Type);
                    }

                    if (!Type.IsInstanceOfType(value))
                    {
                        lock (this)
                        {
                            //Only caches object converter used on first use
                            if (ConvertType == null)
                            {
                                ConvertType = value.GetType();
                                ConvertValueFn = TypeConverter.CreateTypeConverter(ConvertType, Type);
                            }
                        }

                        if (ConvertType.IsInstanceOfType(value))
                        {
                            value = ConvertValueFn(value);
                        }
                        else
                        {
                            var tempConvertFn = TypeConverter.CreateTypeConverter(value.GetType(), Type);
                            value = tempConvertFn(value);
                        }
                    }
                }

                SetValueFn(instance, value);
            }
        }

        private static Dictionary<string, object> ConvertToDictionary<T>(
                            IEnumerable<KeyValuePair<string, T>> collection,
                            Func<string, object, object> mapper = null)
        {
            if (mapper != null)
            {
                return mapper.MapToDictionary(collection);
            }

            var to = new Dictionary<string, object>();
            foreach (var entry in collection)
            {
                string key = entry.Key;
                object value = entry.Value;
                to[key] = value;
            }
            return to;
        }

        private static Dictionary<string, object> MapToDictionary<T>(
            this Func<string, object, object> mapper, 
            IEnumerable<KeyValuePair<string, T>> collection)
        {
            return collection.ToDictionary(
                                    pair => pair.Key,
                                    pair => mapper(pair.Key, pair.Value));
        }

        public static Dictionary<string, object> ToObjectDictionary(this object obj)
        {
            return ToObjectDictionary(obj, null);
        }

        public static Dictionary<string, object> ToObjectDictionary(
                            this object obj,
                            Func<string, object, object> mapper)
        {
            if (obj == null)
                return null;

            if (obj is Dictionary<string, object> alreadyDict)
            {
                if (mapper != null)
                    return mapper.MapToDictionary(alreadyDict);
                return alreadyDict;
            }

            if (obj is IDictionary<string, object> interfaceDict)
            {
                if (mapper != null)
                    return mapper.MapToDictionary(interfaceDict);
                return new Dictionary<string, object>(interfaceDict);
            }

            var to = new Dictionary<string, object>();
            if (obj is Dictionary<string, string> stringDict)
            {
                return ConvertToDictionary(stringDict, mapper);
            }

            if (obj is IDictionary d)
            {
                foreach (var key in d.Keys)
                {
                    string k = key.ToString();
                    object v = d[key];
                    v = mapper?.Invoke(k, v) ?? v;
                    to[k] = v;
                }
                return to;
            }

            if (obj is NameValueCollection nvc)
            {
                for (var i = 0; i < nvc.Count; i++)
                {
                    string k = nvc.GetKey(i);
                    object v = nvc.Get(i);
                    v = mapper?.Invoke(k, v) ?? v;
                    to[k] = v;
                }
                return to;
            }

            if (obj is IEnumerable<KeyValuePair<string, object>> objKvps)
            {
                return ConvertToDictionary(objKvps, mapper);
            }
            if (obj is IEnumerable<KeyValuePair<string, string>> strKvps)
            {
                return ConvertToDictionary(strKvps, mapper);
            }

            var type = obj.GetType();
            if (type.GetKeyValuePairsTypes(out var keyType, out var valueType, out var kvpType) && obj is IEnumerable e)
            {
                var keyGetter = TypeProperties.Get(kvpType).GetPublicGetter("Key");
                var valueGetter = TypeProperties.Get(kvpType).GetPublicGetter("Value");
                
                foreach (var entry in e)
                {
                    var key = keyGetter(entry);
                    var value = valueGetter(entry);
                    string k = key.ConvertTo<string>();
                    value = mapper?.Invoke(k, value) ?? value;
                    to[k] = value;
                }
                return to;
            }


            if (obj is KeyValuePair<string, object> objKvp)
            {
                string kk = nameof(objKvp.Key);
                object kv = objKvp.Key;
                kv = mapper?.Invoke(kk, kv) ?? kv;

                string vk = nameof(objKvp.Value);
                object vv = objKvp.Value;
                vv = mapper?.Invoke(vk, vv) ?? vv;

                return new Dictionary<string, object> 
                {
                    [kk] = kv, 
                    [vk] = vv
                };
            }
            if (obj is KeyValuePair<string, string> strKvp)
            {
                string kk = nameof(objKvp.Key);
                object kv = strKvp.Key;
                kv = mapper?.Invoke(kk, kv) ?? kv;

                string vk = nameof(strKvp.Value);
                object vv = strKvp.Value;
                vv = mapper?.Invoke(vk, vv) ?? vv;

                return new Dictionary<string, object>
                {
                    [kk] = kv,
                    [vk] = vv
                };
            }
            if (type.GetKeyValuePairTypes(out _, out var _))
            {
                string kk = "Key";
                object kv = TypeProperties.Get(type).GetPublicGetter("Key")(obj).ConvertTo<string>();
                kv = mapper?.Invoke(kk, kv) ?? kv;

                string vk = "Value";
                object vv = TypeProperties.Get(type).GetPublicGetter("Value")(obj);
                vv = mapper?.Invoke(vk, vv) ?? vv;

                return new Dictionary<string, object>
                {
                    [kk] = kv,
                    [vk] = vv
                };
            }

            if (!toObjectMapCache.TryGetValue(type, out var def))
                toObjectMapCache[type] = def = CreateObjectDictionaryDefinition(type);

            foreach (var fieldDef in def.Fields)
            {
                string k = fieldDef.Name;
                object v = fieldDef.GetValueFn(obj);
                v = mapper?.Invoke(k, v) ?? v;
                to[k] = v;
            }

            return to;
        }

        public static Type GetKeyValuePairsTypeDef(this Type dictType)
        {
            //matches IDictionary<,>, IReadOnlyDictionary<,>, List<KeyValuePair<string, object>>
            var genericDef = dictType.GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (genericDef == null) 
                return null;
            
            var genericEnumType = genericDef.GetGenericArguments()[0];
            return GetKeyValuePairTypeDef(genericEnumType);
        }

        public static Type GetKeyValuePairTypeDef(this Type genericEnumType) => genericEnumType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));

        public static bool GetKeyValuePairsTypes(this Type dictType, out Type keyType, out Type valueType) =>
            dictType.GetKeyValuePairsTypes(out keyType, out valueType, out _);
        
        public static bool GetKeyValuePairsTypes(this Type dictType, out Type keyType, out Type valueType, out Type kvpType)
        {
            //matches IDictionary<,>, IReadOnlyDictionary<,>, List<KeyValuePair<string, object>>
            var genericDef = dictType.GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (genericDef != null)
            {
                kvpType = genericDef.GetGenericArguments()[0];
                if (GetKeyValuePairTypes(kvpType, out keyType, out valueType)) 
                    return true;
            }
            kvpType = keyType = valueType = null;
            return false;
        }

        public static bool GetKeyValuePairTypes(this Type kvpType, out Type keyType, out Type valueType)
        {
            var genericKvps = kvpType.GetTypeWithGenericTypeDefinitionOf(typeof(KeyValuePair<,>));
            if (genericKvps != null)
            {
                var genericArgs = kvpType.GetGenericArguments();
                keyType = genericArgs[0];
                valueType = genericArgs[1];
                return true;
            }

            keyType = valueType = null;
            return false;
        }

        public static object FromObjectDictionary(this IEnumerable<KeyValuePair<string, object>> values, Type type)
        {
            if (values == null)
                return null;
            
            var alreadyDict = typeof(IEnumerable<KeyValuePair<string, object>>).IsAssignableFrom(type);
            if (alreadyDict)
                return values;

            var to = type.CreateInstance();
            if (to is IDictionary d)
            {
                if (type.GetKeyValuePairsTypes(out var toKeyType, out var toValueType))
                {
                    foreach (var entry in values)
                    {
                        var toKey = entry.Key.ConvertTo(toKeyType);
                        var toValue = entry.Value.ConvertTo(toValueType);
                        d[toKey] = toValue;
                    }
                }
                else
                {
                    foreach (var entry in values)
                    {
                        d[entry.Key] = entry.Value;
                    }
                }
            }
            else
            {
                PopulateInstanceInternal(values, to, type);
            }
            
            return to;
        }

        public static void PopulateInstance(this IEnumerable<KeyValuePair<string, object>> values, object instance)
        {
            if (values == null || instance == null)
                return;
            
            PopulateInstanceInternal(values, instance, instance.GetType());
        }

        private static void PopulateInstanceInternal(IEnumerable<KeyValuePair<string, object>> values, object to, Type type)
        {
            if (!toObjectMapCache.TryGetValue(type, out var def))
                toObjectMapCache[type] = def = CreateObjectDictionaryDefinition(type);

            foreach (var entry in values)
            {
                if (!def.FieldsMap.TryGetValue(entry.Key, out var fieldDef) &&
                    !def.FieldsMap.TryGetValue(entry.Key.ToPascalCase(), out fieldDef)
                    || entry.Value == null 
                    || entry.Value == DBNull.Value)
                    continue;

                fieldDef.SetValue(to, entry.Value);
            }
        }

        public static void PopulateInstance(this IEnumerable<KeyValuePair<string, string>> values, object instance)
        {
            if (values == null || instance == null)
                return;
            
            PopulateInstanceInternal(values, instance, instance.GetType());
        }

        private static void PopulateInstanceInternal(IEnumerable<KeyValuePair<string, string>> values, object to, Type type)
        {
            if (!toObjectMapCache.TryGetValue(type, out var def))
                toObjectMapCache[type] = def = CreateObjectDictionaryDefinition(type);

            foreach (var entry in values)
            {
                if (!def.FieldsMap.TryGetValue(entry.Key, out var fieldDef) &&
                    !def.FieldsMap.TryGetValue(entry.Key.ToPascalCase(), out fieldDef)
                    || entry.Value == null)
                    continue;
                
                fieldDef.SetValue(to, entry.Value);
            }
        }

        public static T FromObjectDictionary<T>(this IEnumerable<KeyValuePair<string, object>> values)
        {
            return (T)values.FromObjectDictionary(typeof(T));
        }

        private static ObjectDictionaryDefinition CreateObjectDictionaryDefinition(Type type)
        {
            var def = new ObjectDictionaryDefinition
            {
                Type = type,
            };

            foreach (var pi in type.GetSerializableProperties())
            {
                def.Add(pi.Name, new ObjectDictionaryFieldDefinition
                {
                    Name = pi.Name,
                    Type = pi.PropertyType,
                    GetValueFn = pi.CreateGetter(),
                    SetValueFn = pi.CreateSetter(),
                });
            }

            if (JsConfig.IncludePublicFields)
            {
                foreach (var fi in type.GetSerializableFields())
                {
                    def.Add(fi.Name, new ObjectDictionaryFieldDefinition
                    {
                        Name = fi.Name,
                        Type = fi.FieldType,
                        GetValueFn = fi.CreateGetter(),
                        SetValueFn = fi.CreateSetter(),
                    });
                }
            }
            return def;
        }

        public static Dictionary<string, object> ToSafePartialObjectDictionary<T>(this T instance)
        {
            var to = new Dictionary<string, object>();
            var propValues = instance.ToObjectDictionary();
            if (propValues != null)
            {
                foreach (var entry in propValues)
                {
                    var valueType = entry.Value?.GetType();

                    try
                    {
                        if (valueType == null || !valueType.IsClass || valueType == typeof(string))
                        {
                            to[entry.Key] = entry.Value;
                        }
                        else if (!TypeSerializer.HasCircularReferences(entry.Value))
                        {
                            if (entry.Value is IEnumerable enumerable)
                            {
                                to[entry.Key] = entry.Value;
                            }
                            else
                            {
                                to[entry.Key] = entry.Value.ToSafePartialObjectDictionary();
                            }
                        }
                        else
                        {
                            to[entry.Key] = entry.Value.ToString();
                        }

                    }
                    catch (Exception ignore)
                    {
                        Tracer.Instance.WriteDebug($"Could not retrieve value from '{valueType?.GetType().Name}': ${ignore.Message}");
                    }
                }
            }
            return to;
        }

        public static Dictionary<string, object> MergeIntoObjectDictionary(this object obj, params object[] sources)
        {
            var to = obj.ToObjectDictionary();
            foreach (var source in sources)
            foreach (var entry in source.ToObjectDictionary())
            {
                to[entry.Key] = entry.Value;
            }
            return to;
        }

        public static Dictionary<string, string> ToStringDictionary(this IEnumerable<KeyValuePair<string, object>> from) => ToStringDictionary(from, null);

        public static Dictionary<string, string> ToStringDictionary(this IEnumerable<KeyValuePair<string, object>> from, IEqualityComparer<string> comparer)
        {
            var to = comparer != null
                ? new Dictionary<string, string>(comparer)
                : new Dictionary<string, string>();

            if (from != null)
            {
                foreach (var entry in from)
                {
                    to[entry.Key] = entry.Value is string s
                        ? s
                        : entry.Value.ConvertTo<string>();
                }
            }

            return to;
        }
    }
}