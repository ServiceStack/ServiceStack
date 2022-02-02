using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ServiceStack.Text;

namespace ServiceStack
{
    public class FieldAccessor
    {
        public FieldAccessor(
            FieldInfo fieldInfo,
            GetMemberDelegate publicGetter,
            SetMemberDelegate publicSetter,
            SetMemberRefDelegate publicSetterRef)
        {
            FieldInfo = fieldInfo;
            PublicGetter = publicGetter;
            PublicSetter = publicSetter;
            PublicSetterRef = publicSetterRef;
        }

        public FieldInfo FieldInfo { get; }

        public GetMemberDelegate PublicGetter { get; }

        public SetMemberDelegate PublicSetter { get; }

        public SetMemberRefDelegate PublicSetterRef { get; }
    }

    public class TypeFields<T> : TypeFields
    {
        public static readonly TypeFields<T> Instance = new TypeFields<T>();

        static TypeFields()
        {
            Instance.Type = typeof(T);
            Instance.PublicFieldInfos = typeof(T).GetPublicFields();
            foreach (var fi in Instance.PublicFieldInfos)
            {
                try
                {
                    var fnRef = fi.SetExpressionRef<T>();
                    Instance.FieldsMap[fi.Name] = new FieldAccessor(
                        fi,
                        ReflectionOptimizer.Instance.CreateGetter(fi),
                        ReflectionOptimizer.Instance.CreateSetter(fi),
                        delegate (ref object instance, object arg)
                        {
                            var valueInstance = (T)instance;
                            fnRef(ref valueInstance, arg);
                            instance = valueInstance;
                        });
                }
                catch (Exception ex)
                {
                    Tracer.Instance.WriteError(ex);
                }
            }
        }

        public new static FieldAccessor GetAccessor(string propertyName)
        {
            return Instance.FieldsMap.TryGetValue(propertyName, out FieldAccessor info)
                ? info
                : null;
        }
    }

    public abstract class TypeFields
    {
        static Dictionary<Type, TypeFields> CacheMap = new Dictionary<Type, TypeFields>();

        public static Type FactoryType = typeof(TypeFields<>);

        public static TypeFields Get(Type type)
        {
            if (CacheMap.TryGetValue(type, out TypeFields value))
                return value;

            var genericType = FactoryType.MakeGenericType(type);
            var instanceFi = genericType.GetPublicStaticField("Instance");
            var instance = (TypeFields)instanceFi.GetValue(null);

            Dictionary<Type, TypeFields> snapshot, newCache;
            do
            {
                snapshot = CacheMap;
                newCache = new Dictionary<Type, TypeFields>(CacheMap)
                {
                    [type] = instance
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref CacheMap, newCache, snapshot), snapshot));

            return instance;
        }

        public FieldAccessor GetAccessor(string propertyName)
        {
            return FieldsMap.TryGetValue(propertyName, out FieldAccessor info)
                ? info
                : null;
        }

        public Type Type { get; protected set; }

        public readonly Dictionary<string, FieldAccessor> FieldsMap =
            new Dictionary<string, FieldAccessor>(PclExport.Instance.InvariantComparerIgnoreCase);

        public FieldInfo[] PublicFieldInfos { get; protected set; }

        public virtual FieldInfo GetPublicField(string name)
        {
            foreach (var fi in PublicFieldInfos)
            {
                if (fi.Name == name)
                    return fi;
            }
            return null;
        }

        public virtual GetMemberDelegate GetPublicGetter(FieldInfo fi) => GetPublicGetter(fi?.Name);

        public virtual GetMemberDelegate GetPublicGetter(string name)
        {
            if (name == null)
                return null;

            return FieldsMap.TryGetValue(name, out FieldAccessor info)
                ? info.PublicGetter
                : null;
        }

        public virtual SetMemberDelegate GetPublicSetter(FieldInfo fi) => GetPublicSetter(fi?.Name);

        public virtual SetMemberDelegate GetPublicSetter(string name)
        {
            if (name == null)
                return null;

            return FieldsMap.TryGetValue(name, out FieldAccessor info)
                ? info.PublicSetter
                : null;
        }

        public virtual SetMemberRefDelegate GetPublicSetterRef(string name)
        {
            if (name == null)
                return null;

            return FieldsMap.TryGetValue(name, out FieldAccessor info)
                ? info.PublicSetterRef
                : null;
        }
    }

    public static class FieldInvoker
    {
        public static GetMemberDelegate CreateGetter(this FieldInfo fieldInfo) =>
            ReflectionOptimizer.Instance.CreateGetter(fieldInfo);

        public static GetMemberDelegate<T> CreateGetter<T>(this FieldInfo fieldInfo) =>
            ReflectionOptimizer.Instance.CreateGetter<T>(fieldInfo);

        public static SetMemberDelegate CreateSetter(this FieldInfo fieldInfo) =>
            ReflectionOptimizer.Instance.CreateSetter(fieldInfo);

        public static SetMemberDelegate<T> CreateSetter<T>(this FieldInfo fieldInfo) =>
            ReflectionOptimizer.Instance.CreateSetter<T>(fieldInfo);

        public static SetMemberRefDelegate<T> SetExpressionRef<T>(this FieldInfo fieldInfo) =>
            ReflectionOptimizer.Instance.CreateSetterRef<T>(fieldInfo);
    }
}
