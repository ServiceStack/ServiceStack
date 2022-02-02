using System;
using System.Globalization;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdFloatConverter : FloatConverter
    {
        public override string ColumnDefinition
        {
            get { return "FLOAT"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var s = ((float)value).ToString(CultureInfo.InvariantCulture);
            if (s.Length > 20) s = s.Substring(0, 20);
            return "'" + s + "'"; // when quoted exception is more clear!
        }
    }

    public class FirebirdDoubleConverter : FloatConverter
    {
        public override string ColumnDefinition
        {
            get { return "FLOAT"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var s = ((double)value).ToString(CultureInfo.InvariantCulture);
            if (s.Length > 20) s = s.Substring(0, 20);
            return "'" + s + "'"; // when quoted exception is more clear!
        }
    }

    public class FirebirdDecimalConverter : DecimalConverter
    {
        public override string ToQuotedString(Type fieldType, object value)
        {
            var s = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            if (s.Length > 20) s = s.Substring(0, 20);
            return "'" + s + "'"; // when quoted exception is more clear!
        }
    }
}