using System;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public class ColumnSchema
    {
        public string ColumnName { get; set; }
        public int ColumnOrdinal { get; set; }
        public int ColumnSize { get; set; }
        public int NumericPrecision { get; set; }
        public int NumericScale { get; set; }
        public bool IsUnique { get; set; }
        public bool IsKey { get; set; }
        public string BaseServerName { get; set; }
        public string BaseCatalogName { get; set; }
        public string BaseColumnName { get; set; }
        public string BaseSchemaName { get; set; }
        public string BaseTableName { get; set; }
        public Type DataType { get; set; }
        public bool AllowDBNull { get; set; }
        public int ProviderType { get; set; }
        public bool IsAliased { get; set; }
        public bool IsExpression { get; set; }
        public bool IsAutoIncrement { get; set; }
        public bool IsRowVersion { get; set; }
        public bool IsHidden { get; set; }
        public bool IsLong { get; set; }
        public bool IsReadOnly { get; set; }
        public Type ProviderSpecificDataType { get; set; }
        public object DefaultValue { get; set; }
        public string DataTypeName { get; set; }
        public string CollationType { get; set; }

        public override string ToString()
        {
            var sql = StringBuilderCache.Allocate();
            
            sql.Append(ColumnName.PadRight(20, ' ')).Append(' ');
            AppendDefinition(sql);

            return StringBuilderCache.ReturnAndFree(sql);
        }

        public string ColumnDefinition => 
            StringBuilderCache.ReturnAndFree(AppendDefinition(StringBuilderCache.Allocate()));

        private StringBuilder AppendDefinition(StringBuilder sql)
        {
            sql.Append(DataTypeName.ToUpper());
            if (DataType.IsRealNumberType() && NumericPrecision > 0)
            {
                sql.Append("(");
                sql.Append(NumericPrecision);
                if (NumericScale > 0)
                {
                    sql.Append(",");
                    sql.Append(NumericScale);
                }

                sql.Append(")");
            }
            else if (!DataType.IsNumericType() && ColumnSize > 0)
            {
                sql.Append("(");
                sql.Append(ColumnSize);
                sql.Append(")");
            }

            if (IsKey)
            {
                sql.Append(" PRIMARY KEY");
                if (IsAutoIncrement)
                {
                    sql.Append(" ").Append("AUTOINCREMENT");
                }
            }
            else
            {
                sql.Append(AllowDBNull ? " NULL" : " NOT NULL");

                if (IsUnique)
                {
                    sql.Append(" UNIQUE");
                }
            }

            if (DefaultValue != null)
            {
                sql.AppendFormat(" DEFAULT ({0})", DefaultValue);
            }

            return sql;
        }
    }

}