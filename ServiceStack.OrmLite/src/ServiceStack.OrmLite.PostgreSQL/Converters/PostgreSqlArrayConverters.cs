using System;
using System.Data;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostrgreSqlByteArrayConverter : ByteArrayConverter
    {
        public override string ColumnDefinition => "BYTEA";

        public override string ToQuotedString(Type fieldType, object value)
        {
            return "E'" + this.ToBinary(value) + "'";
        }
    }

    public class PostgreSqlStringArrayConverter : ReferenceTypeConverter
    {
        public override string ColumnDefinition => "text[]";

        public override DbType DbType => DbType.Object;

        public override string GetColumnDefinition(int? stringLength)
        {
            return stringLength != null
                ? base.GetColumnDefinition(stringLength)
                : ColumnDefinition;
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var stringArray = (string[])value;
            return this.ToArray(stringArray);
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            return fieldType == typeof(string[])
                ? value
                : DialectProvider.StringSerializer.SerializeToString(value);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return value is string strVal
                ? strVal.FromJson<string[]>()
                : value;
        }
    }

    public abstract class PostgreSqlArrayConverterBase<T> : NativeValueOrmLiteConverter
    {
        public override DbType DbType => DbType.Object;

        public override string ToQuotedString(Type fieldType, object value)
        {
            var integerArray = (T[])value;
            return this.ToArray(integerArray);
        }
    }
    
    public class PostgreSqlShortArrayConverter : PostgreSqlArrayConverterBase<short>
    {
        public override string ColumnDefinition => "short[]";
    }
    
    public class PostgreSqlIntArrayConverter : PostgreSqlArrayConverterBase<int>
    {
        public override string ColumnDefinition => "integer[]";
    }

    public class PostgreSqlLongArrayConverter : PostgreSqlArrayConverterBase<long>
    {
        public override string ColumnDefinition => "bigint[]";
    }

    public class PostgreSqlFloatArrayConverter : PostgreSqlArrayConverterBase<float>
    {
        public override string ColumnDefinition => "real[]";
    }

    public class PostgreSqlDoubleArrayConverter : PostgreSqlArrayConverterBase<double>
    {
        public override string ColumnDefinition => "double precision[]";
    }

    public class PostgreSqlDecimalArrayConverter : PostgreSqlArrayConverterBase<decimal>
    {
        public override string ColumnDefinition => "numeric[]";
    }

    public class PostgreSqlDateTimeTimeStampArrayConverter : PostgreSqlArrayConverterBase<DateTime>
    {
        public override string ColumnDefinition => "timestamp[]";
    }

    public class PostgreSqlDateTimeOffsetTimeStampTzArrayConverter : PostgreSqlArrayConverterBase<DateTimeOffset>
    {
        public override string ColumnDefinition => "timestamp with time zone[]";
    }


    public static class PostgreSqlConverterExtensions
    {
        /// <summary>
        /// based on Npgsql2's source: Npgsql2\src\NpgsqlTypes\NpgsqlTypeConverters.cs
        /// </summary>
        public static string ToBinary(this IOrmLiteConverter converter, object NativeData)
        {
            var byteArray = (byte[])NativeData;
            var res = StringBuilderCache.Allocate();
            foreach (byte b in byteArray)
                if (b >= 0x20 && b < 0x7F && b != 0x27 && b != 0x5C)
                    res.Append((char)b);
                else
                    res.Append("\\\\")
                        .Append((char)('0' + (7 & (b >> 6))))
                        .Append((char)('0' + (7 & (b >> 3))))
                        .Append((char)('0' + (7 & b)));
            return StringBuilderCache.ReturnAndFree(res);
        }

        public static string ToArray<T>(this IOrmLiteConverter converter, T[] source)
        {
            var values = StringBuilderCache.Allocate();
            foreach (var value in source)
            {
                if (values.Length > 0) values.Append(",");
                values.Append(converter.DialectProvider.GetQuotedValue(value, typeof(T)));
            }
            return "{" + StringBuilderCache.ReturnAndFree(values) + "}";
        }
    }
}