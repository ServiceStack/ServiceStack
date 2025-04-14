using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace ServiceStack.OrmLite.Tests;

public partial class TestConfig
{
    /// <summary>
    /// This value controls which providers are tested for all <see cref="TestFixtureOrmLiteAttribute"/> tests where dialects are not explicitly set
    /// </summary>
    public static Dialect Dialects = EnvironmentVariable("ORMLITE_DIALECT", Dialect.Sqlite);
}

[Flags]
public enum Dialect
{
    Sqlite = 1,
        
    SqlServer = 1 << 1,
    SqlServer2012 = 1 << 2,
    SqlServer2014 = 1 << 3,
    SqlServer2016 = 1 << 4,
    SqlServer2017 = 1 << 5,
    SqlServer2019 = 1 << 6,
    SqlServer2022 = 1 << 7,
        
    PostgreSql9 = 1 << 8,
    PostgreSql10 = 1 << 9,
    PostgreSql11 = 1 << 10,
        
    MySql = 1 << 11,
    MySqlConnector = 1 << 12,
        
    Oracle = 1 << 13,
        
    Firebird = 1 << 14,
    Firebird4 = 1 << 15,
        
    // any versions
    AnyPostgreSql = PostgreSql9 | PostgreSql10 | PostgreSql11,
    AnyMySql = MySql | MySqlConnector, 
    AnySqlServer = SqlServer | SqlServer2012 | SqlServer2014 | SqlServer2016 | SqlServer2017 | SqlServer2019 | SqlServer2022,
    AnyOracle = Oracle,
        
    // db groups
    BaseSupported = Sqlite | SqlServer | AnyPostgreSql | MySql | MySqlConnector,
    Supported = Sqlite | AnySqlServer | AnyMySql | AnyPostgreSql,
    Community = Firebird | Oracle,
        
    // all
    All = Supported | Community
}

public struct DialectContext
{
    public Dialect Dialect;
    public int Version;
    public DialectContext(Dialect dialect, int version)
    {
        Dialect = dialect;
        Version = version;
    }
        
    public Tuple<Dialect, int> Tuple => System.Tuple.Create(Dialect, Version);
    public static string Key(Tuple<Dialect, int> tuple) => Key(tuple.Item1, tuple.Item2);
    public static string Key(Dialect dialect, int version) => dialect + "-" + version;

    public override string ToString()
    {
        var defaultLabel = Dialect + " " + Version;
        switch (Dialect)
        {
            case Dialect.Sqlite:
                return SqliteDb.VersionString(Version); 
            case Dialect.SqlServer:
            case Dialect.SqlServer2012:
            case Dialect.SqlServer2014:
            case Dialect.SqlServer2016:
            case Dialect.SqlServer2017:
            case Dialect.SqlServer2019:
            case Dialect.SqlServer2022:
                return SqlServerDb.VersionString(Dialect, Version); 
            case Dialect.MySql:
                return MySqlDb.VersionString(Version); 
            case Dialect.PostgreSql9:
            case Dialect.PostgreSql10:
            case Dialect.PostgreSql11:
                return PostgreSqlDb.VersionString(Version); 
            case Dialect.Oracle:
                return OracleDb.VersionString(Version); 
            case Dialect.Firebird:
            case Dialect.Firebird4:
                return FirebirdDb.VersionString(Version); 
        }

        return defaultLabel;
    }

    public OrmLiteConnectionFactory NamedConnection => OrmLiteConnectionFactory.NamedConnections[Key(Dialect, Version)];
}

public static class SqliteDb
{
    public const int Memory = 1;
    public const int File = 100;
    public static int[] Versions => TestConfig.EnvironmentVariableInto("SQLITE_VERSION", [Memory]);
    public static string DefaultConnection => MemoryConnection;
    public static string MemoryConnection => TestConfig.DialectConnections[Tuple.Create(Dialect.Sqlite, Memory)];
    public static string FileConnection => TestConfig.DialectConnections[Tuple.Create(Dialect.Sqlite, File)];
    public static string VersionString(int version) => "SQLite " + (version == Memory
        ? "Memory"
        : version == File
            ? "File"
            : version.ToString());
}
public static class SqlServerDb
{
    public const int V2012 = 2012;
    public const int V2014 = 2014;
    public const int V2016 = 2016;
    public const int V2017 = 2017;
    public static int V2019 = 2019;
    public static int V2022 = 2022;
    public static int[] Versions = TestConfig.EnvironmentVariableInto("MSSQL_VERSION", [
        V2012, V2014, V2016, V2017, V2019, V2022
    ]);
    public static int[] V2012Versions = Versions.Where(x => x == V2012).ToArray();
    public static int[] V2014Versions = Versions.Where(x => x == V2014).ToArray();
    public static int[] V2016Versions = Versions.Where(x => x == V2016).ToArray();
    public static int[] V2017Versions = Versions.Where(x => x == V2017).ToArray();
    public static int[] V2019Versions = Versions.Where(x => x == V2019).ToArray();
    public static int[] V2022Versions = Versions.Where(x => x == V2022).ToArray();
        
    public static string DefaultConnection => TestConfig.DialectConnections[Tuple.Create(Dialect.SqlServer2022, V2022)];

    public static string VersionString(Dialect dialect, int version) => "SQL Server " + version;
        
    public static Dictionary<Dialect, int> CompatibilityLevels = new()
    {
        [Dialect.SqlServer2012] = 110,
        [Dialect.SqlServer2014] = 120,
        [Dialect.SqlServer2016] = 130,
        [Dialect.SqlServer2017] = 140,
        [Dialect.SqlServer2019] = 150,
        [Dialect.SqlServer2022] = 160,
    };
}
public static class MySqlDb
{
    public const int V5_5 = 55;
    public const int V10_1 = 101;
    public const int V10_2 = 102;
    public const int V10_3 = 103;
    public const int V10_4 = 104;
    public static readonly int[] Versions;
    public static int[] MySqlConnectorVersions;
    public static readonly string DefaultConnection;

    static MySqlDb()
    {
        try
        {
            // Versions = TestConfig.EnvironmentVariableInto("MYSQL_VERSION", [V5_5, V10_1, V10_2, V10_3, V10_4]);
            Versions = TestConfig.EnvironmentVariableInto("MYSQL_VERSION", [V10_4]);
            MySqlConnectorVersions = Versions.Where(x => x == V10_4).ToArray();
            DefaultConnection = TestConfig.DialectConnections[Tuple.Create(Dialect.MySql, V10_4)];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static string VersionString(int version) => "MySQL " + (version == V5_5
        ? "v5_5"
        : version == V10_1
            ? "v10_1"
            : version == V10_2
                ? "v10_2"
                : version == V10_3
                    ? "v10_3"
                    : version == V10_4
                        ? "v10_4"
                        : version.ToString());

}
public static class PostgreSqlDb
{
    public const int V9 = 9;
    public const int V10 = 10;
    public const int V11 = 11;
    public static readonly int[] Versions = TestConfig.EnvironmentVariableInto("PGSQL_VERSION", new[]{ V9, V10, V11 });
    public static readonly string DefaultConnection = TestConfig.GetConnection(Dialect.PostgreSql11, V11);
    public static int[] V9Versions = Versions.Where(x => x == V9).ToArray();
    public static int[] V10Versions = Versions.Where(x => x == V10).ToArray();
    public static int[] V11Versions = Versions.Where(x => x == V11).ToArray();
        
    public static string VersionString(int version) => "PostgreSQL " + (version == V9
        ? "v9"
        : version == V10
            ? "v10"
            : version == V11
                ? "v11"
                : version.ToString());
}
public static class OracleDb
{
    public const int V11 = 11;
    public static readonly int[] Versions = TestConfig.EnvironmentVariableInto("ORACLE_VERSION", new[]{ V11 });
    public static readonly string DefaultConnection = TestConfig.GetConnection(Dialect.Oracle, V11);
    public static string VersionString(int version) => "Oracle " + (version == V11
        ? "v11"
        : version.ToString());
}
public static class FirebirdDb
{
    public const int V3 = 3;
    public const int V4 = 4;
    public static readonly int[] Versions = TestConfig.EnvironmentVariableInto("FIREBIRD_VERSION", new[]{ V3, V4 });
    public static readonly string V3Connection = TestConfig.GetConnection(Dialect.Firebird, V3);
    public static readonly string V4Connection = TestConfig.GetConnection(Dialect.Firebird, V4);
    public static readonly string DefaultConnection = V4Connection;
    public static string VersionString(int version) => "Firebird " + (version == V3
        ? "v3"
        : version == V4
            ? "v4"
            : version.ToString());
}

/// <summary>
/// Primary config for all tests 
/// </summary>
public partial class TestConfig
{
    public const bool EnableDebugLogging = false;

    public static Dictionary<Dialect, IOrmLiteDialectProvider> DialectProviders = new()
    {
        [Dialect.Sqlite] = SqliteDialect.Provider,
        [Dialect.SqlServer] = SqlServerDialect.Provider,
        [Dialect.SqlServer2012] = SqlServer2012Dialect.Provider,
        [Dialect.SqlServer2014] = SqlServer2014Dialect.Provider,
        [Dialect.SqlServer2016] = SqlServer2016Dialect.Provider,
        [Dialect.SqlServer2017] = SqlServer2017Dialect.Provider,
        [Dialect.SqlServer2019] = SqlServer2019Dialect.Provider,
        [Dialect.SqlServer2022] = SqlServer2022Dialect.Provider,
        [Dialect.PostgreSql9] = PostgreSqlDialect.Provider,
        [Dialect.PostgreSql10] = PostgreSqlDialect.Provider,
        [Dialect.PostgreSql11] = PostgreSqlDialect.Provider,
        [Dialect.MySql] = MySqlDialect.Provider,
        [Dialect.MySqlConnector] = MySqlConnectorDialect.Provider,
        [Dialect.Oracle] = OracleDialect.Provider,
        [Dialect.Firebird] = FirebirdDialect.Provider,
        [Dialect.Firebird4] = Firebird4Dialect.Provider,
    };

    public static string GetConnection(Dialect dialect, int version)
    {
        if (DialectConnections.TryGetValue(Tuple.Create(dialect, version), out var connString))
            return connString;

        return null;
    }

    private static Dictionary<Tuple<Dialect, int>, string> dialectConnections;
    public static Dictionary<Tuple<Dialect,int>, string> DialectConnections => dialectConnections ??= LoadDialectConnections();

    private static Dictionary<Tuple<Dialect, int>, string> LoadDialectConnections()
    {
        try 
        { 
            return new Dictionary<Tuple<Dialect,int>, string> 
            {
                [Tuple.Create(Dialect.Sqlite, SqliteDb.Memory)] = EnvironmentVariable(["SQLITE_MEMORY_CONNECTION", "SQLITE_CONNECTION" ], ":memory:"),
                [Tuple.Create(Dialect.Sqlite, SqliteDb.File)] = EnvironmentVariable(["SQLITE_FILE_CONNECTION", "SQLITE_CONNECTION" ], "~/App_Data/db.sqlite".MapAbsolutePath()),

                [Tuple.Create(Dialect.SqlServer, SqlServerDb.V2012)] = EnvironmentVariable(["MSSQL2012_CONNECTION", "MSSQL_CONNECTION"], "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;"),
                [Tuple.Create(Dialect.SqlServer2017, SqlServerDb.V2017)] = EnvironmentVariable(["MSSQL2017_CONNECTION", "MSSQL_CONNECTION"], "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;"),
                [Tuple.Create(Dialect.SqlServer2019, SqlServerDb.V2019)] = EnvironmentVariable(["MSSQL2019_CONNECTION", "MSSQL_CONNECTION"], "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;"),
                [Tuple.Create(Dialect.SqlServer2022, SqlServerDb.V2022)] = EnvironmentVariable(["MSSQL2022_CONNECTION", "MSSQL_CONNECTION"], "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;"),

                [Tuple.Create(Dialect.PostgreSql9, PostgreSqlDb.V9)]  = EnvironmentVariable(["PGSQL9_CONNECTION",  "PGSQL_CONNECTION"], "Server=localhost;Port=48301;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"),
                [Tuple.Create(Dialect.PostgreSql10, PostgreSqlDb.V10)] = EnvironmentVariable(["PGSQL10_CONNECTION", "PGSQL_CONNECTION"], "Server=localhost;Port=48302;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"),
                [Tuple.Create(Dialect.PostgreSql11, PostgreSqlDb.V11)] = EnvironmentVariable(["PGSQL11_CONNECTION", "PGSQL_CONNECTION"], "Server=localhost;Port=48303;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"),
                
                [Tuple.Create(Dialect.MySql, MySqlDb.V5_5)]  = EnvironmentVariable(["MYSQL55_CONNECTION",  "MYSQL_CONNECTION"], "Server=localhost;Port=48201;Database=test;UID=root;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True;"),
                [Tuple.Create(Dialect.MySql, MySqlDb.V10_1)] = EnvironmentVariable(["MYSQL101_CONNECTION", "MYSQL_CONNECTION"], "Server=localhost;Port=48202;Database=test;UID=root;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True;"),
                [Tuple.Create(Dialect.MySql, MySqlDb.V10_2)] = EnvironmentVariable(["MYSQL102_CONNECTION", "MYSQL_CONNECTION"], "Server=localhost;Port=48203;Database=test;UID=root;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True;"),
                [Tuple.Create(Dialect.MySql, MySqlDb.V10_3)] = EnvironmentVariable(["MYSQL103_CONNECTION", "MYSQL_CONNECTION"], "Server=localhost;Port=48204;Database=test;UID=root;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True;"),
                [Tuple.Create(Dialect.MySql, MySqlDb.V10_4)] = EnvironmentVariable(["MYSQL104_CONNECTION", "MYSQL_CONNECTION"], "Server=localhost;Port=48205;Database=test;UID=root;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True;"),

                [Tuple.Create(Dialect.MySqlConnector, MySqlDb.V10_4)] = EnvironmentVariable(["MYSQL104_CONNECTION", "MYSQL_CONNECTION"], "Server=localhost;Port=48205;Database=test;UID=root;Password=p@55wOrd;SslMode=Required"),
                
                [Tuple.Create(Dialect.Oracle, OracleDb.V11)] = EnvironmentVariable(["ORACLE11_CONNECTION", "ORACLE_CONNECTION"], ""),
                
                [Tuple.Create(Dialect.Firebird, FirebirdDb.V3)] = EnvironmentVariable(["FIREBIRD3_CONNECTION", "FIREBIRD_CONNECTION"], @"User=SYSDBA;Password=masterkey;Database=/firebird/data/test.gdb;DataSource=127.0.0.1;Port=48101;Dialect=3;charset=ISO8859_1;MinPoolSize=0;MaxPoolSize=100;"),

                [Tuple.Create(Dialect.Firebird, FirebirdDb.V4)] = EnvironmentVariable(["FIREBIRD4_CONNECTION", "FIREBIRD_CONNECTION"], @"User=SYSDBA;Password=masterkey;Database=C:\tools\Firebird\data\test.fdb;DataSource=127.0.0.1;Dialect=3;charset=utf8;MinPoolSize=0;MaxPoolSize=100;"),
            };
        }
        catch (Exception e)
        {
            //Best place to Catch Exceptions is in NUnit.Framework.Internal.Builders.DefaultSideBuilder Line 115
            Console.WriteLine(e);
            throw;
        }
    }

    public static Dictionary<Dialect, int[]> DialectVersions = new()
    {
        [Dialect.Sqlite] = SqliteDb.Versions,
        [Dialect.SqlServer2012] = SqlServerDb.V2012Versions,
        [Dialect.SqlServer2014] = SqlServerDb.V2014Versions,
        [Dialect.SqlServer2016] = SqlServerDb.V2016Versions,
        [Dialect.SqlServer2017] = SqlServerDb.V2017Versions,
        [Dialect.SqlServer2019] = SqlServerDb.V2019Versions,
        [Dialect.SqlServer2022] = SqlServerDb.V2022Versions,
        [Dialect.PostgreSql9] = PostgreSqlDb.V9Versions,
        [Dialect.PostgreSql10] = PostgreSqlDb.V10Versions,
        [Dialect.PostgreSql11] = PostgreSqlDb.V11Versions,
        [Dialect.MySql] = MySqlDb.Versions,
        [Dialect.MySqlConnector] = MySqlDb.MySqlConnectorVersions,
            
        [Dialect.Oracle] = OracleDb.Versions,
        [Dialect.Firebird] = FirebirdDb.Versions,
    };
        
    public static IOrmLiteDialectProvider DefaultProvider = SqliteDialect.Provider;
    public static string DefaultConnection = SqliteDb.DefaultConnection;

    public static string EnvironmentVariable(string[] variables, string defaultValue) => 
        variables.Map(Environment.GetEnvironmentVariable).FirstOrDefault(x => x != null) ?? defaultValue;

    public static T EnvironmentVariable<T>(string variable, T defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrEmpty(value) ? defaultValue : Convert<T>(value);
    }
        
    public static T[] EnvironmentVariableInto<T>(string variable, T[] defaultValues)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrEmpty(value) ? defaultValues : value.FromJsv<T[]>();
    }
        
    private static T Convert<T>(string value)
    {
        var converter = TypeDescriptor.GetConverter(typeof(T));
        return (T)converter.ConvertFromInvariantString(value);
    }

    public static OrmLiteConnectionFactory InitDbFactory()
    {
        // init DbFactory, should be mainly ignored in tests as they should always ask for a provider specific named connection
        var dbFactory = new OrmLiteConnectionFactory(DefaultConnection, DefaultProvider);

        foreach (var dialectConnection in DialectConnections)
        {
            var dialect = dialectConnection.Key.Item1;
            if (!DialectProviders.TryGetValue(dialect, out var dialectProvider))
                continue;
                
            dbFactory.RegisterConnection(DialectContext.Key(dialectConnection.Key), dialectConnection.Value, dialectProvider);
        }

        foreach (var provider in DialectProviders)
        {
            dbFactory.RegisterDialectProvider(provider.Key.ToString(), provider.Value);
        }
            
        return dbFactory;
    }

    public static void InitDbScripts(OrmLiteConnectionFactory dbFactory)
    {
        if ((Dialects & Dialect.AnyPostgreSql) != 0)
        {
            void SetupPostgreSql(Dialect dialect, int version)
            {
                if ((Dialects & dialect) != 0)
                {
                    if (DialectConnections.TryGetValue(Tuple.Create(dialect, version), out var connString))
                    {
                        using var db = dbFactory.OpenDbConnectionString(connString + ";Timeout=10", dialect.ToString());
                        InitPostgres(dialect, db);
                    }
                }
            }
            SetupPostgreSql(Dialect.PostgreSql9, PostgreSqlDb.V9);
            SetupPostgreSql(Dialect.PostgreSql10, PostgreSqlDb.V10);
            SetupPostgreSql(Dialect.PostgreSql11, PostgreSqlDb.V11);
        }

        if ((Dialects & Dialect.MySqlConnector) != 0)
        {
            try
            {
                foreach (var version in DialectVersions[Dialect.MySqlConnector])
                {
                    using var db = dbFactory.OpenDbConnectionString(
                        DialectConnections[Tuple.Create(Dialect.MySqlConnector, version)] + ";Timeout=10",
                        Dialect.MySqlConnector.ToString());
                    InitMySqlConnector(Dialect.MySqlConnector, db);
                }
            }
            catch {}
        }

        if ((Dialects & Dialect.AnySqlServer) != 0)
        {
            void SetupSqlServer(Dialect dialect, int version)
            {
                if ((Dialects & dialect) != 0)
                {
                    if (DialectConnections.TryGetValue(Tuple.Create(dialect, version), out var connString))
                    {
                        using var db = dbFactory.OpenDbConnectionString(connString + ";Timeout=10", dialect.ToString());
                        InitSqlServer(dialect, db);
                    }
                }
            }
            SetupSqlServer(Dialect.SqlServer2012, SqlServerDb.V2012);
            SetupSqlServer(Dialect.SqlServer2014, SqlServerDb.V2014);
            SetupSqlServer(Dialect.SqlServer2016, SqlServerDb.V2016);
            SetupSqlServer(Dialect.SqlServer2017, SqlServerDb.V2017);
            SetupSqlServer(Dialect.SqlServer2019, SqlServerDb.V2019);
            SetupSqlServer(Dialect.SqlServer2022, SqlServerDb.V2022);
        }
    }
        
    public static void InitPostgres(Dialect dialect, IDbConnection db)
    {
        db.ExecuteSql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
        db.ExecuteSql("CREATE EXTENSION IF NOT EXISTS hstore");
        db.ExecuteSql("CREATE EXTENSION IF NOT EXISTS pgcrypto");
        // To avoid "Too many connections" issues during full test.
        db.ExecuteSql("ALTER SYSTEM SET max_connections = 500;");

        var dialectProvider = db.GetDialectProvider();
        var schemaName = dialectProvider.NamingStrategy.GetSchemaName("Schema");
        db.ExecuteSql($"CREATE SCHEMA IF NOT EXISTS {dialectProvider.GetQuotedName(schemaName)}");
    }

    public static void InitMySqlConnector(Dialect dialect, IDbConnection db)
    {
        db.ExecuteSql("CREATE DATABASE IF NOT EXISTS `testMySql`");
    }
        
    public static void InitSqlServer(Dialect dialect, IDbConnection db)
    {
        // Create unique db per fixture to avoid conflicts when testing dialects
        // uses COMPATIBILITY_LEVEL set to each version 

        var dbName = dialect.ToString();
        var compatibilityLevel = SqlServerDb.CompatibilityLevels[dialect];
        var createSqlDb = $@"If(db_id(N'{dbName}') IS NULL)
  BEGIN
  CREATE DATABASE {dbName};
  ALTER DATABASE {dbName} SET COMPATIBILITY_LEVEL = {compatibilityLevel};
  END";
        db.ExecuteSql(createSqlDb);
    }

}