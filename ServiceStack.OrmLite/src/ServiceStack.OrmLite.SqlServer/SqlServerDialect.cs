using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite;

public static class SqlServerDialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2012Dialect.Instance;
    public static SqlServerOrmLiteDialectProvider Instance => SqlServer2012Dialect.Instance;
}

public static class SqlServer2008Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2008OrmLiteDialectProvider.Instance;
    public static SqlServer2008OrmLiteDialectProvider Instance => SqlServer2008OrmLiteDialectProvider.Instance;
}

public static class SqlServer2012Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2012OrmLiteDialectProvider.Instance;
    public static SqlServer2012OrmLiteDialectProvider Instance => SqlServer2012OrmLiteDialectProvider.Instance;
}

public static class SqlServer2014Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2014OrmLiteDialectProvider.Instance;
    public static SqlServer2014OrmLiteDialectProvider Instance => SqlServer2014OrmLiteDialectProvider.Instance;
}

public static class SqlServer2016Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2016OrmLiteDialectProvider.Instance;
    public static SqlServer2016OrmLiteDialectProvider Instance => SqlServer2016OrmLiteDialectProvider.Instance;
}

public static class SqlServer2017Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2017OrmLiteDialectProvider.Instance;
    public static SqlServer2017OrmLiteDialectProvider Instance => SqlServer2017OrmLiteDialectProvider.Instance;
}

public static class SqlServer2019Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2019OrmLiteDialectProvider.Instance;
    public static SqlServer2019OrmLiteDialectProvider Instance => SqlServer2019OrmLiteDialectProvider.Instance;
}

public static class SqlServer2022Dialect
{
    public static IOrmLiteDialectProvider Provider => SqlServer2022OrmLiteDialectProvider.Instance;
    public static SqlServer2022OrmLiteDialectProvider Instance => SqlServer2022OrmLiteDialectProvider.Instance;
}
