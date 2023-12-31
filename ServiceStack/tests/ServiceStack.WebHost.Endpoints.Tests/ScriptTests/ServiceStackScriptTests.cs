using System.Collections.Generic;
using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Formats;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests;

public class QueryProducts : QueryData<Product> {}
    
public class GetAllProducts : IReturn<GetAllProductsResponse> {}

public class GetAllProductsResponse
{
    public Product[] Results { get; set; }
}

public class TemplateServiceStackFiltersService : Service
{
    public object Any(GetAllProducts request) => new GetAllProductsResponse
    {
        Results = QueryData.Products
    };
}
    
public class QueryTemplateRockstars : QueryDb<Rockstar> {}
    
public class QueryCustomers : QueryDb<Customer> 
{
    public string CustomerId { get; set; }
    public string CompanyNameContains { get; set; }
    public string[] CountryIn { get; set; }
}
    
public class ServiceStackScriptTests
{
    class AppHost() : AppSelfHostBase(nameof(SharpPagesIntegrationTests), typeof(MyTemplateServices).Assembly)
    {
        public readonly List<IVirtualPathProvider> TemplateFiles = 
        [
            new MemoryVirtualFiles(),
            new ResourceVirtualFiles(typeof(HtmlFormat).Assembly)
        ];
        public override List<IVirtualPathProvider> GetVirtualFileSources() => TemplateFiles;

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = true
            });

            container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(":memory:",
                SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(UnitTestExample.SeedData);
                    
                db.DropAndCreateTable<Customer>();
                db.InsertAll(QueryData.Customers);
            }

            Plugins.Add(new SharpPagesFeature
            {
                ApiPath = "/sharpapi",
                Args =
                {
                    ["products"] = QueryData.Products,
                },
                ScriptMethods =
                {
                    new DbScriptsAsync(),
                    new AutoQueryScripts(),
                },
            });
                
            Plugins.Add(new AutoQueryDataFeature { MaxLimit = 100 }
                .AddDataSource(ctx => ctx.ServiceSource<Product>(ctx.ConvertTo<GetAllProducts>()))
            );
                
            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

            var files = TemplateFiles[0];
                
            files.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");
            files.WriteFile("autoquery-data-products.html", @"
{{ 'category,orderBy,take' |> importRequestParams }}{{ { category, orderBy, take } |> withoutNullValues |> sendToAutoQuery('QueryProducts') 
   |> toResults |> select: { it.ProductName }\n }}");

            files.WriteFile("autoquery-rockstars.html", @"
{{ { qs.age, qs.orderBy, qs.take } |> withoutNullValues |> sendToAutoQuery('QueryTemplateRockstars') 
   |> toResults |> select: { it.FirstName } { it.LastName }\n }}");

            files.WriteFile("autoquery-customer.html", @"
{{ { qs.customerId } |> sendToAutoQuery('QueryCustomers') 
     |> toResults |> select: { it.CustomerId }: { it.CompanyName }, { it.City }\n }}");

            files.WriteFile("autoquery-customers.html", @"
{{ { qs.countryIn, qs.orderBy } |> sendToAutoQuery('QueryCustomers') 
     |> toResults |> select: { it.CustomerId }: { it.CompanyName }, { it.Country }\n }}");

            files.WriteFile("autoquery-top5-de-uk.html", @"
{{ { countryIn:['UK','Germany'], orderBy:'customerId', take:5 } |> sendToAutoQuery('QueryCustomers') 
     |> toResults |> select: { it.CustomerId }: { it.CompanyName }, { it.Country }\n }}");
                
            files.WriteFile("sharpapi/customers.html", @"
{{ 'id,city,country' |> importRequestParams }}
{{ qs.limit ?? 100   |> assignTo: limit }}

{{ 'select CustomerId, CompanyName, City, Country from Customer' |> assignTo: sql }}

{{ PathArgs |> endIfEmpty |> useFmt('{0} where CustomerId = @id', sql) |> dbSingle({ id: PathArgs[0] }) 
            |> return }}

{{ id       |> endIfEmpty |> use('CustomerId = @id')   |> addTo: filters }}
{{ city     |> endIfEmpty |> use('City = @city')       |> addTo: filters }}
{{ country  |> endIfEmpty |> use('Country = @country') |> addTo: filters }}
{{ filters  |> endIfEmpty |> useFmt('{0} where {1}', sql, join(filters, ' and ')) |> assignTo: sql }}

{{ sql      |> appendFmt(' ORDER BY CompanyName {0}', sqlLimit(limit)) 
            |> dbSelect({ country, city, id }) 
            |> return }}
");
        }
    }

    public static string BaseUrl = Config.ListeningOn;
        
    private readonly ServiceStackHost appHost;
    public ServiceStackScriptTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(BaseUrl);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_call_AutoQuery_Data_services()
    {
        var html = BaseUrl.CombineWith("autoquery-data-products").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Chai
Chang
Aniseed Syrup".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_Data_services_with_limit()
    {
        var html = BaseUrl.CombineWith("autoquery-data-products?orderBy=ProductName&take=3").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Alice Mutton
Aniseed Syrup
Boston Crab Meat


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_Data_services_with_category()
    {
        var html = BaseUrl.CombineWith("autoquery-data-products?category=Beverages").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

Chai
Chang
Guaran&#225; Fant&#225;stica
Sasquatch Ale
Steeleye Stout
C&#244;te de Blaye
Chartreuse verte
Ipoh Coffee
Laughing Lumberjack Lager
Outback Lager
Rh&#246;nbr&#228;u Klosterbier
Lakkalik&#246;&#246;ri


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_Db_services()
    {
        var html = BaseUrl.CombineWith("autoquery-rockstars").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Jimi Hendrix
Jim Morrison
Kurt Cobain
Elvis Presley
David Grohl
Eddie Vedder
Michael Jackson


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_Db_services_with_limit()
    {
        var html = BaseUrl.CombineWith("autoquery-rockstars?orderBy=FirstName&take=3").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

David Grohl
Eddie Vedder
Elvis Presley


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_Db_services_by_age()
    {
        var html = BaseUrl.CombineWith("autoquery-rockstars?age=27&orderBy=LastName").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

Kurt Cobain
Jimi Hendrix
Jim Morrison


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_QueryCustomer_service_by_CityIn()
    {
        var html = BaseUrl.CombineWith("autoquery-customers")
            .AddQueryParam("countryIn","UK,Germany")
            .AddQueryParam("orderBy","customerId")
            .GetStringFromUrl(responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        html.Print();
        Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
<body id=root>

ALFKI: Alfreds Futterkiste, Germany
AROUT: Around the Horn, UK
BLAUS: Blauer See Delikatessen, Germany
BSBEV: B&#39;s Beverages, UK
CONSH: Consolidated Holdings, UK
DRACD: Drachenblut Delikatessen, Germany
EASTC: Eastern Connection, UK
FRANK: Frankenversand, Germany
ISLAT: Island Trading, UK
KOENE: K&#246;niglich Essen, Germany
LEHMS: Lehmanns Marktstand, Germany
MORGK: Morgenstern Gesundkost, Germany
NORTS: North/South, UK
OTTIK: Ottilies K&#228;seladen, Germany
QUICK: QUICK-Stop, Germany
SEVES: Seven Seas Imports, UK
TOMSP: Toms Spezialit&#228;ten, Germany
WANDK: Die Wandernde Kuh, Germany


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_AutoQuery_QueryCustomer_top5_UK_Germany()
    {
        var html = BaseUrl.CombineWith("autoquery-top5-de-uk").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        html.Print();
        Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
<body id=root>

ALFKI: Alfreds Futterkiste, Germany
AROUT: Around the Horn, UK
BLAUS: Blauer See Delikatessen, Germany
BSBEV: B&#39;s Beverages, UK
CONSH: Consolidated Holdings, UK


</body>
</html>".NormalizeNewLines()));
    }

    [Test]
    public void Can_call_customers_sharpapi_page_without_arguments()
    {
        var url = BaseUrl.CombineWith("sharpapi", "customers");

        var json = url.GetJsonFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Json));
        var customers = json.FromJson<List<Customer>>();
        Assert.That(customers.Count, Is.EqualTo(QueryData.Customers.Count));
    }

    [Test]
    public void Can_call_customers_sharpapi_page_with_all_arguments()
    {
        var url = BaseUrl.CombineWith("sharpapi", "customers")
            .AddQueryParam("country", "UK")
            .AddQueryParam("city", "London")
            .AddQueryParam("limit", 10);

        var json = url.GetJsonFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Json));
        var customers = json.FromJson<List<Customer>>();

        Assert.That(customers.Map(x => x.CustomerId), Is.EquivalentTo("AROUT,BSBEV,CONSH,EASTC,NORTS,SEVES".Split(',')));
        Assert.That(customers.All(x => x.Country == "UK"));
        Assert.That(customers.All(x => x.City == "London"));
    }

    [Test]
    public void Can_call_single_customer_with_path_args()
    {
        var json = BaseUrl.CombineWith("sharpapi", "customers", "ALFKI").GetJsonFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Json));
        var customer = json.FromJson<Customer>();
        Assert.That(customer.CustomerId, Is.EqualTo("ALFKI"));
        Assert.That(customer.CompanyName, Is.EqualTo("Alfreds Futterkiste"));
        Assert.That(customer.City, Is.EqualTo("Berlin"));
        Assert.That(customer.Country, Is.EqualTo("Germany"));
    }

    [Test]
    public void Can_call_customer_with_csv_extension_to_force_ContentType()
    {
        var json = BaseUrl.CombineWith("sharpapi", "customers").AddQueryParam("limit", 1).GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Json));
        Assert.That(json, Does.StartWith("["));
            
        var html = BaseUrl.CombineWith("sharpapi", "customers.html").AddQueryParam("limit", 1).GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Html));
        Assert.That(html, Does.StartWith("<"));
            
        var csv = BaseUrl.CombineWith("sharpapi", "customers.csv").AddQueryParam("limit", 1).GetStringFromUrl();
        Assert.That(csv, Does.StartWith("CustomerId,"));
    }

    [Test]
    public void Can_call_single_customer_with_json_extension_to_force_ContentType()
    {
        var json = BaseUrl.CombineWith("sharpapi", "customers", "ALFKI.json").GetStringFromUrl(
            responseFilter:res => res.MatchesContentType(MimeTypes.Json));
        var customer = json.FromJson<Customer>();
        Assert.That(customer.CustomerId, Is.EqualTo("ALFKI"));
        Assert.That(customer.CompanyName, Is.EqualTo("Alfreds Futterkiste"));
        Assert.That(customer.City, Is.EqualTo("Berlin"));
        Assert.That(customer.Country, Is.EqualTo("Germany"));
    }

    [Test]
    public void Can_use_ifAuthenticated_filters_when_authenticated()
    {
        var context = new ScriptContext
        {
            ScriptMethods = { new ServiceStackScripts() },
            Args =
            {
                [ScriptConstants.Request] = new MockHttpRequest
                {
                    Items =
                    {
                        [Keywords.Session] = new AuthUserSession { DisplayName = "Auth User", IsAuthenticated = true }
                    }
                }
            }
        }.Init();
            
        Assert.That(context.EvaluateScript("{{ isAuthenticated }}"), Is.EqualTo("True"));
        Assert.That(context.EvaluateScript("{{ ifAuthenticated |> show: Y }}"), Is.EqualTo("Y"));
        Assert.That(context.EvaluateScript("{{ ifNotAuthenticated |> show: N }}"), Is.EqualTo(""));
        Assert.That(context.EvaluateScript("{{ 1 |> onlyIfAuthenticated }}"), Is.EqualTo("1"));
        Assert.That(context.EvaluateScript("{{ 1 |> endIfAuthenticated }}"), Is.EqualTo(""));
    }

    [Test]
    public void Can_use_ifAuthenticated_filters_when_not_authenticated()
    {
        var context = new ScriptContext
        {
            ScriptMethods = { new ServiceStackScripts() },
        }.Init();
            
        Assert.That(context.EvaluateScript("{{ isAuthenticated }}"), Is.EqualTo("False"));
        Assert.That(context.EvaluateScript("{{ ifAuthenticated |> show: Y }}"), Is.EqualTo(""));
        Assert.That(context.EvaluateScript("{{ ifNotAuthenticated |> show: N }}"), Is.EqualTo("N"));
        Assert.That(context.EvaluateScript("{{ 1 |> onlyIfAuthenticated }}"), Is.EqualTo(""));
        Assert.That(context.EvaluateScript("{{ 1 |> endIfAuthenticated }}"), Is.EqualTo("1"));
    }
}