using System;
using System.Collections.Generic;
using System.Data;
#if !NETCORE
using System.Data.Common;
using System.IO;
#endif
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.Tests;

/// <summary>
/// Use this base class in conjunction with one or more <seealso cref="TestFixtureOrmLiteAttribute"/>
/// attributes to repeat tests for each db dialect.
/// Alternatively, specify <seealso cref="TestFixtureOrmLiteDialectsAttribute"/>
/// to repeat tests for each flag of <seealso cref="Dialect" /> 
/// </summary>
/// <example>
/// <code>
/// // example
/// [TestFixtureOrmLite] // all configured dialects
/// [TestFixtureOrmLiteDialects(Dialect.Supported)] // all base versions of supported dialects
/// public TestClass : OrmLiteProvidersTestBase {
///   public TestClass(DialectContext context) : base(context) {}
///
///   // Test runs once per specified providers
///   [Test]
///   public void SomeTestMethod() {
///     // current dialect 
///     var dialect = Dialect;
///     // current dialect provider instance
///     var dp = DialectProvider;
///     // get connection for provider and dialect
///     using(var db = OpenDbConnection()) {
///       // your db agnostic test code
///     }
///   }
/// }
/// </code>
/// </example>
public abstract class OrmLiteProvidersTestBase 
{
    /// <summary>
    /// The current db dialect
    /// </summary>
    public readonly Dialect Dialect;

    public readonly DialectFeatures DialectFeatures;

    /// <summary>
    /// The current DialogProvider instance
    /// </summary>
    protected IOrmLiteDialectProvider DialectProvider;
        
    // The Database Factory
    protected OrmLiteConnectionFactory DbFactory { get; set; }

    protected TestLogFactory Log => OrmLiteFixtureSetup.LogFactoryInstance; 

    /// <summary>
    /// The test logs
    /// TODO can scoped logs be created per provider?
    /// </summary>
    public IList<KeyValuePair<TestLogger.Levels, string>> Logs => TestLogger.GetLogs(); 
        
    public OrmLiteProvidersTestBase(DialectContext context)
    {
        Dialect = context.Dialect;
        DialectFeatures = new DialectFeatures(Dialect);
        DbFactory = context.NamedConnection.CreateCopy();
        DialectProvider = DbFactory.DialectProvider;

        if (OrmLiteConfig.DialectProvider == null) 
            OrmLiteConfig.DialectProvider = DialectProvider;
    }

    public virtual IDbConnection OpenDbConnection() => DbFactory.OpenDbConnection();
    public virtual Task<IDbConnection> OpenDbConnectionAsync() => DbFactory.OpenDbConnectionAsync();
}

/// <summary>
/// Holds dialect flags applicable to specific SQL language features
/// </summary>
public class DialectFeatures
{
    public readonly bool RowOffset;
    public readonly bool SchemaSupport;
        
    public DialectFeatures(Dialect dialect)
    {
        // Tag dialects with supported features and use to toggle in tests
        RowOffset = (Dialect.SqlServer2012 | Dialect.SqlServer2014 | Dialect.SqlServer2016 | Dialect.SqlServer2017 | Dialect.SqlServer2019 | Dialect.SqlServer2022).HasFlag(dialect);
        SchemaSupport = !(Dialect.Sqlite).HasFlag(dialect);
    }
}

[SetUpFixture]
public class OrmLiteFixtureSetup
{
    public static TestLogFactory LogFactoryInstance => new();
        
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // init logging, for use in tests, filter by type?
        LogManager.LogFactory = LogFactoryInstance;
            
        // setup db factories
        var dbFactory = TestConfig.InitDbFactory();
    }

}

/// <summary>
/// Repeats tests for all dialect versions from <see cref="TestConfig.Dialects"/>
/// To restrict tests to specific dialects use <see cref="TestFixtureOrmLiteDialectsAttribute"/>
/// To filter tests for specific dialects use <see cref="IgnoreDialectAttribute"/>
/// </summary>
/// <inheritdoc cref="TestFixtureOrmLiteDialectsAttribute"/>
public class TestFixtureOrmLiteAttribute : TestFixtureOrmLiteDialectsAttribute
{
    public TestFixtureOrmLiteAttribute() : base(TestConfig.Dialects)
    {
        // loads the dialects from TestConfig.DefaultDialects
        // which can be overridden using an environment variable
    }
}

/// <summary>
/// Repeats tests for all Dialect flags specified.
/// Also sets NUnit categories for each dialect flag which 
/// enables adhoc filtering of tests by using Dialect enum flag values
/// as category names in the test runner
/// </summary>
/// <example>
/// Use Dialect flags enum values to filter out one or more dialects from test runs
/// <code>
/// dotnet test --filter TestCategory=SqlServer // filters SqlServer tests for all dialects/db versions
/// dotnet test --filter TestCategory=MySql5_5 // filters MySql tests for db version v5.5 
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class TestFixtureOrmLiteDialectsAttribute : NUnitAttribute, IFixtureBuilder2
{
    private readonly Dialect dialect;
    private readonly NUnitTestFixtureBuilder _builder = new NUnitTestFixtureBuilder();
    private readonly string reason;
    private readonly int[] versions;

    public TestFixtureOrmLiteDialectsAttribute(Dialect dialect)
    {
        this.dialect = dialect;
        reason = $"Dialect not included in TestConfig.Dialect value {TestConfig.Dialects}";
    }

    public TestFixtureOrmLiteDialectsAttribute(Dialect dialect, int version) : this(dialect, new []{ version }) {}
    public TestFixtureOrmLiteDialectsAttribute(Dialect dialect, int[] versions) : this(dialect) => this.versions = versions;

    public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo)
    {
        return BuildFrom(typeInfo, null);
    }
        
    public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo, IPreFilter filter)
    {
        var fixtureData = new List<TestFixtureData>();

        void AddTestDataVersions(Dialect matchesDialect, int[] dialectVersions)
        {
            if ((matchesDialect & dialect) != matchesDialect)
                return;
                
            fixtureData.AddRange(this.versions == null
                ? dialectVersions.Map(v => new TestFixtureData(new DialectContext(matchesDialect, v)))
                : this.versions.Map(v => new TestFixtureData(new DialectContext(matchesDialect, v))));
        }

        AddTestDataVersions(Dialect.Sqlite, SqliteDb.Versions);
        AddTestDataVersions(Dialect.SqlServer, SqlServerDb.V2012Versions);
        AddTestDataVersions(Dialect.SqlServer2012, SqlServerDb.V2012Versions);
        AddTestDataVersions(Dialect.SqlServer2014, SqlServerDb.V2014Versions);
        AddTestDataVersions(Dialect.SqlServer2016, SqlServerDb.V2016Versions);
        AddTestDataVersions(Dialect.SqlServer2017, SqlServerDb.V2017Versions);
        AddTestDataVersions(Dialect.SqlServer2019, SqlServerDb.V2019Versions);
        AddTestDataVersions(Dialect.SqlServer2022, SqlServerDb.V2022Versions);
        AddTestDataVersions(Dialect.PostgreSql9, PostgreSqlDb.V9Versions);
        AddTestDataVersions(Dialect.PostgreSql10, PostgreSqlDb.V10Versions);
        AddTestDataVersions(Dialect.PostgreSql11, PostgreSqlDb.V11Versions);
        AddTestDataVersions(Dialect.MySql, MySqlDb.Versions);
        AddTestDataVersions(Dialect.MySqlConnector, MySqlDb.MySqlConnectorVersions);
        AddTestDataVersions(Dialect.Oracle, OracleDb.Versions);
        AddTestDataVersions(Dialect.Firebird, FirebirdDb.Versions);

        foreach (var data in fixtureData)
        {
            // ignore test if not in TestConfig but add as ignored to explain why
            var dialectContext = ((DialectContext)data.Arguments[0]);
            if (!TestConfig.Dialects.HasFlag(dialectContext.Dialect))
                data.Ignore(reason);

            data.Properties.Add(PropertyNames.Category, dialectContext.ToString());
            yield return _builder.BuildFrom(typeInfo, filter, data);
        }
    }
}

/// <summary>
/// Can be applied to a test to skip for specific dialects
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class IgnoreDialectAttribute : NUnitAttribute, ITestAction
{
    private readonly Dialect dialect;
    private readonly string reason;
    private readonly int[] versions;

    /// <summary>
    /// Ignore one or more specific dialects from testing
    /// </summary>
    /// <param name="dialect">The dialect flags to ignore</param>
    /// <param name="reason">The ignore reason that will be output in test results</param>
    public IgnoreDialectAttribute(Dialect dialect, string reason)
    {
        this.dialect = dialect;
        this.reason = reason;
    }

    /// <summary>
    /// Ignore one or more specific dialects from testing
    /// </summary>
    /// <param name="dialect">The dialect flags to ignore</param>
    /// <param name="versions">Specific versions you want to ignore</param>
    /// <param name="reason">The ignore reason that will be output in test results</param>
    public IgnoreDialectAttribute(Dialect dialect, int[] versions, string reason)
    {
        this.dialect = dialect;
        this.versions = versions;
        this.reason = reason;
    }

    public IgnoreDialectAttribute(Dialect dialect, int version, string reason) 
        : this(dialect, new[] {version}, reason) {}
        
    public void BeforeTest(ITest test)
    {
        // get the dialect from either the class or method parent
        // and if dialect matches, ignore test
        var testContexts = test.TestType == "TestMethod" 
            ? test.Parent.Arguments.OfType<DialectContext>() 
            : test.Arguments.OfType<DialectContext>();
            
        foreach (var testContext in testContexts)
        {
            if (this.dialect.HasFlag(testContext.Dialect) && test.RunState != RunState.NotRunnable)
            {
                if (versions == null || versions.Contains(testContext.Version))
                {
                    Assert.Ignore($"Ignoring for {testContext}: {reason}");
                }
            }
        }
    }

    public void AfterTest(ITest test)
    {
    }

    public ActionTargets Targets { get; }
}