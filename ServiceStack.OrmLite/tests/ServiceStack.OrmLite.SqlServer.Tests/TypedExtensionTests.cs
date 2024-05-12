using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.SqlServerTests;

public class TypedExtensionTests
{
    public class TestDao
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Thing { get; set; }
    }

    private IOrmLiteDialectProvider provider;
    private OrmLiteConnectionFactory factory;

    [OneTimeSetUp]
    public void Setup()
    {
        provider = new SqlServer2014OrmLiteDialectProvider();
        var connectionString = OrmLiteTestBase.GetConnectionString();
        factory = new OrmLiteConnectionFactory(connectionString, provider, false);
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        OrmLiteConfig.DialectProvider = provider;

        using var connection = factory.OpenDbConnection();
        if (connection.TableExists<TestDao>())
        {
            connection.DropTable<TestDao>();
        }
    }

    [Test]
    public void GivenAnOrmLiteTypedConnectionFactory_WhenUsingOrmLiteExtensionsAndGlobalProviderNotSet_ThenArgumentNullExceptionIsNotThrown()
    {
        using var db = factory.OpenDbConnection();
        db.CreateTableIfNotExists<TestDao>();

        var dao = new TestDao {Id = 1, Thing = "Thing"};

        db.Insert(dao);
        db.SingleById<TestDao>(1);

        dao.Thing = "New Thing";

        db.Update(dao, d => d.Id == dao.Id);
        db.Delete(dao);
        db.DropTable<TestDao>();
    }
}