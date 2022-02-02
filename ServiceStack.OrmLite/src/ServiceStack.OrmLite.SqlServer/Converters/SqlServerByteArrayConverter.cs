using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerByteArrayConverter : ByteArrayConverter
    {
        public override string ColumnDefinition => "VARBINARY(MAX)";

        public override string ToQuotedString(Type fieldType, object value)
        {
            return "0x" + BitConverter.ToString((byte[])value).Replace("-", "");
        }
    }
}