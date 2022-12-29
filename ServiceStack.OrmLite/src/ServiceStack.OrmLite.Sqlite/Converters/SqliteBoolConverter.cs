using System;
using System.Data;

namespace ServiceStack.OrmLite.Sqlite.Converters
{
    public class SqliteBoolConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "INTEGER";

        public override DbType DbType => DbType.Int32;

        public override string ToQuotedString(Type fieldType, object value)
        {
            var boolValue = (bool)value;
            return base.DialectProvider.GetQuotedValue(boolValue ? 1 : 0, typeof(int));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            return (bool)value ? 1 : 0;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is bool b)
                return b;
            
            var intVal = int.Parse(value.ToString());
            return intVal != 0;
        }
    }
}