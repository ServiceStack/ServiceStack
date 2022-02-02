# OrmLite Testing

The following describes some typical testing scenarios

## Docker Db instances

For integration testing, there is a docker-compose file that can spin up all versions of supported providers. See the readme in /src/docker for more details

## Test Basics

To create tests to run against one or more providers, inherit from `OrmLiteProvidersTestBase` 
and use the `TestFixtureOrmLiteAttribute` class.

```csharp
[TestFixtureOrmLite]
public class DbFeatures1 : OrmLiteProvidersTestBase 
{
    // Required Ctor, Dialects will be injected by attribute
    public DbFeatures1(Dialect dialect) : base(dialect)
    {
    }
    
    // Tests can be repeated for one or more providers
    [Test]
    public void Test1() 
    {
        // Current test dialect
        var dialect = base.Dialect;
    
        // current test dialectprovider
        var dialectProvider = base.DialectProvider;
        
        // current DbFactory
        var dbFactory = base.DbFactory;

        // open the correct db connection based on dialect 
        using(var db = OpenConnection())
        {
            // db agnostic tests
        }
    }
}
```

By default, the tests will run against in-memory instances of Sqlite.
This is set in `TestConfig.DefaultDialects` and can be overridden either by changing
the value assigned or by setting an Environment Variable named `DefaultDialects` to one of the Dialect enum values.

The accepted values come from the enum `Dialect` which is injected into the class constructor.

## Targeting specific providers

To run tests against specific providers, use `TestFixtureOrmLiteDialectsAttribute`.

```csharp
[TestFixtureOrmLiteDialects(Dialect.SqlServer2008 | Dialect.SqlServer2012)]
public class SqlDbFeatures1 : OrmLiteProvidersTestBase 
{
    ...
    
    [Test]
    public void Test1() 
    {
        // Will execute for SqlServer provider and dialect versions

        // Current test dialect
        var dialect = base.Dialect;

        // current test dialectprovider
        var dialectProvider = base.DialectProvider

        // open the correct db connection based on dialect 
        using(var db = OpenConnection())
        {
            // db agnostic tests
        }
    }
}
```

## Excluding specific tests 

### For all tests in fixture

To exclude testing specific dialects for all tests in a fixture, use the `IgnoreDialectAttribute`

```csharp
[TestFixtureOrmLite()]
[IgnoreDialect(Dialects.AnyMySql | Dialects.PostgreSql9, "Not supported by database")]
public class SqlDbFeatures1 : OrmLiteProvidersTestBase 
{
}
```

### Individual tests 

To exclude individual tests for one or more db providers, use the `IgnoreDialectAttribute`

```csharp
[Test]
[IgnoreDialect(Dialect.PostgreSql9 | Dialect.PostgreSql10, "ignore message to output")]
[IgnoreDialect(Dialect.AnySqlServer, "diff ignore message to output")]
public void Test1()
{
    // Test will not run for any dialects ignored above but any others 
}
``` 

### Test runner filtering

Each test has a category added corresponding to the dialect which allows for filtering 
tests using the existing Category filters for nunit runners or dotnet test.

```bash
# Only run Sql server dialect tests
dotnet test --filter TestCategory=AnySqlServer

# Run all tests except MySql5_5
dotnet test --filter TestCategory!=MySql5_5 
``` 
