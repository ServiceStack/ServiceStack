using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class MockSessionTests
    {
        public static AuthUserSession CreateUserSession()
        {
            return new AuthUserSession
            {
                UserAuthId = "1",
                Language = "en",
                PhoneNumber = "*****",
                FirstName = "Test",
                LastName = "User",
                PrimaryEmail = "test@email.com",
                UserAuthName = "Mocked",
                UserName = "Mocked",
            };
        }

        [Test]
        public void Can_Mock_Session_in_Container()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureAppHost = host => host.RegisterService(typeof(MockSessionTestService)),
                ConfigureContainer = x => x.Register<IAuthSession>(c => CreateUserSession())
            }.Init())
            {
                var response = appHost.ExecuteService(new MockSessionTest()) as AuthUserSession;

                Assert.That(response.UserAuthId, Is.EqualTo("1"));
                Assert.That(response.UserAuthName, Is.EqualTo("Mocked"));
                Assert.That(response.PrimaryEmail, Is.EqualTo("test@email.com"));
            }
        }

        [Test]
        public void Can_Mock_UnitTest_Session_in_IOC_with_MockHttpRequest()
        {
            using (new BasicAppHost
            {
                ConfigureContainer = container =>
                        container.Register<IAuthSession>(c => CreateUserSession())
            }.Init())
            {
                var service = new SessionService
                {
                    Request = new MockHttpRequest()
                };
                var session = service.GetSession();
                Assert.That(session.UserAuthId, Is.EqualTo("1"));
                Assert.That(session.UserAuthName, Is.EqualTo("Mocked"));
            }
        }

        [Test]
        public void Can_Mock_IntegrationTest_Session_with_Request()
        {
            using (new BasicAppHost(typeof(SessionService).Assembly).Init())
            {
                var req = new MockHttpRequest
                {
                    Items = { [Keywords.Session] = new AuthUserSession { UserName = "Mocked" } }
                };

                using (var service = HostContext.ResolveService<SessionService>(req))
                {
                    Assert.That(service.GetSession().UserName, Is.EqualTo("Mocked"));
                }
            }
        }

        [Test]
        public void Can_Mock_Session_in_RequestFilterAttribute()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureAppHost = host =>
                {
                    host.RegisterService(typeof(MockSessionTestService));
                }
            }.Init())
            {
                var response = appHost.ExecuteService(new MockSessionAttributeTest()) as AuthUserSession;

                Assert.That(response.UserAuthId, Is.EqualTo("1"));
                Assert.That(response.UserAuthName, Is.EqualTo("Mocked"));
                Assert.That(response.PrimaryEmail, Is.EqualTo("test@email.com"));
            }
        }

        public class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base("Mock Session Integration Test", typeof(MockSessionTestService).Assembly) { }

            public override void Configure(Container container)
            {
                GlobalRequestFilters.Add((req, res, dto) =>
                {
                    req.Items[Keywords.Session] = new AuthUserSession
                    {
                        UserAuthId = "1",
                        Language = "en",
                        PhoneNumber = "*****",
                        FirstName = "Test",
                        LastName = "User",
                        PrimaryEmail = "test@emailtest.com",
                        UserAuthName = "testuser",
                    };
                });
            }
        }

        [Test]
        public void Can_Mock_Session_in_RequestFilter_in_IntegrationTest()
        {
            using (new AppHost().Init().Start(Config.AbsoluteBaseUri))
            {
                var client = new JsonServiceClient(Config.AbsoluteBaseUri);
                var response = client.Get(new MockSessionTest());

                Assert.That(response.UserAuthId, Is.EqualTo("1"));
                Assert.That(response.UserAuthName, Is.EqualTo("testuser"));
                Assert.That(response.PrimaryEmail, Is.EqualTo("test@emailtest.com"));
            }
        }
    }

    public class MockSessionTest : IReturn<AuthUserSession> { }
    public class MockSessionAttributeTest : IReturn<AuthUserSession> { }

    public class UseMockedSession : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto) =>
            req.Items[Keywords.Session] = MockSessionTests.CreateUserSession();
    }

    public class MockSessionTestService : Service
    {
        public object Any(MockSessionTest request) => SessionAs<AuthUserSession>();

        [UseMockedSession]
        public object Any(MockSessionAttributeTest request) => SessionAs<AuthUserSession>();
    }
}