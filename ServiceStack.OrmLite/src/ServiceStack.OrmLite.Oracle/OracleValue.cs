using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleValue : IConvertible
    {
        private readonly object _oracleValue;
        private readonly Type _oracleValueType;
        public OracleValue(object oracleValue)
        {
            _oracleValue = oracleValue;
            _oracleValueType = oracleValue.GetType();
        }

        public TypeCode GetTypeCode()
        {
            return typeof(OracleValue).GetTypeCode();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(Value);
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(Value);
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(Value);
        }

        public byte ToByte(IFormatProvider provider)
        {
            return GetMethodValue<byte>("ToByte");
        }

        public short ToInt16(IFormatProvider provider)
        {
            return GetMethodValue<short>("ToInt16");
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(Value);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return GetMethodValue<int>("ToInt32");
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(Value);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return GetMethodValue<long>("ToInt64");
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(Value);
        }

        public float ToSingle(IFormatProvider provider)
        {
            return GetMethodValue<float>("ToSingle");
        }

        public double ToDouble(IFormatProvider provider)
        {
            return GetMethodValue<double>("ToDouble");
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal) Value;
        }

        private object Value
        {
            get
            {
                return GetMethodValue<object>("get_Value");
            }
        }

        public bool IsNull()
        {
            if (_oracleValue is DBNull || _oracleValue == null)
                return true;

            return GetMethodValue<bool>("get_IsNull");
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(Value);
        }

        public string ToString(IFormatProvider provider)
        {
            return _oracleValue == null ? string.Empty : _oracleValue.ToString();
        }

        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof (DateTimeOffset?) && IsNull())
                return null;

            if (conversionType == typeof (DateTimeOffset) || conversionType == typeof (DateTimeOffset?))
                return ToDateTimeOffset();

            return Convert.ChangeType(Value, conversionType, provider);
        }

        private DateTimeOffset ToDateTimeOffset()
        {
            if (_oracleValue is DateTime)
                return new DateTimeOffset((DateTime)_oracleValue);

            var dateTime = (DateTime) Value;
            var offset = GetMethodValue<TimeSpan>("GetTimeZoneOffset");

            return new DateTimeOffset(dateTime, offset);
        }

        private readonly IDictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();
        private T GetMethodValue<T>(string methodName)
        {
            MethodInfo method;

            if (!_methodCache.TryGetValue(methodName, out method))
            {
                method = _oracleValueType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                _methodCache.Add(methodName, method);
            }

            return method != null
                ? (T)method.Invoke(_oracleValue, null)
                : (T)_oracleValue;
        }
    }
}
