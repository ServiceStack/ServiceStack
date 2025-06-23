using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlFloatConverter : FloatConverter
    {
        public override string ColumnDefinition => "DOUBLE PRECISION";
    }

    public class KingbaseSqlDoubleConverter : DoubleConverter
    {
        public override string ColumnDefinition => "DOUBLE PRECISION";
    }
}