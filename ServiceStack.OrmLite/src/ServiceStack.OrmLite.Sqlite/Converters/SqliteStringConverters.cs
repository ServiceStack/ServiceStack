using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Sqlite.Converters
{
    public class SqliteStringConverter : StringConverter
    {
        public override string MaxColumnDefinition => UseUnicode ? "NVARCHAR(1000000)" : "VARCHAR(1000000)";
        
        public override int MaxVarCharLength => 1000000;
    }
}