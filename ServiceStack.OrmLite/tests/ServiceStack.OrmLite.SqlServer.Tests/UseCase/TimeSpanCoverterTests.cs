using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.SqlServer.Converters;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase;

public class TimeSpanCoverterTests : OrmLiteTestBase
{
    private OrmLiteConnectionFactory dbFactory;
    
    [OneTimeSetUp]
    public new void TestFixtureSetUp()
    {
        base.TestFixtureSetUp();
        var dp = new SqlServerOrmLiteDialectProvider();
        dp.RegisterConverter<TimeSpan>(new SqlServerTimeConverter
        {
            Precision = 7
        });
        dbFactory = new OrmLiteConnectionFactory(base.ConnectionString, dp);
        using var db = dbFactory.Open();
        db.DropTable<TestTimeConverter>();
        db.ExecuteNonQuery(@"CREATE TABLE [dbo].[TestTimeConverter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[mytime] [time](7) NULL
) ON [PRIMARY]");
    }

    [Test]
    public void TestTimeConverterTimeSpan()
    {
        using var db = dbFactory.Open();
        Assert.DoesNotThrow(() =>
        {
            db.Insert(new TestTimeConverter
            {
                mytime = null
            });
            db.Insert(new TestTimeConverter
            {
                mytime = TimeSpan.FromDays(1)
            });
            var results = db.Select<TestTimeConverter>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results.Count, Is.EqualTo(2));
        });
    }

    class TestTimeConverter
    {
        [AutoIncrement]
        public int Id { get; set; }
        public TimeSpan? mytime { get; set; }
    }
}