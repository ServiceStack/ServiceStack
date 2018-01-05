using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ReturnsValueType : IReturn<string> { }

    public class BadService : Service
    {
        public object Any(ReturnsValueType request) => 1;
    }

    [TestFixture]
    public class StrictModeTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(StrictModeTests), typeof(BadService).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig { StrictMode = true });
            }
        }

        private readonly ServiceStackHost appHost;

        public StrictModeTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Returning_ValueTime_throws_StrictModeException()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            try
            {
                var response = client.Get(new ReturnsValueType());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(StrictModeException)));
            }
        }
    }

    [TestFixture]
    public class StrictModeAppHostTests
    {
        class BadUserSessionAppHost : AppSelfHostBase
        {
            public BadUserSessionAppHost()
                : base(nameof(StrictModeTests), typeof(BadService).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    StrictMode = true
                });

                Plugins.Add(new AuthFeature(
                    () => new BadUserSession(), new IAuthProvider[] {
                        new CredentialsAuthProvider(),
                    }));
            }
        }

        public class BadUserSession : AuthUserSession
        {
            public BadUserSession CyclicalDep { get; set; }

            public BadUserSession()
            {
                CyclicalDep = this;
            }
        }

        [Test]
        public void Does_recognize_Cyclical_Deps()
        {
            Assert.That(TypeSerializer.HasCircularReferences(new BadUserSession()));
        }

        [Test]
        public void Using_UserSession_with_Cyclical_deps_throws_StrictModeException()
        {
            using (var appHost = new BadUserSessionAppHost())
            {
                try
                {
                    appHost
                        .Init()
                        .Start(Config.ListeningOn); //.NET Core has delayed initialization
                    Assert.Fail("Should throw");
                }
                catch (StrictModeException ex)
                {
                    Assert.That(ex.ParamName, Is.EqualTo("sessionFactory"));
                    Assert.That(ex.Code, Is.EqualTo(StrictModeCodes.CyclicalUserSession));
                }
            }
        }
    }
}