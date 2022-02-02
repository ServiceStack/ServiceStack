using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerBoolConverter : BoolConverter
    {
        public override string ColumnDefinition
        {
            get { return "BIT"; }
        }

        public override DbType DbType
        {
            get { return DbType.Boolean; }
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            if (value is bool)
                return value;

            return 0 != (long)this.ConvertNumber(typeof(long), value);
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var boolValue = (bool)value;
            return base.DialectProvider.GetQuotedValue(boolValue ? 1 : 0, typeof(int));
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is bool)
                return value;

            return 0 != (long)this.ConvertNumber(typeof(long), value);
        }
    }
}