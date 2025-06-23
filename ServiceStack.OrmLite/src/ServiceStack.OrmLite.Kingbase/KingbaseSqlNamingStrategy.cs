using Kdbndp;

namespace ServiceStack.OrmLite.Kingbase;

public class KingbaseSqlNamingStrategy(DbMode dbMode) : OrmLiteNamingStrategyBase
{
    public DbMode DbMode { get; } = dbMode;


    public override string GetSchemaName(string name) => name == null
        ? null
        : SchemaAliases.TryGetValue(name, out var alias)
            ? alias
            : name.ToLowercaseUnderscore();

    public override string GetTableName(string name)
    {
        return TableAliases.TryGetValue(name, out var alias)
            ? alias
            : name.ToLowercaseUnderscore();
    }

    public override string GetColumnName(string name)
    {
        return ColumnAliases.TryGetValue(name, out var alias)
            ? alias
            : name.ToLowercaseUnderscore();
    }
}