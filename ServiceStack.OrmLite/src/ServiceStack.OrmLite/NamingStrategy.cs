namespace ServiceStack.OrmLite;

public class OrmLiteDefaultNamingStrategy : OrmLiteNamingStrategyBase {}
public class AliasNamingStrategy : OrmLiteNamingStrategyBase
{
    public INamingStrategy UseNamingStrategy { get; set; }

    public override string GetTableName(string name)
    {
        string alias;
        return UseNamingStrategy != null
            ? UseNamingStrategy.GetTableName(TableAliases.TryGetValue(name, out alias) ? alias : name)
            : base.GetTableName(TableAliases.TryGetValue(name, out alias) ? alias : name);
    }

    public override string GetColumnName(string name)
    {
        string alias;
        return UseNamingStrategy != null
            ? UseNamingStrategy.GetColumnName(ColumnAliases.TryGetValue(name, out alias) ? alias : name)
            : base.GetColumnName(ColumnAliases.TryGetValue(name, out alias) ? alias : name);
    }
}

public class LowercaseUnderscoreNamingStrategy : OrmLiteNamingStrategyBase
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

public class UpperCaseNamingStrategy : OrmLiteNamingStrategyBase
{
    public override string GetTableName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToUpper();

    public override string GetColumnName(string name) => ColumnAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToUpper();
}

public class PrefixNamingStrategy : OrmLiteNamingStrategyBase
{
    public string TablePrefix { get; set; }

    public string ColumnPrefix { get; set; }

    public override string GetTableName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : TablePrefix + name;

    public override string GetColumnName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : ColumnPrefix + name;
}

public class NamingStrategyExtensions
{
    
}