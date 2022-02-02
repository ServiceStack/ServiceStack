using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleEnumConverter : EnumConverter
    {
        public override string ToQuotedString(Type fieldType, object value)
        {
            if (fieldType.HasAttributeCached<EnumAsIntAttribute>())
            {
                return this.ConvertNumber(fieldType.GetEnumUnderlyingType(), value).ToString();
            }

            if (value is int && !fieldType.IsEnumFlags())
            {
                value = fieldType.GetEnumName(value);
            }

            if (fieldType.IsEnum)
            {
                var enumValue = DialectProvider.StringSerializer.SerializeToString(value);
                // Oracle stores empty strings in varchar columns as null so match that behavior here
                if (enumValue == null)
                    return null;
                enumValue = DialectProvider.GetQuotedValue(enumValue.Trim('"'));
                return enumValue == "''"
                    ? "null"
                    : enumValue;
            }
            return base.ToQuotedString(fieldType, value);
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            if (value is int && !fieldType.IsEnumFlags())
            {
                value = fieldType.GetEnumName(value);
            }

            if (fieldType.HasAttributeCached<EnumAsIntAttribute>())
            {
                if (value is string)
                {
                    value = Enum.Parse(fieldType, value.ToString());
                }
                return (int) value;
            }

            var enumValue = DialectProvider.StringSerializer.SerializeToString(value);
            // Oracle stores empty strings in varchar columns as null so match that behavior here
            if (enumValue == null)
                return null;
            enumValue = enumValue.Trim('"');
            return enumValue == ""
                ? null
                : enumValue;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return base.FromDbValue(fieldType, GetDbValue(fieldType, value));
        }

        private static object GetDbValue(Type fieldType, object value)
        {
            if (!fieldType.IsEnum) return value;

            var oracleValue = value as OracleValue;

            return oracleValue == null ? value : Convert.ToInt32(value);
        }
    }
}