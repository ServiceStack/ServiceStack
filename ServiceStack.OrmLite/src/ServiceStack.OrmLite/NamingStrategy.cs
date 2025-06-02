using System.Collections.Generic;

namespace ServiceStack.OrmLite;

public class OrmLiteDefaultNamingStrategy : OrmLiteNamingStrategyBase {}
public class AliasNamingStrategy : OrmLiteNamingStrategyBase
{
    public Dictionary<string, string> TableAliases = new();
    public Dictionary<string, string> ColumnAliases = new();
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
    public override string GetTableName(string name)
    {
        return name.ToLowercaseUnderscore();
    }

    public override string GetColumnName(string name)
    {
        return name.ToLowercaseUnderscore();
    }
}

public class UpperCaseNamingStrategy : OrmLiteNamingStrategyBase
{
    public override string GetTableName(string name)
    {
        return name.ToUpper();
    }

    public override string GetColumnName(string name)
    {
        return name.ToUpper();
    }
}

public class PrefixNamingStrategy : OrmLiteNamingStrategyBase
{
    public string TablePrefix { get; set; }

    public string ColumnPrefix { get; set; }

    public override string GetTableName(string name)
    {
        return TablePrefix + name;
    }

    public override string GetColumnName(string name)
    {
        return ColumnPrefix + name;
    }

}