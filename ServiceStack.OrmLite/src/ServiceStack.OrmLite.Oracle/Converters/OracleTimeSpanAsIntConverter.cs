using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleTimeSpanAsIntConverter : TimeSpanAsIntConverter
    {
        public override string ColumnDefinition
        {
            get { return "NUMERIC(18)"; }
        }
    }
}