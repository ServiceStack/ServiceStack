using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
#if NETCORE
using System.Globalization;
#endif

namespace ServiceStack.OrmLite.Converters
{
    public enum EnumKind
    {
        String,
        Int,
        Char,
        EnumMember,
    }
    
    public class EnumConverter : StringConverter
    {
        public EnumConverter() : base(255) {}

        static Dictionary<Type, EnumKind> enumTypeCache = new Dictionary<Type, EnumKind>();

        public static EnumKind GetEnumKind(Type enumType)
        {
            if (enumTypeCache.TryGetValue(enumType, out var enumKind))
                return enumKind;

            enumKind = IsIntEnum(enumType)
                ? EnumKind.Int
                : enumType.HasAttributeCached<EnumAsCharAttribute>()
                    ? EnumKind.Char
                    : HasEnumMembers(enumType) 
                        ? EnumKind.EnumMember
                        : EnumKind.String;

            Dictionary<Type, EnumKind> snapshot, newCache;
            do
            {
                snapshot = enumTypeCache;
                newCache = new Dictionary<Type, EnumKind>(enumTypeCache) {
                    [enumType] = enumKind
                };
            } while (!ReferenceEquals(
                System.Threading.Interlocked.CompareExchange(ref enumTypeCache, newCache, snapshot), snapshot));
            
            return enumKind;
        }
        
        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            var enumKind = GetEnumKind(fieldType);

            p.DbType = enumKind == EnumKind.Int
                ? Enum.GetUnderlyingType(fieldType) == typeof(long)
                    ? DbType.Int64
                    : DbType.Int32
                : DbType;
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var enumKind = GetEnumKind(fieldType);
            if (enumKind == EnumKind.Int)
                return this.ConvertNumber(Enum.GetUnderlyingType(fieldType), value).ToString();

            if (enumKind == EnumKind.Char)
                return DialectProvider.GetQuotedValue(ToCharValue(value).ToString());

            var isEnumFlags = fieldType.IsEnumFlags() ||
                (!fieldType.IsEnum && fieldType.IsNumericType()); //i.e. is real int && not Enum

            if (!isEnumFlags && long.TryParse(value.ToString(), out var enumValue))
                value = Enum.ToObject(fieldType, enumValue);

            var enumString = enumKind == EnumKind.EnumMember
                ? value.ToString()
                : DialectProvider.StringSerializer.SerializeToString(value);
            if (enumString == null || enumString == "null")
                enumString = value.ToString();

            return !isEnumFlags 
                ? DialectProvider.GetQuotedValue(enumString.Trim('"')) 
                : enumString;
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            var enumKind = GetEnumKind(fieldType);

            if (value.GetType().IsEnum)
            {
                if (enumKind == EnumKind.Int)
                    return Convert.ChangeType(value, Enum.GetUnderlyingType(fieldType));
                
                if (enumKind == EnumKind.Char)
                    return Convert.ChangeType(value, typeof(char));
            }

            if (enumKind == EnumKind.Char)
            {
                var charValue = ToCharValue(value);
                return charValue;
            }

            if (long.TryParse(value.ToString(), out var enumValue))
            {
                if (enumKind == EnumKind.Int)
                    return enumValue;

                value = Enum.ToObject(fieldType, enumValue);
            }

            if (enumKind == EnumKind.EnumMember) // Don't use serialized Enum Value
                return value.ToString();

            var enumString = DialectProvider.StringSerializer.SerializeToString(value);
            return enumString != null && enumString != "null"
                ? enumString.Trim('"') 
                : value.ToString();
        }

        public static char ToCharValue(object value)
        {
            var charValue = value is char c
                ? c
                : value is string s && s.Length == 1
                    ? s[0]
                    : value is int i
                        ? (char) i
                        : (char) Convert.ChangeType(value, typeof(char));
            return charValue;
        }

        //cache expensive to calculate operation
        static readonly ConcurrentDictionary<Type, bool> intEnums = new();

        public static bool IsIntEnum(Type fieldType)
        {
            var isIntEnum = intEnums.GetOrAdd(fieldType, type => 
                type.IsEnumFlags() ||
                type.HasAttributeCached<EnumAsIntAttribute>() || 
                !type.IsEnum && 
                type.IsNumericType()); //i.e. is real int && not Enum)

            return isIntEnum;
        }

        public static bool HasEnumMembers(Type enumType)
        {
            var enumMembers = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fi in enumMembers)
            {
                var enumMemberAttr = fi.FirstAttribute<EnumMemberAttribute>();
                if (enumMemberAttr?.Value != null)
                    return true;
            }
            return false;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            var enumKind = GetEnumKind(fieldType);
            
            if (enumKind == EnumKind.Char)
                return Enum.ToObject(fieldType, (int)ToCharValue(value));

            if (value is string strVal)
            {
                if (string.IsNullOrEmpty(strVal))
                    return Enum.ToObject(fieldType, 0);

#if NET6_0_OR_GREATER
                return Enum.TryParse(fieldType, strVal, ignoreCase:true, out var ret)
                    ? ret
                    : Text.TypeSerializer.DeserializeFromString(strVal, fieldType);
#else
                try
                {
                    return Enum.Parse(fieldType, strVal, ignoreCase: true);
                }
                catch (Exception)
                {
                    return Text.TypeSerializer.DeserializeFromString(strVal, fieldType);
                }
#endif
            }
            
            return Enum.ToObject(fieldType, value);
        }
    }

    public class RowVersionConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "BIGINT";

        public override DbType DbType => DbType.Int64;

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is byte[] bytes)
	        {
		        if (fieldType == typeof(byte[])) return bytes;
		        if (fieldType == typeof(ulong)) return OrmLiteUtils.ConvertToULong(bytes);

		        // an SQL row version has to be declared as either byte[] OR ulong... 
		        throw new Exception("Rowversion property must be declared as either byte[] or ulong");
	        }

            return value != null
                ? this.ConvertNumber(typeof(ulong), value)
                : null;
        }
    }

    public class ReferenceTypeConverter : StringConverter
    {
        public override string ColumnDefinition => DialectProvider.GetStringConverter().MaxColumnDefinition;

        public override string MaxColumnDefinition => DialectProvider.GetStringConverter().MaxColumnDefinition;

        public override string GetColumnDefinition(int? stringLength)
        {
            return stringLength != null
                ? base.GetColumnDefinition(stringLength)
                : MaxColumnDefinition;
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            return DialectProvider.GetQuotedValue(DialectProvider.StringSerializer.SerializeToString(value));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            //Let ADO.NET providers handle byte[]
            return fieldType == typeof(byte[]) 
                ? value 
                : DialectProvider.StringSerializer.SerializeToString(value);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is string str)
                return DialectProvider.StringSerializer.DeserializeFromString(str, fieldType);

            var convertedValue = value.ConvertTo(fieldType);
            return convertedValue;
        }
    }

    public class ValueTypeConverter : StringConverter
    {
        public override string ColumnDefinition => DialectProvider.GetStringConverter().MaxColumnDefinition;
        public override string MaxColumnDefinition => DialectProvider.GetStringConverter().MaxColumnDefinition;

        public override string GetColumnDefinition(int? stringLength)
        {
            return stringLength != null
                ? base.GetColumnDefinition(stringLength)
                : MaxColumnDefinition;
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            return DialectProvider.GetQuotedValue(DialectProvider.StringSerializer.SerializeToString(value));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            var convertedValue = DialectProvider.StringSerializer.DeserializeFromString(value.ToString(), fieldType);
            return convertedValue;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (fieldType.IsInstanceOfType(value))
                return value;

            var convertedValue = DialectProvider.StringSerializer.DeserializeFromString(value.ToString(), fieldType);
            return convertedValue;
        }
    }
}