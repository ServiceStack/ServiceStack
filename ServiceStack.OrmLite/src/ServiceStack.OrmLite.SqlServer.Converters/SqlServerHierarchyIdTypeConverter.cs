using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    /// <summary>
    /// SqlServer Database Converter for the HierarchyId data type
    /// https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.types.sqlhierarchyid.aspx
    /// </summary>
    public class SqlServerHierarchyIdTypeConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "hierarchyId";

        public override DbType DbType => DbType.Object;

        public override string ToQuotedString(Type fieldType, object value)
        {
            if (fieldType == typeof(SqlHierarchyId))
            {
                string str = null;
                if (value != null)
                {
                    var hierarchyId = (SqlHierarchyId)value;
                    if (!hierarchyId.IsNull)
                        str = hierarchyId.ToString();
                }
                str = (str == null) ? "null" : $"'{str}'";
                return $"CAST({str} AS {ColumnDefinition})";
            }

            return base.ToQuotedString(fieldType, value);
        }

        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            if (fieldType == typeof(SqlHierarchyId))
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
                return SqlHierarchyId.Null;

            if (value is SqlHierarchyId)
                return (SqlHierarchyId)value;

            if (value is string)
            {
                return SqlHierarchyId.Parse(value.ToString());
            }

            return base.FromDbValue(fieldType, value);
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            if (value == null || value is DBNull)
            {
                return SqlHierarchyId.Null;
            }

            if (value is SqlHierarchyId)
            {
                return value;
            }

            if (value is string)
            {
                var str = value as string;
                return SqlHierarchyId.Parse(str);
            }

            return base.ToDbValue(fieldType, value);
        }
    }
}
