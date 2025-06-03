using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Sqlite.Converters;

public class SqliteByteArrayConverter : ByteArrayConverter
{
    public override string ToQuotedString(Type fieldType, object value)
    {
        return "x'" + BitConverter.ToString((byte[])value).Replace("-", "") + "'";
    }
}