using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerFloatConverter : FloatConverter
    {
        public override string ColumnDefinition
        {
            get { return "FLOAT"; }
        }
    }

    public class SqlServerDoubleConverter : DoubleConverter
    {
        public override string ColumnDefinition
        {
            get { return "FLOAT"; }
        }
    }

    public class SqlServerDecimalConverter : DecimalConverter
    {
        public SqlServerDecimalConverter() : base(38, 6) {}
    }
}