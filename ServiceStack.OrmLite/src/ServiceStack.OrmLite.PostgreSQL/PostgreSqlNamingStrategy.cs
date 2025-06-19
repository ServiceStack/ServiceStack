namespace ServiceStack.OrmLite.PostgreSQL;

public class PostgreSqlNamingStrategy : OrmLiteNamingStrategyBase
{
    public bool IgnoreAlias { get; set; } = true;
    public override string GetAlias(string name) => IgnoreAlias 
        ? name 
        : name.ToLowercaseUnderscore();
        
    public override string GetSchemaName(string name) => name == null ? null 
        : SchemaAliases.TryGetValue(name, out var alias) 
            ? alias 
            : name.ToLowercaseUnderscore();

    public override string GetTableName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToLowercaseUnderscore();

    public override string GetColumnName(string name) => ColumnAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToLowercaseUnderscore();
}