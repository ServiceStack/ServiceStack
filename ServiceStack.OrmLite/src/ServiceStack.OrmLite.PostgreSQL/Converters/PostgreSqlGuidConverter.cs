
using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlGuidConverter : GuidConverter
    {
        public override string ColumnDefinition
        {
            get { return "UUID"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var guidValue = (Guid)value;
            return base.DialectProvider.GetQuotedValue(guidValue.ToString("N"), typeof(string));
        }
    }
}