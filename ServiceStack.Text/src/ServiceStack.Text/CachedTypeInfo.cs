using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace ServiceStack.Text
{
    public class CachedTypeInfo
    {
        static Dictionary<Type, CachedTypeInfo> CacheMap = new Dictionary<Type, CachedTypeInfo>();

        public static CachedTypeInfo Get(Type type)
        {
            if (CacheMap.TryGetValue(type, out var value))
                return value;

            var instance = new CachedTypeInfo(type);

            Dictionary<Type, CachedTypeInfo> snapshot, newCache;
            do
            {
                snapshot = CacheMap;
                newCache = new Dictionary<Type, CachedTypeInfo>(CacheMap)
                {
                    [type] = instance
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref CacheMap, newCache, snapshot), snapshot));

            return instance;
        }

        public CachedTypeInfo(Type type)
        {
            EnumInfo = EnumInfo.GetEnumInfo(type);
        }

        public EnumInfo EnumInfo { get; }
    }
    
    public class EnumInfo
    {
        public static EnumInfo GetEnumInfo(Type type)
        {
            if (type.IsEnum)
                return new EnumInfo(type);
            
            var nullableType = Nullable.GetUnderlyingType(type);           
            if (nullableType?.IsEnum == true)
                return new EnumInfo(nullableType);

            return null;
        }

        private readonly Type enumType;
        private EnumInfo(Type enumType)
        {
            this.enumType = enumType;
            enumMemberReverseLookup = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var enumMembers = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fi in enumMembers)
            {
                var enumValue = fi.GetValue(null);
                var strEnum = fi.Name;
                var enumMemberAttr = fi.FirstAttribute<EnumMemberAttribute>();
                if (enumMemberAttr?.Value != null)
                {
                    if (enumMemberValues == null)
                    {
                        enumMemberValues = new Dictionary<object, string>();
                    }
                    enumMemberValues[enumValue] = enumMemberAttr.Value;
                    enumMemberReverseLookup[enumMemberAttr.Value] = enumValue;
                }
                else
                {
                    enumMemberReverseLookup[strEnum] = enumValue;
                }
            }
            isEnumFlag = enumType.IsEnumFlags();
        }

        private readonly bool isEnumFlag;
        private readonly Dictionary<object, string> enumMemberValues;
        private readonly Dictionary<string, object> enumMemberReverseLookup;

        public object GetSerializedValue(object enumValue)
        {
            if (enumMemberValues != null && enumMemberValues.TryGetValue(enumValue, out var memberValue))
                return memberValue;
            if (isEnumFlag || JsConfig.TreatEnumAsInteger)
                return enumValue;
            return enumValue.ToString();
        }

        public object Parse(string serializedValue)
        {
            if (enumMemberReverseLookup.TryGetValue(serializedValue, out var enumValue))
                return enumValue;
            
            return Enum.Parse(enumType, serializedValue, ignoreCase: true); //Also parses quoted int values, e.g. "1"
        }
    }
    
}