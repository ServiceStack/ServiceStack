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

    public class FirebirdDoubleConverter : DoubleConverter
    {
        // Firebird FLOAT is single-precision (32-bit) -> loses precision for a .NET double
        // (e.g. 3.14159 -> 3.14159011...). DOUBLE PRECISION is the correct 64-bit mapping.
        // (Also derive from DoubleConverter, not FloatConverter, so DbType/handling are correct.)
        public override string ColumnDefinition
        {
            get { return "DOUBLE PRECISION"; }
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