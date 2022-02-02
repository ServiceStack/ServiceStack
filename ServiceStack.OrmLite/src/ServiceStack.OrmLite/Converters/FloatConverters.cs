using System;
using System.Data;
using System.Globalization;

namespace ServiceStack.OrmLite.Converters
{
    public class FloatConverter : NativeValueOrmLiteConverter
    {
        public override string ColumnDefinition => "DOUBLE";
        public override DbType DbType => DbType.Single;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var typeCode = fieldType.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Single:
                    return Convert.ToSingle(value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Double:
                    return Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(value).ToString(CultureInfo.InvariantCulture);
            }

            return base.ToQuotedString(fieldType, value);
        }
    }

    public class DoubleConverter : FloatConverter
    {
        public override DbType DbType => DbType.Double;
    }

    public class DecimalConverter : FloatConverter, IHasColumnDefinitionPrecision
    {
        public int Precision { get; set; }
        public int Scale { get; set; }

        public DecimalConverter()
            : this(18, 12) {}

        public DecimalConverter(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }

        public override string ColumnDefinition => GetColumnDefinition(Precision, Scale);

        public override DbType DbType => DbType.Decimal;

        public virtual string GetColumnDefinition(int? precision, int? scale)
        {
            return $"DECIMAL({precision.GetValueOrDefault(Precision)},{scale.GetValueOrDefault(Scale)})";
        }
    }
}