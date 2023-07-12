using System;
using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public class GuidConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "GUID";
        public override DbType DbType => DbType.Guid;

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is string s)
                return Guid.Parse(s);
            
            return base.FromDbValue(fieldType, value);
        }
    }
}