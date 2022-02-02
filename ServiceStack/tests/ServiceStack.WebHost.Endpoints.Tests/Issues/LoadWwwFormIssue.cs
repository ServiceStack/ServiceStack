using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Route("/wwwform")]
    public class TestForm
    {
        public string A { get; set; }
    }

    public class TestFormService : Service
    {
        public object Any(TestForm request)
        {
            using (var cmd = Db.CreateCommand())
            {
                var reqAttrs = request.ToObjectDictionary();
                Db.GetDialectProvider().PrepareInsertRowStatement<TestForm>(cmd, reqAttrs);
                return cmd.CommandText;
            }
        }
    }

    public class LoadWwwFormIssue
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(LoadWwwFormIssue), typeof(TestFormService).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(c => 
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
            }
        }

        private ServiceStackHost appHost;
        public LoadWwwFormIssue()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_call_wwwform()
        {
            string ValidResponse = $"INSERT INTO \"{nameof(TestForm)}\" (\"{nameof(TestForm.A)}\") VALUES (@{nameof(TestForm.A)})";

            var baseUrl = Config.ListeningOn.CombineWith("wwwform");
            
            var responseStr = baseUrl.PostToUrl(null);//"A=B");
            Assert.That(responseStr, Is.EqualTo(ValidResponse));

            responseStr = baseUrl.PostToUrl("A");
            Assert.That(responseStr, Is.EqualTo(ValidResponse));

            responseStr = baseUrl.PostStringToUrl("A=B");
            Assert.That(responseStr, Is.EqualTo(ValidResponse));

            responseStr = baseUrl.GetStringFromUrl();
            Assert.That(responseStr, Is.EqualTo(ValidResponse));
        }
    }
}