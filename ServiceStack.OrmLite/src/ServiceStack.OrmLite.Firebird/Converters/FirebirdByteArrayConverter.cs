using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdByteArrayConverter : ByteArrayConverter
    {
        public override string ToQuotedString(Type fieldType, object value)
        {
            return "x'" + BitConverter.ToString((byte[])value).Replace("-", "") + "'";
        }
    }
}