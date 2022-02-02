using System;
using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public class BoolConverter : NativeValueOrmLiteConverter
    {
        public override string ColumnDefinition => "BOOL";
        public override DbType DbType => DbType.Boolean;

        //Also support coercing 0 != int as Bool
        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is bool)
                return value;

            return 0 != (long)this.ConvertNumber(typeof(long), value);
        }
    }

    public class BoolAsIntConverter : BoolConverter
    {
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
    }
}