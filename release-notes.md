# Release Notes
--------

# New HTTP Benchmarks example project

[![HTTP Benchmarks](https://raw.githubusercontent.com/ServiceStack/HttpBenchmarks/master/src/ResultsView/Content/img/AdminUI.png)](https://benchmarks.servicestack.net/)

Following the release of the [Email Contacts](https://github.com/ServiceStack/EmailContacts/) solution, a new documented ServiceStack example project allowing you to uploaded Apache HTTP Benchmarks to visualize and analyze their results has been released at: [github.com/ServiceStack/HttpBenchmarks](https://github.com/ServiceStack/HttpBenchmarks) and is hosted at [benchmarks.servicestack.net](https://benchmarks.servicestack.net/).

### Example Results

  - [Performance of different RDBMS in an ASP.NET Host](https://benchmarks.servicestack.net/databases-in-asp-net)
  - [Performance of different ServiceStack Hosts](https://benchmarks.servicestack.net/servicestack-hosts)

The documentation includes a development guide that walks through the projects different features:

 - Integration with `Glimpse` with support for `DotNetOpenAuth`
 - Allow authentication with Twitter, Facebook, Google and LinkedIn OAuth providers
 - Enables registration of new user accounts
 - Use of `[FallbackRoute]` attribute to allow users to create top-level routes (e.g. twitter.com/name) 
 - Explains why you want to aim for minimal JS dependencies
 - Introduction of **Really Simple MV Pattern** using plain JavaScript
 - Integration with multi-file Uploader `FineUploader`
 - Processes multiple file uploads including files in **.zip** packages using `DotNetZip`
 - Integration with `Highcharts.js`
 - Hosting differences of ASP.NET with AWS
 - Deploying to AWS and creating customized deployment packages with MSDeploy
 - Configuring SSL
 - Forcing SSL Redirects

The repository also includes benchmark scripts and host projects of [all ServiceStack HTTP Hosts](https://github.com/ServiceStack/HttpBenchmarks/tree/master/servers
), which all support runtime configuration of different RDBMS's: 

# v4.0.18 Release Notes

## New, much faster Self-Host!

Prior to this release ServiceStack had 2 self-hosting options with different [Concurrency Models](https://github.com/ServiceStack/ServiceStack/wiki/Concurrency-model):

- `AppHostHttpListenerBase` - Executes requests on the IO callback thread
- `AppHostHttpListenerPoolBase` - Executes requests on .NET's built-in ThreadPool

Where in typical scenarios (i.e. CPU intensive or blocking IO), executing on .NET's Thread Pool provides better performance.

This [Self-hosting performance analysis](http://en.rdebug.com/2013/05/06/servicestack-selfhosted-performance-boost/) from the ServiceStack community shows we're able to achieve even better performance by utilizing the excellent [Smart Thread Pool](http://www.codeproject.com/Articles/7933/Smart-Thread-Pool) instead, which is now available in the `AppHostHttpListenerSmartPoolBase` base class.

The new Smart Pool self-host routinely outperforms all other self hosting options, and does especially well in heavy IO scenarios as seen in the benchmarks below: 

<table>
    <thead>
        <tr>
            <th></th>
            <th>Self Host</th>
            <th>ASP.NET/IIS Express</th>
            <th>HttpListener Pool</th>
            <th>HttpListener</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <th>Database updates</th>
            <td>1x</td>
            <td>1.9x</td>
            <td>2x</td>
            <td>4.1x</td>
        </tr>
        <tr>
            <th>Single database query</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.5x</td>
            <td>2.6x</td>
        </tr>
        <tr>
            <th>Multiple database queries</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.4x</td>
            <td>2.6x</td>
        </tr>
        <tr>
            <th>Plaintext</th>
            <td>1x</td>
            <td>2.3x</td>
            <td>2.4x</td>
            <td>1.6x</td>
        </tr>
        <tr>
            <th>Fortunes Razor View</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.5x</td>
            <td>1.8x</td>
        </tr>
        <tr>
            <th>JSON serialization</th>
            <td>1x</td>
            <td>1.2x</td>
            <td>1.4x</td>
            <td>1x</td>
        </tr>
    </tbody>
</table>

### Using different Self Host options

You can easily switch between the different self-hosting options by simply changing your AppHost's base class, e.g: 

```csharp
public class AppHost : AppHostHttpListenerBase { ... }
public class AppHost : AppHostHttpListenerPoolBase { ... }
public class AppHost : AppHostHttpListenerSmartPoolBase { ... }
```

Both the HttpListener Pool and SmartPool hosts have configurable pool sizes that can be tweaked to perform better under different scenarios.

### Optimal Self Hosted option

As the number of self-hosts grow, we've added a new `AppSelfHostBase` base class that represents an alias for the highest performing self-hosting option with an optimal configuration that we'll continue to tune for performance against typical scenarios. Unless you've identified specific configurations that performs better for your use-case, the recommendation is for new self-hosts to inherit this configuration:

```csharp
public class AppHost : AppSelfHostBase { ... }
```

## OrmLite 

OrmLite received a lot more attention this release with a number of value-added additions:

### Improved Oracle RDBMS provider

The OrmLite Oracle Provider has been significantly improved thanks to [Bruce Cowen](https://github.com/BruceCowan-AI) efforts who's brought the quality in-line with other RDBMS providers which now passes OrmLite's test suite. As part of this change, the Oracle Provider now depends on [Oracle's Data Provider for .NET](http://www.oracle.com/technetwork/topics/dotnet/index-085163.html) and can be installed with: 

    PM> Install-Package ServiceStack.OrmLite.Oracle
    PM> Install-Package ServiceStack.OrmLite.Oracle.Signed

More notes about the Oracle provider are maintained in the [OrmLite Release Notes](https://github.com/ServiceStack/ServiceStack.OrmLite/#oracle-provider-notes).

### Improved Typed SqlExpressions

The existing `db.SqlExpression<T>()` API has a more readable alias in:

```csharp
db.From<Table>();
```

Which now supports an optional SQL **FROM** fragment that can be used to specify table joins, e.g:

    var results = db.Select(db.From<Person>("Person INNER JOIN Band ON Person.Id = Band.PersonId"));

#### New ISqlExpression API

OrmLite API's have overloads to execute any SQL builders that implement the simple `ISqlExpression` API, i.e:

```csharp
public interface ISqlExpression
{
    string ToSelectStatement();
}
```

This allows for more readable code when using a decoupled Sql Builder, e.g:

```csharp
int over40s = db.Scalar<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age > 40));

List<string> lastNames = db.Column<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age == 27));

HashSet<int> uniqueAges = db.ColumnDistinct<int>(db.From<Person>().Select(x => x.Age).Where(q => q.Age < 50));

Dictionary<int,string> map = db.Dictionary<int,string>(db.From<Person>().Select(x => new {x.Id, x.LastName}));
```

#### Partial Selects

This also improves the APIs for partial SELECT queries, which originally required the use of custom SQL:

```csharp
var partialColumns = db.SelectFmt<SubsetOfShipper>(typeof(Shipper), "ShipperTypeId = {0}", 2);
```
    
But can now be expressed in any of the more typed examples below: 

```csharp
var partialColumns = db.Select<SubsetOfShipper>(db.From<Shipper>().Where(q => q.ShipperTypeId == 2));
```

Or partially populating the same POCO with only the columns specified:

```csharp
var partialColumns = db.Select<Shipper>(q => q.Select(x => new { x.Phone, x.CompanyName })
                                              .Where(x => x.ShipperTypeId == 2));

var partialColumns = db.Select<Shipper>(q => q.Select("Phone, CompanyName")
                                              .Where(x => x.ShipperTypeId == 2));
```

#### Nullable Limit APIs

The Limit API's now accept `int?` making it easier to apply paging in your ServiceStack services, e.g:

```csharp
public Request 
{
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

public List<Table> Any(Request request)
{
    return Db.Select(db.From<Table>.Limit(request.Skip, request.Take));
}
```

Which will only filter the results for the values provided. Aliases for `Skip()` and `Take()` are also available if LINQ naming is preferred.

#### New AliasNamingStrategy

A new alias naming strategy was added (in addition to `[Alias]` attribute) that lets you specify a dictionary of Table and Column aliases OrmLite should used instead, e.g:

```csharp
OrmLiteConfig.DialectProvider.NamingStrategy = new AliasNamingStrategy {
    TableAliases  = { { "MyTable", "TableAlias" } },
    ColumnAliases = { { "MyField", "ColumnAlias" } },
};
```

Which OrmLite will use instead, e.g when creating a table:

```csharp
db.CreateTable<MyTable>();
```

Aliases can also be referenced when creating custom SQL using the `SqlTable()` and `SqlColumn()` extension methods, e.g:

```csharp
var result = db.SqlList<MyTable>(
    "SELECT * FROM {0} WHERE {1} = {2}".Fmt(
        "MyTable".SqlTable(),
        "MyField".SqlColumn(), "foo".SqlValue()));
```

#### New Exists APIs

Nicer if you just need to check for existence, instead of retrieving a full result-set e.g:

```csharp
bool hasUnder50s = db.Exists<Person>(x => x.Age < 50);
bool hasUnder50s = db.Exists(db.From<Person>().Where(x => x.Age < 50));
```

## Redis

### New Scan APIs Added

Redis v2.8 introduced a beautiful new [SCAN](http://redis.io/commands/scan) operation that provides an optimal strategy for traversing a redis instance entire keyset in managable-size chunks utilizing only a client-side cursor and without introducing any server state. It's a higher performance alternative and should be used instead of [KEYS](http://redis.io/commands/keys) in application code. SCAN and its related operations for traversing members of Sets, Sorted Sets and Hashes are now available in the Redis Client in the following API's:

```csharp
public interface IRedisClient
{
    ...
    IEnumerable<string> ScanAllKeys(string pattern = null, int pageSize = 1000);
    IEnumerable<string> ScanAllSetItems(string setId, string pattern = null, int pageSize = 1000);
    IEnumerable<KeyValuePair<string, double>> ScanAllSortedSetItems(string setId, string pattern = null, int pageSize = 1000);
    IEnumerable<KeyValuePair<string, string>> ScanAllHashEntries(string hashId, string pattern = null, int pageSize = 1000);    
}

//Low-level API
public interface IRedisNativeClient
{
    ...
    ScanResult Scan(ulong cursor, int count = 10, string match = null);
    ScanResult SScan(string setId, ulong cursor, int count = 10, string match = null);
    ScanResult ZScan(string setId, ulong cursor, int count = 10, string match = null);
    ScanResult HScan(string hashId, ulong cursor, int count = 10, string match = null);
}
```

The `IRedisClient` provides a higher-level API that abstracts away the client cursor to expose a lazy Enumerable sequence to provide an optimal way to stream scanned results that integrates nicely with LINQ, e.g:

```csharp
var scanUsers = Redis.ScanAllKeys("urn:User:*");
var sampleUsers = scanUsers.Take(10000).ToList(); //Stop after retrieving 10000 user keys 
```

### New HyperLog API

The development branch of Redis server (available when v3.0 is released) includes an ingenious algorithm to approximate the unique elements in a set with maximum space and time efficiency. For details about how it works see Redis's creator Salvatore's blog who [explains it in great detail](http://antirez.com/news/75). Essentially it lets you maintain an efficient way to count and merge unique elements in a set without having to store its elements. 
A Simple example of it in action:

```csharp
redis.AddToHyperLog("set1", "a", "b", "c");
redis.AddToHyperLog("set1", "c", "d");
var count = redis.CountHyperLog("set1"); //4

redis.AddToHyperLog("set2", "c", "d", "e", "f");

redis.MergeHyperLogs("mergedset", "set1", "set2");

var mergeCount = redis.CountHyperLog("mergedset"); //6
```

## HTTP and MQ Service Clients

### Substitutable OneWay MQ and HTTP Service Clients

Service Clients and MQ Clients have become a lot more interoperable where all MQ Clients now implement the Service Clients `IOneWayClient` API which enables writing code that works with both HTTP and MQ Clients:

```csharp
IOneWayClient client = GetClient();
client.SendOneWay(new RequestDto { ... });
```

Likewise the HTTP Service Clients implement the Messaging API `IMessageProducer`:

```csharp
void Publish<T>(T requestDto);
void Publish<T>(IMessage<T> message);
```

When publishing a `IMessage<T>` the message metadata are sent as HTTP Headers with an `X-` prefix.

### UploadProgress added on Service Clients

Which works similar to [OnDownloadProgress](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/AsyncProgressTests.cs) where you can specify a callback to provide UX Progress updates, e.g:

```csharp
client.OnUploadProgress = (bytesWritten, total) => "Written {0}/{1} bytes...".Print(bytesWritten, total);

client.PostFileWithRequest<UploadResponse>(url, new FileInfo(path), new Upload { CreatedBy = "Me" });
```

## Razor Support

Our support for [No Ceremony Razor pages](https://github.com/ServiceStack/EmailContacts/#the-no-ceremony-option---dynamic-pages-without-controllers) has been very well received which has all but alleviated the need of requiring services / controllers for dynamic html pages. One of the areas where a Service may be required is for execution any custom request filters, which we've now added support for by letting you choose to execute all request filters for a specific Request with: 

```csharp
@{
    ApplyRequestFilters(new RequestDto());
}
```

This will execute all the Request Filters applied to the specified Request DTO. Any one of the filters ends the request (e.g. with a redirect) and the rest of the Razor page will stop execution.

Likewise it's possible to redirect from within Razor with:

```csharp
@{
    if (!IsAuthenticated) {
        Response.RedirectToUrl("/login");
        throw new StopExecutionException();
    }
}
```
An alternative to `StopExecutionException` is to have an explicit `return;`, the difference being that it will continue to execute the remainder of the page, although neither approach will emit any Razor output to the response.

As redirecting non-authenticated users is a common use-case it's also available as a one-liner:

```csharp
@{
    RedirectIfNotAuthenticated();
}
```

Which if no url is specified it will redirect to the path configured on `AuthFeature.HtmlRedirect`.

### ss-utils.js

A few enhancements were added to ServiceStack's **/js/ss-utils.js** is ServiceStack's built-in JS library, first demonstrated in [Email Contacts solution](https://github.com/ServiceStack/EmailContacts/#servicestack-javascript-utils---jsss-utilsjs):

Declarative event handlers can send multiple arguments:

```html
<ul>
    <li data-click="single">Foo</li>
    <li data-click="multiple:arg1,arg2">Bar</li>
</ul>
```

```javascript
$(document).bindHandlers({
    single: function(){
        var li = this;
    },
    multiple: function(arg1, arg2) {
        var li = this;
    }
});
```

Trigger client-side validation errors with `setFieldError()`:

```javascript
$("form").bindForm({
    validate: function(){
        var params = $(this).serializeMap();
        if (params.Password != params.Confirm){
            $(this).setFieldError('Password', 'Passwords to not match');
            return false;
        }
    }
});
```

Model binding now also populates `data-href` and `data-src` attributes e.g:

```html
<a data-href="FieldName"><img data-src="FieldName" /></a>
```

```javascript
$("form").applyValues({ FieldName: imgUrl });
```
## Other Changes

### Restriction attributes allowed on Services
    
Restriction attributes can be added on Service classes in addition to Request DTOs (which still take precedence).

```csharp
[Restrict(LocalhostOnly = true)]
public class LocalHostOnlyServices : Service { ... }
```

## AppSettings

### New OrmLiteAppSettings

Added new read/write AppSettings config option utilizing OrmLite as the back-end. 
This now lets you maintain your applications configuration in any [RDBMS back-end OrmLite supports](https://github.com/ServiceStack/ServiceStack.OrmLite/#download). It basically works like a mini Key/Value database in which can store any serializable value against any key which is maintained into the simple Id/Value `ConfigSettings` table.

#### Usage

Registration just uses an OrmLite DB Factory, e.g:

```csharp
container.Register(c => new OrmLiteAppSettings(c.Resolve<IDbConnectionFactory>()));
var appSettings = container.Resolve<OrmLiteAppSettings>();
appSettings.InitSchema(); //Create the ConfigSettings table if it doesn't exist
```

It then can be accessed like any [AppSetting APIs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/Configuration/AppSettingsTests.cs):

```csharp
//Read the `MyConfig` POCO stored at `config` otherwise use default value if it doesn't exist
MyConfig config = appSettings.Get("config", new MyConfig { Key = "DefaultValue" });
```

It also supports writing config values in addition to the AppSettings read-only API's, e.g:

```csharp
var latestStats = appSettings.GetOrCreate("stats", () => statsProvider.GetLatest());
```

### Extract key / value settings from text file

The new ParseKeyValueText extension method lets you extract key / value data from text, e.g: 

```csharp
var configText = @"
StringKey string value
IntKey 42
ListKey A,B,C,D,E
DictionaryKey A:1,B:2,C:3,D:4,E:5
PocoKey {Foo:Bar,Key:Value}";

Dictionary<string, string> configMap = configText.ParseKeyValueText(delimiter:" ");
```

When combined with the existing `DictionarySettings`, enables a rich, simple and clean alternative to .NET's App.config config section for reading structured configuration into clean data structures, e.g:

```csharp
IAppSettings appSettings = new DictionarySettings(configMap);

string value = appSettings.Get("StringKey");

int value = appSettings.Get("IntKey", defaultValue:1);

List<string> values = appSettings.GetList("ListKey");

Dictionary<string,string> valuesMap = appSettings.GetList("DictionaryKey");

MyConfig config = appSettings.Get("PocoKey", new MyConfig { Key = "DefaultValue"});
```

As we expect this to be a popular combination we've combined them into a single class that accepts a filePath, providing a simple alternative to custom Web.config configurations:

```csharp
var appSettings = new TextFileSettings("~/app.settings".MapHostAbsolutePath());
```

### PerfUtils

We've included the [C# Benchmark Utils](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Common/PerfUtils.cs) previously used in [Sudoku Benchmarks](https://github.com/dartist/sudoku_solver#benchmarks) originally inspired from [Dart's benchmark_harness](https://github.com/dart-lang/benchmark_harness). Unlike other benchmark utils, it runs for a specified period of time (2000ms by default) then returns the avg iteration time in microseconds. Here's an example usage comparing performance of maintaining a unique int collection between HashSet vs List:

```csharp
var rand = new Random();
var set = new HashSet<int>();
var avgMicroSecs = PerfUtils.Measure(
    () => set.Add(rand.Next(0, 1000)), runForMs:2000);

"HashSet: {0}us".Print(avgMicroSecs);

var list = new List<int>();
avgMicroSecs = PerfUtils.Measure(() => {
        int i = rand.Next(0, 1000);
        if (!list.Contains(i))
            list.Add(i);
    }, runForMs: 2000);

"List: {0}us".Print(avgMicroSecs);
```

### Minor Changes

- Numeric type mismatches between POCOs used in OrmLite and underlying RDBMS Tables are transparently coerced
- `Vary: Accept` is included in Global HTTP Headers to resolve browsers caching different Content-Type for the same url
- Razor configuration removes references to a specific version of ASP.NET Web Pages and adds `System` to default namespaces
- Swagger API emits an ApiVersion, configurable with `Config.ApiVersion` that defaults to "1.0"    
- Partials now render inside user-defined Razor sections
- Added `email.ToGravatarUrl()` extension method to retrieve avatar url from an email
- Replaced self-hosts use of ThreadStatics with CallContext to preserve Request scope in async requests
- Avoid runtime razor exceptions in Mono by not registering duplicate assemblies (i.e. from GAC) in RazorHost
- AppHostHttpListenerPoolBase self-host has a default pool size of `16 x Environment.ProcessorCount`
- ServiceStack's `IAppHost.CustomErrorHttpHandlers` can now override built-in HTTP Error handlers and fallback to generic error responses

### New Signed Projects

- [ServiceStack.ProtoBuf.Signed](https://www.nuget.org/packages/ServiceStack.ProtoBuf.Signed)

### Breaking Changes

- Moved `Config.GlobalHtmlErrorHttpHandler` to `IAppHost.GlobalHtmlErrorHttpHandler`


# v4.0.15 Release Notes

### Individual Products now available

In this release we've added the most requested "non-technical feature" by creating new licenses for [individual ServiceStack products](https://servicestack.net/#products) which provide
much better value when only using one of ServiceStack's stand-alone libraries on their own. 

New products available:

  - [servicestack.net/text](https://servicestack.net/text)
  - [servicestack.net/redis](https://servicestack.net/redis)
  - [servicestack.net/ormlite](https://servicestack.net/ormlite)

> Both OrmLite and Redis includes an implicit license for ServiceStack.Text

### ServiceStack

  - Upgraded ServiceStack's external dependencies to use latest version on NuGet
  - Modified [ServiceStack.RabbitMq](http://www.nuget.org/packages/ServiceStack.RabbitMq) to only depend on **ServiceStack** instead of **ServiceStack.Server**
  - Added optional `fieldName` property to ServiceClient [PostFileWithRequest](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/IRestClient.cs#L52-L55)
  - Changed exceptions in FileSystem scanning to be logged as warnings, fixes issues with NTFS symbolic links
  - Pass through Thread CurrentCulture when executing a sync request in a new Task
  - Added Evaluator.NamespaceAssemblies to specify alternate default namespace for Assemblies 
  - Changed to use OrdinalIgnoreCase instead of InvariantCultureIgnoreCase when possible

### OrmLite

#### OrmLite's core Exec functions are now overridable as a Filter

Continuing in efforts to make OrmLite more introspectable and configurable, OrmLite's core Exec functions 
[have been re-factored out into a substitutable Exec Filter](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/fa55404200f4a319eae3a298b648462dadafce5e).

This now makes it possible to inject a custom managed exec function where you can inject your own behavior, tracing, profiling, etc.

It comes in useful for situations when you want to use SqlServer in production but use an `in-memory` Sqlite database in tests and you want to emulate any missing SQL Server Stored Procedures in code:

```csharp
public class MockStoredProcExecFilter : OrmLiteExecFilter
{
    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        try
        {
            return base.Exec(dbConn, filter);
        }
        catch (Exception ex)
        {
            if (dbConn.GetLastSql() == "exec sp_name @firstName, @age")
                return (T)(object)new Person { FirstName = "Mocked" };
            throw;
        }
    }
}

OrmLiteConfig.ExecFilter = new MockStoredProcExecFilter();

using (var db = OpenDbConnection())
{
    var person = db.SqlScalar<Person>("exec sp_name @firstName, @age",
        new { firstName = "aName", age = 1 });

    person.FirstName.Print(); //Mocked
}
```
Or if you want to do things like executing each operation multiple times, e.g:

```csharp
public class ReplayOrmLiteExecFilter : OrmLiteExecFilter
{
    public int ReplayTimes { get; set; }

    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        var holdProvider = OrmLiteConfig.DialectProvider;
        var dbCmd = CreateCommand(dbConn);
        try
        {
            var ret = default(T);
            for (var i = 0; i < ReplayTimes; i++)
            {
                ret = filter(dbCmd);
            }
            return ret;
        }
        finally
        {
            DisposeCommand(dbCmd);
            OrmLiteConfig.DialectProvider = holdProvider;
        }
    }
}

OrmLiteConfig.ExecFilter = new ReplayOrmLiteExecFilter { ReplayTimes = 3 };

using (var db = OpenDbConnection())
{
    db.DropAndCreateTable<PocoTable>();
    db.Insert(new PocoTable { Name = "Multiplicity" });

    var rowsInserted = db.Count<PocoTable>(q => q.Name == "Multiplicity"); //3
}
```

#### Other improvements

  - Added [SqlVerifyFragment string extension](https://github.com/ServiceStack/ServiceStack.OrmLite/commit/7f0711aa3368087037d8b7b84cf9f70f1ea2b191) to verify sql fragments where free-text is allowed in SqlExpression APIs  
  - Change MySql to create TimeSpan's column as INT to store ticks

### Redis

  - Add new Increment by double and long methods to Redis Client

### Text

  - Added [T.PopulateFromPropertiesWithoutAttribute](https://github.com/ServiceStack/ServiceStack.Text/commit/9bd0cc35c0a4e3ddcb7e6b6b88e760f45496145b) Auto Mapping method

### New Signed NuGet Packages

  - [ServiceStack.OrmLite.Sqlite.Windows.Signed](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite.Windows.Signed) 

# v4.0.12 Release Notes

## New [Email Contact Services](https://github.com/ServiceStack/EmailContacts/)

A new ServiceStack guidance is available detailing the recommended setup and physical layout structure of typical medium-sized ServiceStack projects.
It includes the complete documentation going through how to create the solution from scratch, and explains all the ServiceStack hidden features it makes use of along the way.

[![EmailContacts Screenshot](https://raw.github.com/ServiceStack/EmailContacts/master/src/EmailContacts/Content/splash.png)](https://github.com/ServiceStack/EmailContacts/)

[EmailContacts](https://github.com/ServiceStack/EmailContacts/) is a Single Page App built using just ServiceStack, 
jQuery and Bootstrap that showcases some of ServiceStack's built-in features, useful in the reducing the effort for 
developing medium-sized Web Applications.

The purpose of EmailContacts is to manage contacts (in [any RDBMS](https://github.com/ServiceStack/ServiceStack.OrmLite/#download)), 
provide a form to be able to send them messages and maintain a rolling history of any emails sent. 
The application also provides an option to have emails instead sent and processed via [Rabbit MQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ).

#### Functional Single Page App in under 130 Lines of HTML and 70 Lines JS

The entire EmailContacts UI is maintained in a single 
[default.cshtml](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/default.cshtml) 
requiring just 70 lines of JavaScript to render the dynamic UI, 
bind server validation errors and provide real-time UX feedback. 
The Application also follows an API-First development style where the Ajax UI calls only published APIs allowing 
all services to be immediately available, naturally, via an end-to-end typed API to Mobile and Desktop .NET clients.

### Example Projects

During this release all Example projects, Demos, Starter Templates, etc in the 
[ServiceStack.Example](https://github.com/ServiceStack/ServiceStack.Examples) and 
[ServiceStack.UseCases](https://github.com/ServiceStack/ServiceStack.UseCases/) 
master repositories were upgraded to ServiceStack v4. A new [ServiceStack + MVC5 project](https://github.com/ServiceStack/ServiceStack.UseCases/tree/master/Mvc5) was also added to UseCases, it just follows the instructions at [[MVC Integration]] wiki, but starts with an empty MVC5 project.

### Added new OrmLiteCacheClient

A new `OrmLiteCacheClient` [Caching Provider](https://github.com/ServiceStack/ServiceStack/wiki/Caching) 
was added to the **ServiceStack.Server** NuGet pacakge. 
This provides a lot of utility by supporting 
[OrmLite's RDBMS providers](https://github.com/ServiceStack/ServiceStack.OrmLite/#download) 
allowing utilization of existing RDBMS's as a distributed cache, potentially saving an infrastructure dependency.

Registration is simply:

```csharp 
//Register OrmLite Db Factory if not already
container.Register<IDbConnectionFactory>(c => 
    new OrmLiteConnectionFactory(connString, SqlServerDialect.Provider)); 

container.RegisterAs<OrmLiteCacheClient, ICacheClient>();

//Create 'CacheEntry' RDBMS table if it doesn't exist already
container.Resolve<ICacheClient>().InitSchema(); 
``` 

### Service Clients

  - Added `CaptureSynchronizationContext` option to get Async Service Clients to execute responses on the same SynchronizationContext as their call-site
  - Added `UserAgent` option, now defaults with the ServiceStack .NET client version

### Minor features

  - Allow unrestricted access for Redis MQ and Rabbit MQ clients within free-quotas
  - SessionIds are no longer created with Url Unfriendly chars `+`, `/`
  - Add typed `ToOneWayUrl()` and `ToReplyUrl()` extension method for generating predefined urls
  - Add Test showing how to use `ExecAllAndWait` extension method to [easily run synch operations in parallel](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/ActionExecTests.cs)
  - Added configurable BufferSize in StaticFileHandler
  - All CacheClients can now store AuthUserSessions when `JsConfig.ExcludeTypeInfo=true`
  - Allow RegistrationService to be used for PUT requests to updates User Registration info
  - Elmah Logger now takes in a `HttpApplication` so it can use `ErrorSignal.Get(application).Raise(<exception>)` allowing modules such as ErrorMail and ErrorPost (ElmahR) to be notified

## OrmLite

  - Add support for [cloning SqlExpressions](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/Expression/ExpressionChainingUseCase.cs#L192-L207)
  - Add example of [migrating SqlServer TIME column to BigInteger](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/AdoNetDataAccessTests.cs)
  - Add example of [calling Stored Procedures with OrmLite vs ADO.NET](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/TypeWithByteArrayFieldTests.cs#L55-L147)
  - Add support for [MaxText in all DB providers](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/TypeDescriptorMetadataTests.cs#L57-L96) with `[StringLength(StringLengthAttribute.MaxText)]`
  - Capture the LastSql Run even for queries with exceptions

## Redis

  - Use enhanced functionality for when newer versions of redis-server exists
    - i.e. Use more precise EXPIRE operations when server supports it
  - Add `GetServerTime()` 

## ServiceStack.Text

  - Moved `JsConfig.RegisterForAot()` to `PclExport.RegisterForAot()`
    - Fine-grained AOT hints available on `IosPclExport` static methods in PCL builds

## Breaking Changes

The [ServiceStack.Stripe](https://www.nuget.org/packages/ServiceStack.Stripe/) NuGet package is now a normal .NET 4.0 release. A new portable NuGet package was created for PCL clients at [ServiceStack.Stripe.Pcl](https://www.nuget.org/packages/ServiceStack.Stripe.Pcl/).

# v4.0.11 Release Notes

## OrmLite

This release saw a lot of effort towards adding new features to OrmLite:

### Pluggable Complex Type Serializers

One of the [most requested features](http://servicestack.uservoice.com/forums/176786-feature-requests/suggestions/4738945-allow-ormlite-to-store-complex-blobs-as-json)
to enable pluggable serialization for complex types in OrmLite is now supported. This can be used to specify different serialization strategies for each 
available RDBMS provider, e.g:

```csharp
//ServiceStack's JSON and JSV Format
SqliteDialect.Provider.StringSerializer = new JsvStringSerializer();       
PostgreSqlDialect.Provider.StringSerializer = new JsonStringSerializer();
//.NET's XML and JSON DataContract serializers
SqlServerDialect.Provider.StringSerializer = new DataContractSerializer();
MySqlDialect.Provider.StringSerializer = new JsonDataContractSerializer();
//.NET XmlSerializer
OracleDialect.Provider.StringSerializer = new XmlSerializableSerializer();
```
You can also provide a custom serialization strategy by implementing 
[IStringSerializer](https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/IStringSerializer.cs).

By default all dialects use the existing JsvStringSerializer, except for PostgreSQL which due to its built-in support for JSON, now uses the JSON format by default.  

#### Breaking Change

Using JSON as a default for PostgreSQL may cause issues if you already have complex types blobbed with the previous JSV Format.
You can revert back to the old behavior by resetting it back to the JSV format with:

```csharp
PostgreSqlDialect.Provider.StringSerializer = new JsvStringSerializer();
```

### New Global Insert / Update Filters

Similar to interceptors in some heavy ORM's, new Insert and Update filters were added which get fired just before any **insert** or **update** operation using OrmLite's typed API's (i.e. not dynamic SQL or partial updates using anon types).
This functionality can be used for easily auto-maintaining Audit information for your POCO data models, e.g:

```csharp
public interface IAudit 
{
    DateTime CreatedDate { get; set; }
    DateTime ModifiedDate { get; set; }
    string ModifiedBy { get; set; }
}

OrmLiteConfig.InsertFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null)
        auditRow.CreatedDate = auditRow.ModifiedDate = DateTime.UtcNow;
};

OrmLiteConfig.UpdateFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null)
        auditRow.ModifiedDate = DateTime.UtcNow;
};
```

Which will ensure that the `CreatedDate` and `ModifiedDate` fields are populated on every insert and update.

### Validation

The filters can also be used for validation where throwing an exception will prevent the operation and bubble the exception, e.g:

```csharp
OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = (dbCmd, row) => {
    var auditRow = row as IAudit;
    if (auditRow != null && auditRow.ModifiedBy == null)
        throw new ArgumentNullException("ModifiedBy");
};

try
{
    db.Insert(new AuditTable());
}
catch (ArgumentNullException) {
   //throws ArgumentNullException
}

db.Insert(new AuditTable { ModifiedBy = "Me!" }); //succeeds
```

### Custom SQL Customizations

A number of new hooks were added to provide more flexibility when creating and dropping your RDBMS tables.

#### Custom Field Declarations

The new `[CustomField]` can be used for specifying custom field declarations in the generated Create table DDL statements, e.g:

```csharp
public class PocoTable
{
    public int Id { get; set; }

    [CustomField("CHAR(20)")]
    public string CharColumn { get; set; }

    [CustomField("DECIMAL(18,4)")]
    public decimal? DecimalColumn { get; set; }
}

db.CreateTable<PocoTable>(); 
```

Generates and executes the following SQL:

```sql
CREATE TABLE "PocoTable" 
(
  "Id" INTEGER PRIMARY KEY, 
  "CharColumn" CHAR(20) NULL, 
  "DecimalColumn" DECIMAL(18,4) NULL 
);  
```

#### Pre / Post Custom SQL Hooks when Creating and Dropping tables 

A number of custom SQL hooks were added that allow you to inject custom SQL before and after tables are created or dropped, e.g:

```csharp
[PostCreateTable("INSERT INTO TableWithSeedData (Name) VALUES ('Foo');" +
                 "INSERT INTO TableWithSeedData (Name) VALUES ('Bar');")]
public class TableWithSeedData
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}
```

And just like other ServiceStack attributes, they can also be added dynamically, e.g:

```csharp
typeof(TableWithSeedData)
    .AddAttributes(new PostCreateTableAttribute(
        "INSERT INTO TableWithSeedData (Name) VALUES ('Foo');" +
        "INSERT INTO TableWithSeedData (Name) VALUES ('Bar');"));
```

Custom SQL Hooks are now available to execute custom SQL before and after a table has been created or dropped, i.e:

```csharp
[PreCreateTable(runSqlBeforeTableCreated)]
[PostCreateTable(runSqlAfterTableCreated)]
[PreDropTable(runSqlBeforeTableDropped)]
[PostDropTable(runSqlAfterTableDropped)]
public class Table {}
```

### Re-factoring OrmLite's SQLite NuGet Packages

In their latest release, the SQLite dev team maintaining the [core SQLite NuGet packages](https://www.nuget.org/profiles/mistachkin/) 
have added a dependency to Entity Framework on their existing Sqlite NuGet packages forcing the installation of Entity Framework for users of OrmLite Sqlite. 
This change also caused some users to see invalid web.config sections after applying the new web.config.transforms.
After speaking to the maintainers they've created a new 
[System.Data.SQLite.Core](http://www.nuget.org/packages/System.Data.SQLite.Core) 
NuGet package without the entity framework dependency and the problematic web.config.transforms.

Unfortunately this was only added for their bundled x86/x64 NuGet package and not their other 
[System.Data.SQLite.x86](http://www.nuget.org/packages/System.Data.SQLite.x86/) and
[System.Data.SQLite.x64](http://www.nuget.org/packages/System.Data.SQLite.x64/) which the team have indicated should be deprecated
in favor of the x86/x64 bundled **System.Data.SQLite.Core** package. 

As a result of this we're removing the dependency to the Sqlite NuGet packages in both architecture specific
[ServiceStack.OrmLite.Sqlite32](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite32/) and 
[ServiceStack.OrmLite.Sqlite64](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite64/) packages and have
instead embedded the Sqlite binaries directly, which will solve the current issues and shield them from any future changes/updates 
from the upstream Sqlite packages.

#### New ServiceStack.OrmLite.Sqlite.Windows NuGet package

Both these arch-specific packages should now be deprecated in favour of a new Sqlite NuGet package supporting both x86/x64 architectures on Windows:

    PM> Install-Package ServiceStack.OrmLite.Sqlite.Windows

Which should now be used for future (or existing) projects previously using the old 
[OrmLite.Sqlite32](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite32/) and 
[OrmLite.Sqlite64](http://www.nuget.org/packages/ServiceStack.OrmLite.Sqlite64/) packages.

The Windows-specific package was added in addition to our existing Mono and Windows compatible release:

    PM> Install-Package ServiceStack.OrmLite.Sqlite.Mono

Which works cross-platform on Windows and Linux/OSX with Mono should you need cross-platform support.  

## .NET Service Clients

New async API's were added for requests marked with returning `IReturnVoid`.
This provides a typed API for executing services with no response that was previously missing, e.g:

```csharp
public class Request : IReturnVoid {}

await client.PostAsync(new Request());
```

The API's for all sync and async REST operations have been changed to return `HttpWebResponse` which now lets you query the returned HTTP Response, e.g:
```csharp
HttpWebResponse response = await client.PostAsync(new Request());
var api = response.Headers["X-Api"];
```

## Authentication

### New IManageRoles API

A new [IManageRoles API](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/IAuthRepository.cs#L26) 
was added that IAuthRepository's can implement in order to provide an alternative strategy for querying and managing Users' 
Roles and permissions. 

This new API is being used in the `OrmLiteAuthRepository` to provide an alternative way to store 
Roles and Permission in their own distinct table rather than being blobbed with the rest of the User Auth data. 
You can enable this new behavior by specifying `UseDistinctRoleTables=true` when registering the OrmLiteAuthRepository, e.g:

```csharp
container.Register<IAuthRepository>(c =>
new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
    UseDistinctRoleTables = true,
});
```

When enabled, roles and permissions are persisted in the distinct **UserAuthRole** table. 
This behavior is integrated with the rest of ServiceStack including the Users Session, RequiredRole/RequiredPermission attributes and the AssignRoles/UnAssignRoles authentication services.
Examples of this can be seen in [ManageRolesTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Common.Tests/ManageRolesTests.cs).

## [Messaging](https://github.com/ServiceStack/ServiceStack/wiki/Messaging)

### Flexible Queue Name strategies

There are now more flexible options for specifying the Queue Names used in [ServiceStack's MQ Servers](https://github.com/ServiceStack/ServiceStack/wiki/Messaging).
You can categorize queue names or avoid conflicts with other MQ services by specifying a global prefix to be used for all Queue Names, e.g:

```csharp
QueueNames.SetQueuePrefix("site1.");

QueueNames<Hello>.In //= site1.mq:Hello.inq
```

Or to gain complete control of each queue name used, provide a custom QueueName strategy, e.g:

```csharp
QueueNames.ResolveQueueNameFn = (typeName, suffix) =>
    "SITE.{0}{1}".Fmt(typeName.ToLower(), suffix.ToUpper());

QueueNames<Hello>.In  //= SITE.hello.INQ
```

> Note: Custom QueueNames need to be declared on both MQ Client in addition to ServiceStack Hosts.  

# v4.10 Release Notes

## Debug Links

To provide better visibility to the hidden functionality in ServiceStack we've added **Debug Info** links section to the `/metadata` page which add links to any Plugins with Web UI's, e.g:

![Debug Info Links](http://i.imgur.com/2Hf3P9L.png)

The Debug Links section is only available in **DebugMode** (recap: set by default in Debug builds or explicitly with `Config.DebugMode = true`). In addition, users with the **Admin** role (or if `Config.AdminAuthSecret` is enabled) can also view the debug Plugins UI's in production.

You can add links to your own [Plugins](https://github.com/ServiceStack/ServiceStack/wiki/Plugins) in the metadata pages with:

```csharp
appHost.GetPlugin<MetadataFeature>().AddPluginLink("swagger-ui/", "Swagger UI");
appHost.GetPlugin<MetadataFeature>().AddDebugLink("?debug=requestinfo", "Request Info");
```

`AddPluginLink` adds links under the **Plugin Links** section and should be used if your plugin is publicly visible, otherwise use `AddDebugLink` for plugins only available during debugging or development.

## [Auto Mapping](https://github.com/ServiceStack/ServiceStack/wiki/Auto-mapping)

#### Improved Support for non-POCO types
Previously you could only map between top-level POCO models, now you can map between scalars and collections directly, e.g:

```csharp
var intVal = 2L.ConvertTo<int>();
var decimalVal = 4.4d.ConvertTo<decimal>();
var usersSet = new[] { new User(1), new User(2) }.ConvertTo<HashSet<User>>();
```

#### Improved Auto-Mapping Performance

A better caching strategy is used for conversions paths and now mapping fields utilize cached Delegate expressions so POCO's with fields Map much faster. 

## Async Support

#### Consistent handling of Async Responses

Previously Response Filters were called with the Task response returned from async services for the Response DTO, e.g. `Task<TResponse>`. The response filters are now chained to the task so Response filters see the same native `TResponse` DTO that are passed in from Sync services.

#### Async services can now be used in MQ Servers

Async responses now block for results which is in-line with sync Services behavior where Message Queue Handlers only process one message at a time for each worker thread assigned to the Request type.

## NuGet packages specify min versions

To ensure NuGet pulls the latest dependencies when installing any ServiceStack package, a minimum version is now specified for all NuGet package dependencies. This [should alleviate dependency issues](http://stackoverflow.com/a/21670294/85785) people are seeing from NuGet's default behavior of pulling down old packages. 

# v4.09 Release Notes

## Rabbit MQ Support

The biggest feature in this release is ServiceStack's new support for 
[hosting Services via a Rabbit MQ Server](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ), 
expanding on our existing [Redis MQ and In Memory messaging](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) options
with a new durable MQ option in the robust and popular [Rabbit MQ](http://www.rabbitmq.com). 
ServiceStack's Rabbit MQ support is available on NuGet with:

    PM> Install-Package ServiceStack.RabbitMq

A new [Rabbit MQ on Windows installation and setup guide](https://github.com/mythz/rabbitmq-windows) was published containing
code samples for working with Rabbit MQ from C#/.NET.

### Configurable Metadata Pages 

New customizable filters were added to the `MetadataFeature` plugin to allow customization of the Master and detail metadata pages before they're rendered.
E.g. you can reverse the order of operation names with:

```csharp
var metadata = (MetadataFeature)Plugins.First(x => x is MetadataFeature);
metadata.IndexPageFilter = page => {
    page.OperationNames.Sort((x,y) => y.CompareTo(x));
};
```

### OrmLite new runtime typed API 

The [IUntypedApi](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/IUntypedApi.cs) interface is useful for when you only have access to a late-bound object runtime type which is accessible via `db.CreateTypedApi`, e.g:

```csharp
public class BaseClass
{
    public int Id { get; set; }
}

public class Target : BaseClass
{
    public string Name { get; set; }
}

var row = (BaseClass)new Target { Id = 1, Name = "Foo" };

var useType = row.GetType();
var typedApi = db.CreateTypedApi(useType);

db.DropAndCreateTables(useType);

typedApi.Save(row);

var typedRow = db.SingleById<Target>(1);
typedRow.Name //= Foo

var updateRow = (BaseClass)new Target { Id = 1, Name = "Bar" };

typedApi.Update(updateRow);

typedRow = db.SingleById<Target>(1);
typedRow.Name //= Bar

typedApi.Delete(typedRow, new { Id = 1 });

typedRow = db.SingleById<Target>(1); //= null
```

#### OrmLite Create Table Support

  - Added NonClustered and Clustered options to `[Index]` attribute

## Breaking changes

### Messaging

In order to support Rabbit MQ Server some changes were made to 
[ServiceStack's Messaging API](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) to support all MQ options, namely:

  - `IMessageQueueClient` now exposes high-level `IMessage` API's instead of raw `byte[]`
  - The `IMessage.Error` property is now a `ResponseStatus` type (same used in Web Services)
  - **Ack** / **Nak** APIs were also added to `IMessageQueueClient`
  - All MQ Brokers now have a default `RetryCount=1`

### ServiceStack.Text

  - UrlEncode extension method now encodes spaces with `+` instead of `%20` to match default `HttpUtility.UrlEncode` behavior

### OrmLite

  - MySql and Sqlite providers now treat GUID's as `char(36)`

# v4.08 Release Notes

Added new [ServiceStack/Stripe](https://github.com/ServiceStack/Stripe) GitHub repository containing a PCL typed, message-based API client gateway for [Stripe's REST API](https://stripe.com/docs/api/). Install from NuGet with:

    Install-Package ServiceStack.Stripe

New in this release:

  - .NET 4.0 build of **ServiceStack.Razor** now available (in addition to .NET 4.5)
  - New **Signed** NuGet packages published for
    - [ServiceStack.Api.Swagger.Signed](https://www.nuget.org/packages/ServiceStack.Api.Swagger.Signed/)
    - [ServiceStack.OrmLite.Oracle.Signed](https://www.nuget.org/packages/ServiceStack.OrmLite.Oracle.Signed/)
  - Updated Swagger UI content files
  - Added MiniProfiler SqlServerStorage adapter to **ServiceStack.Server**
  - The [Razor Rockstars](https://github.com/ServiceStack/RazorRockstars/) and [Social Bootstrap Api](https://github.com/ServiceStack/SocialBootstrapApi/) projects have both been upgraded to v4

### OrmLite

  - Enums with `[Flag]` attribute (aka Enum flags) now stored as ints
  - `TimeSpan` now stores ticks as longs for all DB providers (Breaking change for Sqlite)

# v4.06 Release Notes

## Portable Class Library Clients!

The biggest feature of this release is the release of the new Portable Client NuGet packages:

[![Portable Class Library Support](https://raw2.github.com/ServiceStack/Hello/master/screenshots/portable-splash-900.png)](https://github.com/ServiceStack/Hello)

  - ServiceStack.Interfaces.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5, Silverlight5, WP8
  - ServiceStack.Client.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5
    - Custom builds: Silverlight 5
  - ServiceStack.Text.Pcl
    - PCL Profiles: iOS, Android, Windows8, .NET 4.5
    - Custom builds: Silverlight 5

This now allows sharing binaries between the above platforms. To illustrate this a new [Hello Repository](https://github.com/ServiceStack/Hello) was created to show how to use the same portable class libraries and DTO's across the different client platforms above.

#### Breaking Changes

Adding PCL support to the client libraries involved a lot of internal re-factoring which caused a few external user-facing changes:

  - The `IDbConnectionFactory` and `IHasDbConnection` interfaces referencing System.Data was moved to ServiceStack.Common
  - Properties exposing the concrete `NameValueCollection` are now behind an `INameValueCollection` interface
  - Dynamic classes like `DynamicJson` have been moved under the `ServiceStack` namespace

### Improved SOAP Support 

For maximum compatibility with different SOAP clients, SOAP Exceptions are now treated as "Soft HTTP Errors" where exceptions
are automatically converted to a **200 OK** but returns the original Status Code in the `X-Status` HTTP Response header or `X-Status` SOAP Header.

Errors can be detected by looking at the X-Status headers or by checking the **ResponseStatus.ErrorCode** property on the Response DTO. 
This is transparently handled in ServiceStack's built-in SoapClients which automatically converts Response Errors into populated 
C# WebServiceExceptions, retaining the same behavior of ServiceStack's other typed clients, as seen in 
[WebServicesTests](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/AlwaysThrowsService.cs#L162).

IHttpRequest.OperationName now reports the Request DTO name for SOAP requests as well, which it gets from the SOAPAction HTTP Header in SOAP 1.1 requests or the **Action** SOAP Header for SOAP 1.2 Requests.

# ServiceStack V4 Release Notes

We're happy to announce that after months of intense development, v4-beta of ServiceStack has finally been released to NuGet! 

As [announced in August](https://plus.google.com/+DemisBellot/posts/g8TcZaE7bv9) to ensure it's continued development, ServiceStack has moved to a self-sustaining commercial model for commercial usage of ServiceStack from **v4+ onwards**. It's the first time we've been able to commit full-time resources to the project and is what has ensured continued investment and enabled v4 to be possible, with even more exciting features in the pipeline and roadmap for 2014.

## [Introductory Offer](https://servicestack.net/pricing)

For our early supporters we're launching the new [servicestack.net](https://servicestack.net) website with [attractive introductory pricing](https://servicestack.net/pricing) available during the beta between **33-40% off** royalty-free/per-developer perpetual licensing and **20% off** our unlimited-developers/per-core subscriptions. There's also an additional **60 days free** maintenance and updates covering the beta period, available in 2013. These discounts are intended to be grandfathered-in and carried over for any future renewals, making the v4-beta the best time to get ServiceStack. For US Customers we also have free ServiceStack T-Shirts and stickers whilst stocks last - If you'd like them, add your preferred T-Shirt sizes in the Order notes.

#### Free Usage for Small and OSS Projects

We're also happy to announce that v4 includes [free quotas](https://servicestack.net/download#free-quotas) allowing the free usage of all of ServiceStack for small projects and evaluation purposes. Whilst OSS projects are able to use the source code on GitHub under the [AGPL/FOSS License Exception](https://github.com/ServiceStack/ServiceStack/blob/master/license.txt), and the older [v3 of ServiceStack](https://github.com/ServiceStackV3/ServiceStackV3) continues to be available under the [BSD license](https://github.com/ServiceStack/ServiceStack/blob/v3/LICENSE).

#### Upgrading from v3

Whilst we recommend starting with **v4** for greenfield projects, v4 has seen significant changes since v3 that will require some development effort to upgrade. During the upgrade we recommend using a tool like [ReSharper](http://www.jetbrains.com/resharper/) to be able to easily find and update reference of any types that have moved.

# What's new in v4

The major version upgrade of ServiceStack to v4 has provided us a long sought **breaking window** opportunity allowing us to re-factor, simplify, clean-up and fix all the warts and cruft that has been lingering in the ServiceStack code-base since its beginning - that due to backwards compatibility we were reluctant to remove. Whilst v4 has seen significant changes to the code-base, all existing tests are passing again with additional tests added for new functionality. 

We managed to retain a lot of the user-facing API's (E.g New API, AppHost, Config) which were already considered ideal so ideally upgrading shouldn't be too disruptive in the normal use-cases. 

v4 provides us a great foundation to build on that will be further improved during the beta by focusing on stability and fixing any reported issues as well as updating existing documentation to match v4's implementation and publish new examples showcasing v4's new features.

## The big refactor of v4

This was the biggest re-factor in ServiceStack's history which at the end resulted in a much leaner, simplified, consistent and internal logically-structured code-base that's much easier to reason about, where even before adding any features the main ServiceStack repository saw:

    1,192 changed files with 18,325 additions and 29,505 deletions. 

The number of deletions is indicative of how much legacy code was able to be removed, with much of the internals having been heavily restructured. Some of the highlights during the re-factor include: 

  - All projects have been upgraded to .NET 4.0, except ServiceStack.Razor which is .NET 4.5 to use the latest version of Razor
  - All obsolete/unused/shims/duplicate functionality and built-up cruft has now been removed (inc. the Old Api). 
  - State and configuration are now cohesively organized where now all AppHost's share the same `ServiceStackHost` base class which now maintains all state in ServiceStack, inc. the empty `BasicAppHost` that's used for unit testing which now shares much of the same state/context as Integration tests.
  - Many namespaces and some concepts have been collapsed (e.g 'Endpoint'), resulting in ServiceStack projects requiring fewer namespaces
  - All DTO's and extension methods and common user-facing classes have been moved to the base `ServiceStack` namespace - allowing them to be much easier to find. 
  - Re-organization of projects, **NuGet packages now map 1:1 with ServiceStack projects** for finer-grained control of dependencies:
    + **ServiceStack.Interfaces** NuGet project created and ServiceInterface has been merged into **ServiceStack**
    + **ServiceStack** NuGet package now only depends **ServiceStack.Common** and **ServiceStack.Client**
    + A new **ServiceStack.Server** project exists for functionality requiring dependencies on OrmLite or Redis, inc. RedisMqServer and OrmLiteAuthRepository. 
    + **ServiceStack.Client** contains all the HTTP, SOAP and MQ Service Clients that have been split from **ServiceStack.Common** and only depends on ServiceStack.Interfaces and ServiceStack.Text (making it easier to maintain custom builds in future).
  - EndpointHostConfig is now `HostConfig` and is limited to just Configuration, e.g. handlers like `CustomErrorHttpHandlers`, `RawHttpHandlers`, `GlobalHtmlErrorHttpHandler` have been moved to ServiceStackHost. 
  - EndpointHost is gone and replaced by the static `HostContext` class which doesn't contain any state itself, it's just a static convenience wrapper around `ServiceStackHost.Instance` (where all state is maintained). 
  - Removed all 'where T:' constraints where possible
  - Removed `ConfigurationResourceManager`, use `AppSettings` instead
  - The `ServiceStack.WebHost.Endpoints.ServiceStackHttpHandlerFactory` used in Web.config's handler mapping has been renamed to just `ServiceStack.HttpHandlerFactory`
  - `Config.ServiceStackHandlerFactoryPath` has been renamed to `Config.HandlerFactoryPath`.
  - Predefined routes have been renamed from `/syncreply`, `/asynconeway` to just `/reply`, `/oneway`
  - ServiceManager has been merged into `ServiceController`. 
  - The **ServiceStack.Logging** and **ServiceStack.Contrib** v4 projects have been merged into the major ServiceStack repo.
  - The dynamic session `base.Session` has been renamed to `base.SessionBag` to better reflect its semantics.
  - The [[Auto Mapping]] Utils extension methods were renamed from `TFrom.TranslateTo<T>()` to `TFrom.ConvertTo<T>()`.
  - The `RequestFilters` and `ResponseFilters` were renamed to `GlobalRequestFilters` and `GlobalResponseFilters` which matches naming in the client `ServiceClientBase.GlobalRequestFilter`.
  - New `GlobalMessageRequestFilters` and `GlobalMessageResponseFilters` have been added which are instead used by non-HTTP endpoints use, e.g. MQ. 
  - `CustomHttpHandlers` has been renamed to `CustomErrorHttpHandlers`
  - The **LocalHttpWebRequestFilter** and **LocalHttpWebResponseFilter** in the Service Clients were renamed to just `RequestFilter` and `ResponseFilter`
  - The Global **HttpWebRequestFilter** and **HttpWebResponseFilter** filters were also renamed to `GlobalRequestFilter` and `GlobalResponseFilter` respectively.
  
### RequestContext now merged into new IRequest / IResponse classes:

An annoyance remaining in the ServiceStack code-base was RequestContext and its relationship with its IHttpRequest and IHttpResponse classes. This was originally modeled after ASP.NET's relationship with HttpContext and its child HttpRequest/HttpResponse classes. Pragmatically speaking this model isn't ideal, as there was functionality spread across all 3 classes, many times duplicated. It was also not obvious how to retrieve IHttpRequest/IHttpResponse classes from a RequestContext and creating a RequestContext from outside of ServiceStack required more knowledge and effort than it should have. 

The new model adopts a flattened structure similar to Dart's server HttpRequest (http://bit.ly/19WUxLJ) which sees the `IRequestContext` eliminated in favour of a single `IRequest` class that also makes available direct access to the Response.

This now becomes much easier to create a Request from outside of ServiceStack with an ASP.NET or HttpListener HttpContext e.g:

```csharp
var service = new MyService {
    Request = HttpContext.Current.ToRequest()
}

var service = new MyService {
    Request = httpListenerContext.ToRequest()
}
```

There's also direct access to the Response from a Request with:

```csharp
IResponse response = Request.Response;
```

#### ASP.NET wrappers now only depends on HttpContextBase

Also the ASP.NET `IHttpRequest` wrappers bind to the newer and mockable HttpContextBase / HttpRequestBase / HttpResponseBase classes which now makes it easier to call services from newer web frameworks like MVC with:

```csharp
var service = new MyService {
    Request = base.HttpContext.ToRequest()
}
```

The biggest user-facing change was renaming the IHttpRequest/IHttpResponse classes to IRequest/IResponse which is more indicative to what they represent, i.e. the Request and Response classes for all endpoints including MQ and future TCP endpoints. Now only HTTP Requests implement IHttpRequest/IHttpResponse which lets you add logic targeting only HTTP Services with a simple type check:

```csharp
var httpReq = request as IHttpRequest;
if (httpReq != null) {
    //Add logic for HTTP Requests...
}
```

Accessing the IHttpResponse works the same way, e.g:

```csharp
var httpRes = Request.Response as IHttpResponse;
if (httpRes != null) {
    //...
}
```

We're still going to add extension methods on IRequest/IResponse to make it easier to discover new functionality, but for HTTP functionality on non-HTTP requests these would just be a NO-OP rather than throw an exception.

### Community v4 migration notes

  - [Upgrading Servicestack to 4.0 – Notes](http://www.binoot.com/2014/02/23/upgrading-servicestack-to-4-0-notes/) by [@binu_thayamkery](https://twitter.com/binu_thayamkery)
  - [Upgrading OrmLite and ServiceStack to v4](http://camtucker.blogspot.ca/2014/01/updating-to-servicestack-v40.html?view=classic) by [@camtucker](http://camtucker.blogspot.ca/)

----  

# New Features in v4


## Server-side Async Support

The [most requested feature](http://bit.ly/16qCiy1), Server-side async support has now been implemented! This was surprisingly easy to do where now all HttpHandlers in ServiceStack inherit from a common `HttpAsyncTaskHandler` base class that now implements `IHttpAsyncHandler`. This lets you return an async Task from your Service in any number of ways as shown in http://bit.ly/1cOJ3hR 

E.g. Services can now have either an object, Task or async Task return types that can return a started or non-started task (which we'll start ourselves). This transition went as smooth as it could where all existing services continuing to work as before and all tests passing.

## [ServiceStack Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) Task-based Async

In matching the new server-side async story and now that all projects have been upgraded to .NET 4.0, all Service Clients have been changed to return .NET 4.0 Task's for all async operations so they can be used in C#'s async/await methods. Some examples of Async in action: http://bit.ly/17ps94C

The Async API's also provide a **OnDownloadProgress** callback which you can tap into to provide a progress indicator in your UI, E.g: http://bit.ly/19ALXUW

#### Use any Request DTO in Client API's

ServiceClient API's that used to only accept Request DTO's with a `IReturn` marker, now have `object` overloads so they can be used for unmarked Request DTO's as well.

### Custom Silverlight and Android builds

We've added custom **Silverlight** and **Android** automated builds for ServiceStack.Client allowing the client libraries to be available in even more environments - with more to follow.

## Signed NuGet Packages

The following Signed NuGet packages are available for core ServiceStack projects in separate NuGet packages using the .Signed suffix:

  - ServiceStack.Client.Signed
  - ServiceStack.Text.Signed
  - ServiceStack.Redis.Signed
  - ServiceStack.OrmLite.Signed
  - ServiceStack.OrmLite.SqlServer.Signed
  - ServiceStack.ServiceStack.Signed
  - ServiceStack.ServiceStack.Razor.Signed
  - ServiceStack.ServiceStack.Server.Signed
  - ServiceStack.Common.Signed

### ServiceStack.Interfaces is now strong-named

In order to be able to have signed clients sharing types with non-signed ServiceStack instances, the DTO models and ServiceStack.Interfaces need to be signed. It was added in the most defensive way possible where **ServiceStack.Interfaces.dll** is the only dll that's strong-named by default. This should cause minimal friction as it is an impl-free assembly that rarely sees any changes. We're also keeping the AssemblyVersion which makes up the strong-name at a constant `4.0` whilst the benign AssemblyFileVersion will report the true version number. 

### Add Code-first Attributes at runtime, de-coupled from POCO's 

Inspection of all Metadata attributes in ServiceStack now uses ServiceStack.Text's attribute reflection API's which support adding of type and property metadata attributes dynamically. This now lets you add the same behavior normally only available via attributes, dynamically at StartUp. Some benefits of this include: being able to keep [unattributed data model POCOs in OrmLite](http://bit.ly/1e5IQqS) or to [extend built-in and external Request DTOs and Services](https://github.com/ServiceStack/ServiceStack/blob/d93ad805c8c8ffce8e32365e4217c65c19069cf0/tests/ServiceStack.WebHost.Endpoints.Tests/RuntimeAttributeTests.cs) with enhanced functionality that was previously only available using attributes.

#### Fluent route configuration available in [Reverse Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing)

Leveraging the dynamic attribute support, we now include fluent Route definitions when retrieving relative or absolute urls in [Reverse Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing), which can be used in Services when returning urls in responses and is also used in Service Clients to determine which routes to use. Note: as Fluent Routes are defined in the AppHost, they aren't registered and therefore not available in disconnected .NET client applications - so using `[Route]` attributes on Request DTO's remains the best way to share route definitions on both client and server.

Priority was added to `[Route]` attributes so auto-generated routes are given less precedence than explicit user-defined custom routes when selecting the best matching route to use.

### The Virtual FileSystem

The Virtual FileSystem is now fully integrated into the rest of ServiceStack, this enables a few interesting things:

  - The `Config.WebHostPhysicalPath` sets where you want physical files in ServiceStack to be served from
  - You can now access static files when ServiceStack is mounted at a custom path, e.g. /api/default.html will serve the static file at ~/default.html
  - By Default, ServiceStack falls back (i.e when no physical file exists) to looking for Embedded Resource Files inside dlls. 
  - You can specify the number and precedence of which Assemblies it looks at with `Config.EmbeddedResourceSources` which by default looks at:
    - The assembly that contains your AppHost
    - **ServiceStack.dll**

The VFS now elegantly lets you replace built-in ServiceStack templates with your own by simply copying the metadata or [HtmlFormat Template files](http://bit.ly/164YbrQ) you want to customize and placing them in your folder at:

    /Templates/HtmlFormat.html        // The auto HtmlFormat template
    /Templates/IndexOperations.html   // The /metadata template
    /Templates/OperationControl.html  // Individual operation template

This works because the ServiceStack.dll is the last assembly in `Config.EmbeddedResourceSources`.

## API-first development

We're starting to optimize ServiceStack's HTML story around an **API-first** style of web development (particularly well suited to ServiceStack) in which services are developed so they naturally support both web and native clients from the start. Effectively this means that the HTML views are just another client that escapes C# earlier and leverages JS+Ajax to provide its dynamic functionality, and any HTML-specific functionality is encouraged to be kept in Razor views rather than using post backs to generate different server-side HTML representations. 

Having developed the new [servicestack.net website](https://servicestack.net) in this way, we've found it to be a lot more productive and responsive than standard server-side MVC development that we we're accustomed to in .NET as JavaScript ends up being more mallable and flexible language with a smaller and reflective surface type area making it better suited in string manipulation, generating HTML views, consuming ajax services, event handling, DOM binding and manipulation, etc. 

We've begun taking advantage of the Virtual FileSystem to ship embedded resources enhancing ServiceStack's JS integration with client-side libraries like [ss-utils.js](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/js/ss-utils.js) that we maintain and update alongside the rest of ServiceStack's dlls. Whilst we intend to create more examples in the near future showcasing this functionality, here's an overview of what's been added:

  - ss-utils.js available in your local ServiceStack webhost at `/js/ss-utils.js`
  - Inspired by AngularJS we've added **declarative** support over jQuery, letting you declaratively register and trigger events, bind values to HTML elements, register document handlers, etc, saving a lot of boilerplate than normal jQuery (more on this soon)
  - Enhanced HTML forms with integration with ServiceStack validation, adds responsive UX/behavior, follows soft redirects
  - Server-side responses can be decorated with Soft redirects with `HttpResult.SoftRedirect` or client events with `HttpResult.TriggerEvent`
  - Use `("a").setActiveLinks()` to automatically set the active link and containing menu items for the current page
  - Use `$("input").change($.ss.clearAdjacentError)` to clear highlighted errors as users correct their inputs
  - Use `T.AsRawJson()` extension method to serialize C# models into JSON literals that are natively accessible in JS
  - Use `T.ToGetUrl()`, `T.ToPostUrl()` to resolve service urls from typed Request DTOs (no code-gen required)

## Improved Razor Support

#### Improved Server-side validation

The server-side validation story has also been improved with MVC's HTML INPUT and Validation Helpers rewritten to look at ServiceStack error responses (earlier lost in the upgrade to Razor 2.0) and making use of the same bootstrap conventional classes that the client-side Ajax validation uses letting you maintain a single style of error feedback for both validation styles. It also now looks at state contained in the POST'ed data when rendering the HTML INPUT controls.
  
#### Fallback Routes

The default Razor views are now also processed by the `FallbackRoute` if one exists, enhancing the story for Single Page Apps who want requests to un-specified routes to be handled by client-side routing instead of returning 404's.

#### Pre-Request filters

Direct (i.e. No Controller) Razor views and static file handlers now have pre-request filters applied to them, so they can be used for adding global behavior across all ServiceStack service and page requests.

#### Precompilation option for Razor Views

New options have been added to RazorFormat `PrecompilePages` and `WaitForPrecompilationOnStartup` that allow you to precompile razor views on startup and specify whether or not you want to wait for compilation to complete are now options available when registering the `RazorFormat`. As these can slow down dev iteration times they are not done when `Config.DebugMode` (aka development mode), but are otherwise enabled by default for production.

#### Other Razor Improvements

  - More functionality was added to Razor Views matching the same API's available in ServiceStack's base `Service` class
  - RenderSection/IsSectionDefined now looks in all connected views.
  - `GetAbsoluteUrl`, `IsPostBack`, `GetErrorStatus()`, `GetErrorMessage()` convience methods added 

### CORS Feature

CorsFeature now by default automatically handles all HTTP `OPTIONS` requests so you no longer have to explicitly allow for OPTION requests in your routes: http://bit.ly/19HbMVf

Can be disabled with: 

```csharp
Plugins.Add(new CorsFeature { AutoHandleOptionsRequests = false })
```

## Authentication

The Auth Tables are now called **UserAuth** and **UserAuthDetails** and implements the IUserAuth and IUserAuthDetails interfaces. For advanced customization, these tables can now be extended using custom models inheriting these interfaces by using the generic AuthRepository types, e.g:

  - OrmLiteAuthRepository<TUserAuth, TUserAuthDetails>
  - RedisAuthRepository<TUserAuth, TUserAuthDetails>

Where the common non-generic **OrmLiteAuthRepository** is just a concrete impl inheriting from `OrmLiteAuthRepository<UserAuth, UserAuthDetails>`. Use `InitSchema()` to ensure missing Auth Tables are created at registration.
  
#### New optional UserAuthRole table added

A new `UserAuthRole` class was created for users who would prefer roles to be managed in separate tables rather than blobbed with the UserAuth table and session. E.g. You can change your custom session to check the database for asserting required users and permissions with:

```csharp
public class CustomUserSession : AuthUserSession
{
    public override bool HasRole(string role)
    {
        using (var db = HostContext.TryResolve<IDbConnectionFactory>().Open())
        {
            return db.Count<UserAuthRole>(q => 
                q.UserAuthId == int.Parse(UserAuthId) && q.Role == role) > 0;
        }
    }

    public override bool HasPermission(string permission)
    {
        using (var db = HostContext.TryResolve<IDbConnectionFactory>().Open())
        {
            return db.Count<UserAuthRole>(q => 
                q.UserAuthId == int.Parse(UserAuthId) && q.Permission == permission) > 0;
        }
    }
}
```

#### Support for Max Login Attempts 

The `OrmLiteAuthRepository` now supports automatically locking out user accounts after reaching the maximum number of Login attempts which can be specified at registration, e.g:

```csharp
container.Register<IAuthRepository>(c =>
    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
        MaxLoginAttempts = appSettings.Get("MaxLoginAttempts", 5)
    });
```

To opt-in to use the new locking behavior provide a value for `MaxLoginAttempts` as shown above. The above registration first uses the value overridable in appSettings if it exists, otherwise it defaults to a Maximum of 5 login attempts. 

#### Adhoc locking of User Accounts

The `CredentialsAuthProvider` also supports locking user accounts by populating the `UserAuth.LockedDate` column with a non-null value. Set it back to null to unlock the account.

#### Initializing Auth Repository Schemas
    
Some Auth Repositories like OrmLite require an existing schema before they can be used, this can be done in the AppHost with:

```csharp
//Create missing Auth Tables in any Auth Repositories that need them
container.Resolve<IAuthRepository>().InitSchema(); 
```
This was previously named `CreateMissingTables()` and is safe to always run as it's a NO-OP for Auth repositories that don't require a schema and only creates missing tables, so is idempotent/non-destructive on subsequent runs.

#### New AuthWeb Test project

A new test project testing all Authentication providers within the same ServiceStack ASP.NET Web Application is in [ServiceStack.AuthWeb.Tests](https://github.com/ServiceStack/ServiceStack/tree/master/tests/ServiceStack.AuthWeb.Tests).

### AppSettings

AppSettings can now be passed a tier in the constructor, e.g. `new AppSettings(tier: "Live")` which it uses as a prefix to reference Tier-specific appSettings first, e.g:

    <add key="Live.AppDb" value="..." />
    
Before falling back to the common key without the prefix, i.e:

    <add key="AppDb" value="..." />

AppSettings now allows a Parsing Strategy, e.g. You can collapse new lines when reading a complex configuration object in Web.Config `<appSettings/>` with:

```csharp
var appSettings = new AppSettings { 
    ParsingStrategy = AppSettingsStrategy.CollapseNewLines 
};
```

### Nested Request DTOs

Using [nested types as Request DTO's](https://github.com/ServiceStack/ServiceStack/commit/376ca38f604214f4d12e2f7803d8e7cfc271b725) are now supported.
Nested Request DTO types include the names of their containing class to form their unique name, allowing the use of multiple nested types with the same name, which is potentially interesting to be used as a versioning strategy.

### Localized symbols

I've added `IAppHost.ResolveLocalizedString` support in [this commit](http://bit.ly/181q0eP) which lets you override the built-in English symbols used in ServiceStack, e.g. this lets you change built-in ServiceStack routes, e.g: `/auth`, `/assignroles`, `?redirect=`, etc. into something more appropriate for your language. 

## Other New Web Framework Features

  - Added convenient [Repository and Logic base classes](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/ILogic.cs) to reduce boilerplate when extracting logic from services into custom classes
  - Added `IAppHost.OnExceptionTypeFilter` to be able to customize ResponseStatus based on Exception types. Used to change the [default behavior of ArgumentExceptions](https://github.com/ServiceStack/ServiceStack/commit/17985239ed6f84b3126c651dbacd0c760a4d2951) so that they're converted to field errors
  - Added `IAppHost.OnServiceException` so service exceptions can be intercepted and converted to different responses
  - Add `ConvertHtmlCodes` extension method converting HTML entities to hex-encoded entities
  - Add `Config.ScanSkipPaths` option to skip any plugins using the VFS to scan the filesystem (e.g. Razor/Markdown feature) from scanning specified directories, `/bin/` and `/obj/` are added by default.
  - Added a pre-defined `/swagger-ui/` route that loads the Swagger UI and auto configures it to look at ServiceStack services. A link to this is on the metadata page under **Plugin Links** heading.
  - Added `ModelFilter` and `ModelPropertyFilter` to allow fine-grained custom control on what's displayed in the Swagger API
  - Wrappers around .NET's JSON and XML DataContract Serializers now share the same `IStringSerializer` interface
  - Added ToMsgPack/FromMsgPack and ToProtoBuf/FromProtoBuf extension methods
  - Improved support for stripping App Virtual Paths when Resolving Absolute Urls useful when applications are hosted with virtual app paths as done in Amazon Web Services. This behavior can be enabled with `Config.StripApplicationVirtualPath = true`.
  - Support for explicitly referencing ignored DTO properties in Route PathInfo definitions, but not QueryStrings
  - Add support for getting Id property from runtime object type
  - Added support for registering a singleton instance as a runtime type
  - Added new [IRestGateway](https://github.com/ServiceStack/ServiceStack/commit/29d60dfa22424fe20ba35c8603686c05f88a6c25) interface that typed 3rd Party gateways can use to retain a consistent and mockable interface. Specialized MockRestGateway added to stub or mock out gateways to 3rd Party services
  - `__requestinfo` is now available on any request with `?debug=requestinfo` and is accessible to administrators or when in **DebugMode**, and provides in-depth diagnostics about the details of the current request and the configured AppHost including Startup errors (if any).
  - Plugins can register startup exceptions with `IAppHost.NotifyStartupException`
  - Added new HTTP Headers on IHttpRequest for `XForwardedPort` and `XForwardedProtocol`
  - Added `[EnsureHttps]` Request Filter to automatically redirect request if service was not requested under a Secure Connection with options to **SkipIfDebugMode** or **SkipIfXForwardedFor** to allow local development and requests via proxies / load-balancers in HTTP.
  - Users in the **Admin** role have super-user access giving them access to all protected resources. You can also use `Config.AdminAuthSecret` to specify a special string to give you admin access without having to login by adding `?authsecret=xxx` to the query string.

-----

## OrmLite

### Improved Consistency

As the API surface of OrmLite expands it became a lot more important to focus on better consistency which now sees all alternative aliases having been removed in favor of standardized naming around SQL equivalents (e.g Select,Insert,etc). Also the parameterized APIs and the C#-like string.Format API's have now been merged with the parameterized APIs now being the default and the string.Format API having a 'Fmt' suffix. 

Most of these APIs now have XML docs and Examples for a better Intelli-sense experience. We've also provided them in a list along side it's generated SQL in [these API tests](http://bit.ly/1gmrnwe)

Some notes:

  - `Select` returns a List
  - `Single` returns 1 row (or null) if it doesn't exist
  - `Scalar` returns a single a scalar value (e.g. int, long)
  - `Where` is a short-hand for 'Select' that takes a single filter
  - `Count` is a convenience that performs an aggregate SQL Count
  - `Exists` returns true if there were any results
  - `Lazy` suffix indicates the results are lazily streamed
  - `Column` returns the first column results in a List
  - `ColumnDistinct` returns the first column unique results in a HashSet
  - `Dictionary` returns a Dictionary made up from the first 2 columns
  - `Lookup` returns a LINQ-like grouping in a Dictionary<K, List<V>>
  - `NonDefaults` suffix indicates only non-null values are used in qry
  - `Only` suffix allows you to specify fields used on the call-site
  - `Sql` prefix are helpers for reading and querying arbitrary raw SQL
  - `Save` is a convenience that inserts or updates depending if it exists or not. It also now populates AutoIncrementing Id's on Inserts.
  - All batch operations like `InsertAll`, `UpdateAll`, `DeleteAll`, `SaveAll` participate in an existing transaction if 1 exists, otherwise a new one
  - Removed all 'where T:' constraints where possible
  - `OrDefault` APIs removed, All APIs now return null instead of throwing
  - autoDisposeConnection removed. false for ":memory:" otherwise true
  - Now that all OrmLite's parameterized `Query*` APIs have been merged (above), any `Query` APIs are from Dapper's extension method, which is also included in OrmLite under ServiceStack.OrmLite.Dapper namespace.
  - All remaining OrmLIte Attributes have been moved to ServiceStack.Interfaces, which in future will be the only dependency needed by your data models and DTOs.

### OrmLite extension methods are now mockable

OrmLite API's can now be mocked by injecting a ResultsFilter letting you mock the results return by OrmLite which it will use instead of hitting the database. You can also mock with a filter function and it also supports nesting, see examples at: http://bit.ly/1aldecK

This will be useful in Unit Testing Services that access OrmLite directly instead of using a repository.﻿

### Support for references, POCO style

We've added a cool new feature to Store and Load related entities that works great on POCO which are enabled when you use the `[Reference]` attribute, e.g: http://bit.ly/1gmvtV6

Unlike normal complex properties in OrmLite:

  - Doesn't persist as complex type blob
  - Doesn't impact normal querying
  - Saves and loads references independently from itself
  - Populated references get serialized in Text serializers (only populated are visible).
  - Data is only loaded 1-reference-level deep
  - Reference Fields require consistent `(T)Id` naming
 
Basically it provides a better story when dealing with referential data that doesn't impact the POCO's ability to be used as DTO's. At the moment it's limited to loading and saving on a Single instance. We'll look at optimizations for batches on this in future.﻿ 

We're going to be giving OrmLite a lot more attention from now on given that we're working full-time on ServiceStack and are using it exclusively for our .NET RDBMS peristence. We also intend on adding specialized support to take advantage of PostgreSQL's new features like their HStore and native JSON support. PostgreSQL has been offering the best features of both RDBMS and NoSQL worlds lately and has recently become a particularly attractive option now that AWS is offering first-class support for PostgreSQL in both their RDS and Redshift services.

-----
## ServiceStack.Text

  - Allow adding metadata attributes to types or attributes at runtime
  - Add JsConfig.ExcludeTypes option to skip serialization of non-serializable properties like Streams
  - Change QueryString's to also adopt the configured `JsConfig.PropertyConvention` and `JsConfig.EmitLowercaseUnderscoreNames` behavior
  - Added an injectable ComplexTypeStrategy to the QueryStringSerializer that allows customizing the generation of complex  types, e.g. can use a hash literal notation strategy with `QueryStringStrategy.FormUrlEncoded`.
  - Added `typeof(T).New()` extension method providing a fast way of creating new instances of static or runtime types that will use factory functions registered in the centralized `JsConfig.ModelFactory` (if configured).
  - The string "on" (i.e. the default value for HTML checkbox) is considered a **true** value for booleans (same with '1')
  - The JSON serializers can be configured to support UnixTime and UnixTimeMs for DateTime's
  - Renamed JsonDateHandler to `DateHandler` and JsonPropertyConvention to `PropertyConvention`

### HTTP Utils are now mockable

Following in the steps of now being able to Mock OrmLite, the [HTTP Utils](https://github.com/ServiceStack/ServiceStack/wiki/Http-Utils) extension methods (http://bit.ly/WyV2tn) are now mockable, e.g:

    using (new HttpResultsFilter {
        StringResult = "mocked"
    })
    {
        //All return "mocked"
        "http://google.com".GetJsonFromUrl();
        "http://google.com".GetXmlFromUrl();
        "http://google.com".GetStringFromUrl(accept: "text/csv");
        "http://google.com".PostJsonToUrl(json: "{\"postdata\":1}");
    }

More examples showing how all HTTP Apis can be mocked are at: http://bit.ly/HdWmgm

-----

## New pre-release MyGet Feeds

Instead of publishing pre-release packages on NuGet, we're instead going to release our interim packages to [MyGet](https://www.myget.org/) first which provides greater control and allows better management of packages.

The Instructions to add ServiceStack's MyGet feed to VS.NET are:

  1. Go to Tools -> Options -> Package Manager -> Package Sources
  2. Add the Source **https://www.myget.org/F/servicestack** with the name of your choice, e.g. _ServiceStack MyGet feed_

-----

## [Older v3 Release Notes](https://github.com/ServiceStack/ServiceStack/wiki/Release-Notes-v3)