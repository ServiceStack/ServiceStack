using System.Data;
using System.Diagnostics;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace CheckHttpListener
{
    public class Config
    {
        public static string ListeningOn = "http://localhost:3000/";
    }

    [Route("/analyze")]
    [Route("/analyze/{Id}")]
    [Route("/analyze/{Id}/{Type}")]
    public class Analyze : IReturn<Analyze>
    {
        public int Id { get; set; }
        public string Type { get; set; }
    }

    [Route("/analyze-01")]
    public class Analyze01 { }
    [Route("/analyze-02")]
    public class Analyze02 { }
    [Route("/analyze-03")]
    public class Analyze03 { }
    [Route("/analyze-04")]
    public class Analyze04 { }
    [Route("/analyze-05")]
    public class Analyze05 { }
    [Route("/analyze-06")]
    public class Analyze06 { }
    [Route("/analyze-07")]
    public class Analyze07 { }
    [Route("/analyze-08")]
    public class Analyze08 { }
    [Route("/analyze-09")]
    public class Analyze09 { }
    [Route("/analyze-10")]
    public class Analyze10 { }

    public class AnalyzeServices : Service
    {
        public object Any(Analyze request)
        {
            return Db.Single<Analyze>(q => q.Type == request.Type);
        }

        public object Any(Analyze02 request)
        {
            return request;
        }

        public object Any(Analyze03 request)
        {
            return request;
        }

        public object Any(Analyze04 request)
        {
            return request;
        }

        public object Any(Analyze05 request)
        {
            return request;
        }

        public object Any(Analyze06 request)
        {
            return request;
        }

        public object Any(Analyze07 request)
        {
            return request;
        }

        public object Any(Analyze08 request)
        {
            return request;
        }

        public object Any(Analyze09 request)
        {
            return request;
        }

        public object Any(Analyze10 request)
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

            container.RegisterAs<OrmLiteCacheClient, ICacheClient>();

            container.Resolve<ICacheClient>().InitSchema();

            using (var cache = container.Resolve<ICacheClient>())
            {
                cache.Set("key:test", new Analyze { Id = 1 });
            }

            using (var cache = container.Resolve<ICacheClient>())
            {
                var test = cache.Get<Analyze>("key:test");
                test.Id.PrintDump();
            }
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
            10.Times(i => DbConnection.Insert(new Analyze { Id = i, Type = "Type" + i }));

            //IdGenerator = AppHost.Container.TryResolve<IDatabaseIdGenerator>();

            //TestData = new DbHelper(DbConnection, IdGenerator);

            //LocationHelper = new LocationHelper(DbConnection, IdGenerator);
            //TimeSeriesHelper = new TimeSeriesHelper(DbConnection, IdGenerator);

            ServiceClient = new JsonServiceClient(Config.ListeningOn);
        }
    }

    public class AnalyzeTestSideEffects_ManyTests : AnalyzeTestSideEffectsBase
    {
        public void Test(int i)
        {
            var request = new Analyze { Type = "Type" + i };
            var response = ServiceClient.Get(request);

            Assert.That(response.Id, Is.EqualTo(i));
        }

        [Test]
        public void Test00() { Test(0); }
        [Test]
        public void Test01() { Test(1); }
        [Test]
        public void Test02() { Test(2); }
        [Test]
        public void Test03() { Test(3); }
        [Test]
        public void Test04() { Test(4); }
        [Test]
        public void Test05() { Test(5); }
        [Test]
        public void Test06() { Test(6); }
        [Test]
        public void Test07() { Test(7); }
        [Test]
        public void Test08() { Test(8); }
        [Test]
        public void Test09() { Test(9); }
        [Test]
        public void Test10() { Test(0); }

        [Test]
        public void Test11() { Test(1); }
        [Test]
        public void Test12() { Test(2); }
        [Test]
        public void Test13() { Test(3); }
        [Test]
        public void Test14() { Test(4); }
        [Test]
        public void Test15() { Test(5); }
        [Test]
        public void Test16() { Test(6); }
        [Test]
        public void Test17() { Test(7); }
        [Test]
        public void Test18() { Test(8); }
        [Test]
        public void Test19() { Test(9); }
        [Test]
        public void Test20() { Test(0); }

        [Test]
        public void Test21() { Test(1); }
        [Test]
        public void Test22() { Test(2); }
        [Test]
        public void Test23() { Test(3); }
        [Test]
        public void Test24() { Test(4); }
        [Test]
        public void Test25() { Test(5); }
        [Test]
        public void Test26() { Test(6); }
        [Test]
        public void Test27() { Test(7); }
        [Test]
        public void Test28() { Test(8); }
        [Test]
        public void Test29() { Test(9); }
        [Test]
        public void Test30() { Test(0); }

        [Test]
        public void Test31() { Test(1); }
        [Test]
        public void Test32() { Test(2); }
        [Test]
        public void Test33() { Test(3); }
        [Test]
        public void Test34() { Test(4); }
        [Test]
        public void Test35() { Test(5); }
        [Test]
        public void Test36() { Test(6); }
        [Test]
        public void Test37() { Test(7); }
        [Test]
        public void Test38() { Test(8); }
        [Test]
        public void Test39() { Test(9); }
        [Test]
        public void Test40() { Test(0); }

        [Test]
        public void Test41() { Test(1); }
        [Test]
        public void Test42() { Test(2); }
        [Test]
        public void Test43() { Test(3); }
        [Test]
        public void Test44() { Test(4); }
        [Test]
        public void Test45() { Test(5); }
        [Test]
        public void Test46() { Test(6); }
        [Test]
        public void Test47() { Test(7); }
        [Test]
        public void Test48() { Test(8); }
        [Test]
        public void Test49() { Test(9); }
        [Test]
        public void Test50() { Test(0); }

        [Test]
        public void Test51() { Test(1); }
        [Test]
        public void Test52() { Test(2); }
        [Test]
        public void Test53() { Test(3); }
        [Test]
        public void Test54() { Test(4); }
        [Test]
        public void Test55() { Test(5); }
        [Test]
        public void Test56() { Test(6); }
        [Test]
        public void Test57() { Test(7); }
        [Test]
        public void Test58() { Test(8); }
        [Test]
        public void Test59() { Test(9); }
        [Test]
        public void Test60() { Test(0); }

        [Test]
        public void Test61() { Test(1); }
        [Test]
        public void Test62() { Test(2); }
        [Test]
        public void Test63() { Test(3); }
        [Test]
        public void Test64() { Test(4); }
        [Test]
        public void Test65() { Test(5); }
        [Test]
        public void Test66() { Test(6); }
        [Test]
        public void Test67() { Test(7); }
        [Test]
        public void Test68() { Test(8); }
        [Test]
        public void Test69() { Test(9); }
        [Test]
        public void Test70() { Test(0); }

        [Test]
        public void Test71() { Test(1); }
        [Test]
        public void Test72() { Test(2); }
        [Test]
        public void Test73() { Test(3); }
        [Test]
        public void Test74() { Test(4); }
        [Test]
        public void Test75() { Test(5); }
        [Test]
        public void Test76() { Test(6); }
        [Test]
        public void Test77() { Test(7); }
        [Test]
        public void Test78() { Test(8); }
        [Test]
        public void Test79() { Test(9); }
        [Test]
        public void Test80() { Test(0); }

        [Test]
        public void Test81() { Test(1); }
        [Test]
        public void Test82() { Test(2); }
        [Test]
        public void Test83() { Test(3); }
        [Test]
        public void Test84() { Test(4); }
        [Test]
        public void Test85() { Test(5); }
        [Test]
        public void Test86() { Test(6); }
        [Test]
        public void Test87() { Test(7); }
        [Test]
        public void Test88() { Test(8); }
        [Test]
        public void Test89() { Test(9); }
        [Test]
        public void Test90() { Test(0); }

        [Test]
        public void Test91() { Test(1); }
        [Test]
        public void Test92() { Test(2); }
        [Test]
        public void Test93() { Test(3); }
        [Test]
        public void Test94() { Test(4); }
        [Test]
        public void Test95() { Test(5); }
        [Test]
        public void Test96() { Test(6); }
        [Test]
        public void Test97() { Test(7); }
        [Test]
        public void Test98() { Test(8); }
        [Test]
        public void Test99() { Test(9); }
    }
}