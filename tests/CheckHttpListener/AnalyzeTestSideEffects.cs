using System.Data;
using System.Diagnostics;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace CheckHttpListener
{
    public class Config
    {
        public static string ListeningOn = "http://localhost:3000/";
    }

    public class Analyze
    {
        public int Id { get; set; }
    }

    public class AnalyzeServices : Service
    {
        public object Any(Analyze request)
        {
            return request;
        }
    }

    public class AnalyzeAppHost : AppHostHttpListenerBase
    {
        public AnalyzeAppHost() : base("Analyze Tests", typeof(AnalyzeServices).Assembly) { }

        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
        }
    }

    [TestFixture]
    public class AnalyzeAppHostRestartSideEffects
    {
        [Test]
        public void Restart_AppHosts()
        {
            for (int i = 0; i < 100; i++)
            {
                var sw = Stopwatch.StartNew();
                using (var appHost = new AnalyzeAppHost())
                {
                    appHost.Init();
                    var db = appHost.TryResolve<IDbConnectionFactory>().Open();

                    appHost.Start(Config.ListeningOn);
                    db.Dispose();
                }
                "starting #1 took {0}ms".Print(sw.ElapsedMilliseconds);
            }
        }
    }


    [TestFixture]
    public class AnalyzeTestSideEffectsBase
    {
        private static int TestCount = 0;

        private AnalyzeAppHost AppHost;
        public IDbConnection DbConnection;
        public IServiceClient ServiceClient;
        private Stopwatch sw;

        private void Acquire()
        {
            sw = Stopwatch.StartNew();
        }

        private void Release()
        {
            "Test {0} {1}ms".Print(TestCount++, sw.ElapsedMilliseconds);
            sw = null;
        }

        [TearDown]
        public void OnTestFixtureTearDown()
        {
            "Disposing Fixture: #{0}".Print(TestCount);
            if (AppHost != null)
            {
                AppHost.Stop();
                AppHost.Dispose();
                AppHost = null;
            }

            if (DbConnection != null)
            {
                DbConnection.Close();
                DbConnection.Dispose();
                DbConnection = null;
            }

            Release();
        }

        [SetUp]
        public void OnTestSetup()
        {
            Acquire();

            AppHost = new AnalyzeAppHost();
            AppHost.Init();

            AppHost.Start(Config.ListeningOn);

            DbConnection = AppHost.Container.TryResolve<IDbConnectionFactory>().Open();

            DbConnection.DropAndCreateTable<Analyze>();

            //MockSiteVisitDatabase.DropAndCreateDummyDatabaseSchema(DbConnection);
            10.Times(i => DbConnection.Insert(new Analyze { Id = i }));

            //IdGenerator = AppHost.Container.TryResolve<IDatabaseIdGenerator>();

            //TestData = new DbHelper(DbConnection, IdGenerator);

            //LocationHelper = new LocationHelper(DbConnection, IdGenerator);
            //TimeSeriesHelper = new TimeSeriesHelper(DbConnection, IdGenerator);

            ServiceClient = new JsonServiceClient(Config.ListeningOn);
        }
    }

    public class AnalyzeTestSideEffects_ManyTests : AnalyzeTestSideEffectsBase
    {
        [Test]
        public void Test00() { }
        [Test]
        public void Test01() { }
        [Test]
        public void Test02() { }
        [Test]
        public void Test03() { }
        [Test]
        public void Test04() { }
        [Test]
        public void Test05() { }
        [Test]
        public void Test06() { }
        [Test]
        public void Test07() { }
        [Test]
        public void Test08() { }
        [Test]
        public void Test09() { }
        [Test]
        public void Test10() { }

        [Test]
        public void Test11() { }
        [Test]
        public void Test12() { }
        [Test]
        public void Test13() { }
        [Test]
        public void Test14() { }
        [Test]
        public void Test15() { }
        [Test]
        public void Test16() { }
        [Test]
        public void Test17() { }
        [Test]
        public void Test18() { }
        [Test]
        public void Test19() { }
        [Test]
        public void Test20() { }

        [Test]
        public void Test21() { }
        [Test]
        public void Test22() { }
        [Test]
        public void Test23() { }
        [Test]
        public void Test24() { }
        [Test]
        public void Test25() { }
        [Test]
        public void Test26() { }
        [Test]
        public void Test27() { }
        [Test]
        public void Test28() { }
        [Test]
        public void Test29() { }
        [Test]
        public void Test30() { }

        [Test]
        public void Test31() { }
        [Test]
        public void Test32() { }
        [Test]
        public void Test33() { }
        [Test]
        public void Test34() { }
        [Test]
        public void Test35() { }
        [Test]
        public void Test36() { }
        [Test]
        public void Test37() { }
        [Test]
        public void Test38() { }
        [Test]
        public void Test39() { }
        [Test]
        public void Test40() { }

        [Test]
        public void Test41() { }
        [Test]
        public void Test42() { }
        [Test]
        public void Test43() { }
        [Test]
        public void Test44() { }
        [Test]
        public void Test45() { }
        [Test]
        public void Test46() { }
        [Test]
        public void Test47() { }
        [Test]
        public void Test48() { }
        [Test]
        public void Test49() { }
        [Test]
        public void Test50() { }

        [Test]
        public void Test51() { }
        [Test]
        public void Test52() { }
        [Test]
        public void Test53() { }
        [Test]
        public void Test54() { }
        [Test]
        public void Test55() { }
        [Test]
        public void Test56() { }
        [Test]
        public void Test57() { }
        [Test]
        public void Test58() { }
        [Test]
        public void Test59() { }
        [Test]
        public void Test60() { }

        [Test]
        public void Test61() { }
        [Test]
        public void Test62() { }
        [Test]
        public void Test63() { }
        [Test]
        public void Test64() { }
        [Test]
        public void Test65() { }
        [Test]
        public void Test66() { }
        [Test]
        public void Test67() { }
        [Test]
        public void Test68() { }
        [Test]
        public void Test69() { }
        [Test]
        public void Test70() { }

        [Test]
        public void Test71() { }
        [Test]
        public void Test72() { }
        [Test]
        public void Test73() { }
        [Test]
        public void Test74() { }
        [Test]
        public void Test75() { }
        [Test]
        public void Test76() { }
        [Test]
        public void Test77() { }
        [Test]
        public void Test78() { }
        [Test]
        public void Test79() { }
        [Test]
        public void Test80() { }

        [Test]
        public void Test81() { }
        [Test]
        public void Test82() { }
        [Test]
        public void Test83() { }
        [Test]
        public void Test84() { }
        [Test]
        public void Test85() { }
        [Test]
        public void Test86() { }
        [Test]
        public void Test87() { }
        [Test]
        public void Test88() { }
        [Test]
        public void Test89() { }
        [Test]
        public void Test90() { }

        [Test]
        public void Test91() { }
        [Test]
        public void Test92() { }
        [Test]
        public void Test93() { }
        [Test]
        public void Test94() { }
        [Test]
        public void Test95() { }
        [Test]
        public void Test96() { }
        [Test]
        public void Test97() { }
        [Test]
        public void Test98() { }
        [Test]
        public void Test99() { }
    }
}