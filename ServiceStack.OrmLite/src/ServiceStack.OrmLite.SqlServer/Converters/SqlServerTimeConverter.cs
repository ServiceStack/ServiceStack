using System;
using System.Data;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerTimeConverter : OrmLiteConverter
    {
        private static readonly DateTime timeSpanOffset = new DateTime(1900, 01, 01);

        public int? Precision { get; set; }

        public override string ColumnDefinition => Precision != null
            ? $"TIME({Precision.Value})"
            : "TIME";

        public override DbType DbType => DbType.DateTime;

        public override object ToDbValue(Type fieldType, object value)
        {
            var timeSpan = (TimeSpan)value;
            return timeSpanOffset + timeSpan;
        }
    }
}