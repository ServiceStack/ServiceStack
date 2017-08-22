using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
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
            Results = TemplateQueryData.Products
        };
    }
    
    public class QueryTemplateRockstars : QueryDb<Rockstar> {}
    
    public class QueryCustomers : QueryDb<Customer> 
    {
        public string CustomerId { get; set; }
        public string CompanyNameContains { get; set; }
        public string[] CountryIn { get; set; }
    }
    
    public class TemplateServiceStackTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateIntegrationTests), typeof(MyTemplateServices).GetAssembly()) {}

            public readonly List<IVirtualPathProvider> TemplateFiles = new List<IVirtualPathProvider> { new MemoryVirtualFiles() };
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
                    db.InsertAll(TemplateQueryData.Customers);
                }

                Plugins.Add(new TemplatePagesFeature
                {
                    Args =
                    {
                        ["products"] = TemplateQueryData.Products,
                    },
                    TemplateFilters =
                    {
                        new TemplateAutoQueryFilters(),
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
{{ { category, orderBy, take } | withoutNullValues | sendToAutoQuery('QueryProducts') 
   | toResults | select: { it.ProductName }\n }}");

                files.WriteFile("autoquery-rockstars.html", @"
{{ { age, orderBy, take } | withoutNullValues | sendToAutoQuery('QueryTemplateRockstars') 
   | toResults | select: { it.FirstName } { it.LastName }\n }}");

                files.WriteFile("autoquery-customer.html", @"
{{ { customerId } | sendToAutoQuery('QueryCustomers') 
     | toResults | select: { it.CustomerId }: { it.CompanyName }, { it.City }\n }}");

                files.WriteFile("autoquery-customers.html", @"
{{ { countryIn, orderBy } | sendToAutoQuery('QueryCustomers') 
     | toResults | select: { it.CustomerId }: { it.CompanyName }, { it.Country }\n }}");

                files.WriteFile("autoquery-top5-de-uk.html", @"
{{ { countryIn:['UK','Germany'], orderBy:'customerId', take:5 } | sendToAutoQuery('QueryCustomers') 
     | toResults | select: { it.CustomerId }: { it.CompanyName }, { it.Country }\n }}");
                
                files.WriteFile("api/customers.html", @"
{{ limit | default(100) | assignTo: limit }}

{{ 'select CustomerId, CompanyName, City, Country from Customer' | assignTo: sql }}

{{ city    | onlyIfExists | use('City = @city')       | addTo: filters }}
{{ country | onlyIfExists | use('Country = @country') | addTo: filters }}
{{ filters | onlyIfExists | useFmt('{0} where {1}', sql, join(filters, ' and ')) | assignTo: sql }}

{{ sql | appendFmt(' ORDER BY CompanyName {0}', sqlLimit(limit)) 
       | dbSelect({ country, city }) | return }}
");
            }
        }

        public static string BaseUrl = Config.ListeningOn;
        
        private readonly ServiceStackHost appHost;
        public TemplateServiceStackTests()
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
            var html = BaseUrl.CombineWith("autoquery-data-products").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-data-products?orderBy=ProductName&take=3").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-data-products?category=Beverages").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-rockstars").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-rockstars?orderBy=FirstName&take=3").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-rockstars?age=27&orderBy=LastName").GetStringFromUrl();
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
                .GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("autoquery-top5-de-uk").GetStringFromUrl();
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
    }
}