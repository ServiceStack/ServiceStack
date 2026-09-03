using System;
using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public class ByteArrayConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "BLOB";
        public override DbType DbType => DbType.Binary;

        public override string ToQuotedString(Type fieldType, object value)
        {
            if (value is byte[] bytes)
                return "0x" + BitConverter.ToString(bytes).Replace("-", "");
            return base.ToQuotedString(fieldType, value);
        }
    }
}