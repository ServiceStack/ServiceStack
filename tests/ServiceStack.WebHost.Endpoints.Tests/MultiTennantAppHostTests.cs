using System.Data;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class MultiTenantAppHost : AppSelfHostBase
    {
        public MultiTenantAppHost()
            : base("Multi Tennant Test", typeof(MultiTenantAppHost).Assembly) { }

        public override void Configure(Container container)
        {
            var dbFactory = new OrmLiteConnectionFactory(
                "~/App_Data/master.sqlite".MapAbsolutePath(), SqliteDialect.Provider);

            const int noOfTennants = 3;

            container.Register<IDbConnectionFactory>(c =>
                new MultiTenantDbFactory(dbFactory));

            var multiDbFactory = (MultiTenantDbFactory)
                container.Resolve<IDbConnectionFactory>();

            using (var db = multiDbFactory.OpenTenant())
                InitDb(db, "MASTER", "Masters inc.");

            noOfTennants.Times(i =>
            {
                var tenantId = "T0" + (i + 1);
                using (var db = multiDbFactory.OpenTenant(tenantId))
                    InitDb(db, tenantId, "ACME {0} inc.".Fmt(tenantId));
            });

            GlobalRequestFilters.Add((req, res, dto) =>
            {
                var forTennant = dto as IForTenant;
                if (forTennant != null)
                    RequestContext.Instance.Items.Add("TenantId", forTennant.TenantId);
            });
        }

        public void InitDb(IDbConnection db, string tenantId, string company)
        {
            db.DropAndCreateTable<TenantConfig>();
            db.Insert(new TenantConfig { Id = tenantId, Company = company });
        }
    }

    public class MultiTenantDbFactory : IDbConnectionFactory
    {
        private readonly IDbConnectionFactory dbFactory;

        public MultiTenantDbFactory(IDbConnectionFactory dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        public IDbConnection OpenDbConnection()
        {
            var tenantId = RequestContext.Instance.Items["TenantId"] as string;
            return OpenTenant(tenantId);
        }

        public IDbConnection OpenTenant(string tenantId = null)
        {
            return tenantId != null
                ? dbFactory.OpenDbConnectionString(
                    "~/App_Data/tenant-{0}.sqlite".Fmt(tenantId).MapAbsolutePath())
                : dbFactory.OpenDbConnection();
        }

        public IDbConnection CreateDbConnection()
        {
            return dbFactory.CreateDbConnection();
        }
    }

    public interface IForTenant
    {
        string TenantId { get; }
    }

    public class TenantConfig
    {
        public string Id { get; set; }
        public string Company { get; set; }
    }

    public class GetTenant : IForTenant, IReturn<GetTenantResponse>
    {
        public string TenantId { get; set; }
    }

    public class GetTenantResponse
    {
        public TenantConfig Config { get; set; }
    }

    public class MultiTenantService : Service
    {
        public object Any(GetTenant request)
        {
            return new GetTenantResponse
            {
                Config = Db.Select<TenantConfig>().FirstOrDefault(),
            };
        }
    }

    [TestFixture]
    public class MultiTennantAppHostTests
    {
        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new MultiTenantAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_use_different_tenant_connections()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Get(new GetTenant());
            Assert.That(response.Config.Company, Is.EqualTo("Masters inc."));

            response = client.Get(new GetTenant { TenantId = "T01" });
            Assert.That(response.Config.Company, Is.EqualTo("ACME T01 inc."));

            response = client.Get(new GetTenant { TenantId = "T02" });
            Assert.That(response.Config.Company, Is.EqualTo("ACME T02 inc."));

            response = client.Get(new GetTenant { TenantId = "T03" });
            Assert.That(response.Config.Company, Is.EqualTo("ACME T03 inc."));

            Assert.Throws<WebServiceException>(() =>
                client.Get(new GetTenant { TenantId = "T04" }));
        }
    }
}