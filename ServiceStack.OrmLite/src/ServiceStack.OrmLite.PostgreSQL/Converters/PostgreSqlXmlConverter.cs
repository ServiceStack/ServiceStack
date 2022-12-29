using System;
using System.Data;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlXmlConverter : PostgreSqlStringConverter
    {
        public override string ColumnDefinition => "XML";
        public override void InitDbParam(IDbDataParameter p, Type fieldType) => p.DbType = DbType.Xml;
        public override object ToDbValue(Type fieldType, object value) => value?.ToString();
        public override string ToQuotedString(Type fieldType, object value) => 
            base.ToQuotedString(fieldType, value.ToString());
    }
}