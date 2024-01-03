using System.Data;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MultiTenantChangeDbAppHost()
    : AppSelfHostBase("Multi Tenant Test", typeof(MultiTenantChangeDbAppHost).Assembly)
{
    public override void Configure(Container container)
    {
        container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(
            "~/App_Data/master.sqlite".MapAbsolutePath(), SqliteDialect.Provider));

        var dbFactory = container.Resolve<IDbConnectionFactory>();

        const int noOfTenants = 3;

        using (var db = dbFactory.OpenDbConnection()) {
            InitDb(db, "MASTER", "Masters inc.");
        }

        noOfTenants.Times(i => {
            var tenantId = "T0" + (i + 1);
            using var db = dbFactory.OpenDbConnectionString(GetTenantConnString(tenantId));
            InitDb(db, tenantId, "ACME {0} inc.".Fmt(tenantId));
        });

        RegisterTypedRequestFilter<IForTenant>((req,res,dto) => 
            req.Items[Keywords.DbInfo] = new ConnectionInfo { ConnectionString = GetTenantConnString(dto.TenantId)});
    }

    public void InitDb(IDbConnection db, string tenantId, string company)
    {
        db.DropAndCreateTable<TenantConfig>();
        db.Insert(new TenantConfig { Id = tenantId, Company = company });
    }

    public string GetTenantConnString(string tenantId) => tenantId != null 
        ? "~/App_Data/tenant-{0}.sqlite".Fmt(tenantId).MapAbsolutePath()
        : null;
}

[TestFixture]
public class MultiTenantChangeDbAppHostTests
{
    ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new MultiTenantChangeDbAppHost()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
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

/*
    Common Service
*/

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

/*
    Alternative way to support multi tenancy using a Custom DB Factory
*/
public class MultiTenantCustomDbFactoryAppHost : AppSelfHostBase
{
    public MultiTenantCustomDbFactoryAppHost()
        : base("Multi Tenant Test", typeof(MultiTenantCustomDbFactoryAppHost).Assembly) { }

    public override void Configure(Container container)
    {
        var dbFactory = new OrmLiteConnectionFactory(
            "~/App_Data/master.sqlite".MapAbsolutePath(), SqliteDialect.Provider);

        const int noOfTenants = 3;

        container.Register<IDbConnectionFactory>(c =>
            new MultiTenantDbFactory(dbFactory));

        var multiDbFactory = (MultiTenantDbFactory)container.Resolve<IDbConnectionFactory>();

        using (var db = multiDbFactory.OpenTenant()) {
            InitDb(db, "MASTER", "Masters inc.");
        }

        noOfTenants.Times(i => {
            var tenantId = "T0" + (i + 1);
            using var db = multiDbFactory.OpenTenant(tenantId);
            InitDb(db, tenantId, "ACME {0} inc.".Fmt(tenantId));
        });

        GlobalRequestFilters.Add((req, res, dto) => {
            if (dto is IForTenant forTenant)
                RequestContext.Instance.Items.Add("TenantId", forTenant.TenantId);
        });
    }

    public void InitDb(IDbConnection db, string tenantId, string company)
    {
        db.DropAndCreateTable<TenantConfig>();
        db.Insert(new TenantConfig { Id = tenantId, Company = company });
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

        public IDbConnection CreateDbConnection() => dbFactory.CreateDbConnection();
    }
}

[TestFixture]
public class MultiTenantCustomDbFactoryAppHostTests
{
    ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new MultiTenantCustomDbFactoryAppHost()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
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