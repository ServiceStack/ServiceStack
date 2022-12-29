using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    /// <summary>
    /// SqlServer Database Converter for the Geometry data type
    /// https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.types.sqlgeometry.aspx
    /// </summary>
    public class SqlServerGeometryTypeConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "geometry";

        public override DbType DbType => DbType.Object;

        public override string ToQuotedString(Type fieldType, object value)
        {
            if (fieldType == typeof(SqlGeometry))
            {
                string str = null;
                if (value != null)
                {
                    var geo = (SqlGeometry)value;
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
            if (fieldType == typeof(SqlGeometry))
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
                return SqlGeometry.Null;

            if (value is SqlGeometry)
                return (SqlGeometry)value;

            if (value is string)
            {
                return SqlGeometry.Parse(value.ToString());
            }

            return base.FromDbValue(fieldType, value);
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            if (value == null || value is DBNull)
            {
                return SqlGeometry.Null;
            }

            if (value is SqlGeometry)
            {
                return value;
            }

            if (value is string)
            {
                var str = value as string;
                return SqlGeometry.Parse(str);
            }

            return base.ToDbValue(fieldType, value);
        }
    }
}
