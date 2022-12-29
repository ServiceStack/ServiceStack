using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    /// <summary>
    /// SqlServer Database Converter for the Geometry data type
    /// https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.types.sqlgeography.aspx
    /// </summary>
    public class SqlServerGeographyTypeConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "geography";

        public override DbType DbType => DbType.Object;

        public override string ToQuotedString(Type fieldType, object value)
        {
            if (fieldType == typeof(SqlGeography))
            {
                string str = null;
                if (value != null)
                {
                    var geo = (SqlGeography)value;
                    if (!geo.IsNull)
                        str = geo.ToString();
                }
                str = (str == null) ? "null" : $"'{str}'";
                return $"CAST({str} AS {ColumnDefinition})";
            }

            return base.ToQuotedString(fieldType, value);
        }

        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            if (fieldType == typeof(SqlGeography))
            {
                var sqlParam = (SqlParameter)p;
                sqlParam.IsNullable = fieldType.IsNullableType();
                sqlParam.SqlDbType = SqlDbType.Udt;
                sqlParam.UdtTypeName = ColumnDefinition;
            }
            base.InitDbParam(p, fieldType);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value == null || value is DBNull)
                return SqlGeography.Null;

            if (value is SqlGeography)
                return (SqlGeography)value;

            if (value is string)
            {
                return SqlGeography.Parse(value.ToString());
            }

            return base.FromDbValue(fieldType, value);
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            if (value == null || value is DBNull)
            {
                return SqlGeography.Null;
            }

            if (value is SqlGeography)
            {
                return value;
            }

            if (value is string)
            {
                var str = value as string;
                return SqlGeography.Parse(str);
            }

            return base.ToDbValue(fieldType, value);
        }
    }
}
