using System.Collections.Generic;
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
    
    public class TemplateServiceStackFiltersTests
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
                }

                Plugins.Add(new TemplatePagesFeature
                {
                    Args =
                    {
                        ["products"] = TemplateQueryData.Products,
                    }
                });
                
                Plugins.Add(new AutoQueryDataFeature { MaxLimit = 100 }
                    .AddDataSource(ctx => ctx.ServiceSource<Product>(ctx.ConvertTo<GetAllProducts>()))
                );

                var files = TemplateFiles[0];
                
                files.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");
                files.WriteFile("aqdata-products.html", @"
{{ { category, orderBy, take } | withoutNullValues | sendToAutoQuery('QueryProducts') 
   | toResults | select: { it.ProductName }\n }}");
            }
        }

        public static string BaseUrl = Config.ListeningOn;
        
        private readonly ServiceStackHost appHost;
        public TemplateServiceStackFiltersTests()
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
            var html = BaseUrl.CombineWith("aqdata-products").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("aqdata-products?orderBy=ProductName&take=3").GetStringFromUrl();
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
            var html = BaseUrl.CombineWith("aqdata-products?category=Beverages").GetStringFromUrl();
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

    }
}