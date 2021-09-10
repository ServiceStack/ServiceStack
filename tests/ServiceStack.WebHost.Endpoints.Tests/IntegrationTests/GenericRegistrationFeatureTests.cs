using System;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
    public class GenericRegistrationFeatureTests
    {
        private ServiceStackHost appHost;

        private static string custonValidationError = "Must contain @";
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(GenericRegistrationFeatureTests), typeof(MyRegistrationService).Assembly) { }
            
            public override void Configure(Container container)
            {
                Plugins.Add(new AuthFeature(() => new CustomRegisterUserSession(),
                    new IAuthProvider[]
                    {
                        new CredentialsAuthProvider()
                    }));

                Plugins.Add(new GenericRegistrationFeature<MyRegistrationService,CustomRegister>
                {
                    ValidateFn = (service, method, dto) =>
                    {
                        var req = (CustomRegister)dto;
                        if (req.Email != null && !req.Email.Contains("@"))
                            throw new ValidationError(new ValidationErrorField(
                                HttpStatusCode.BadRequest, "email", custonValidationError));
                        return null;
                    }
                });

                container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(":memory:",
                    SqliteDialect.Provider));
                container.Register<IAuthRepository>(new OrmLiteAuthRepository<CustomRegisterUserAuth,UserAuthDetails>(
                    container.Resolve<IDbConnectionFactory>()));

                var authRepo = container.Resolve<IAuthRepository>();
                authRepo.InitSchema();

            }
        }

        [OneTimeSetUp]
        public void OnetimeSetup()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_register_user()
        {
            var client = GetClient();
            var req = GetValidRegisterRequest();
            var response = client.Post(req);
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.UserId, Is.Not.Null);

            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            var regUser = db.SingleById<CustomRegisterUserAuth>(int.Parse(response.UserId));
            
            Assert.That(regUser.NetworkName, Is.EqualTo("Test1234"));
        }

        [Test]
        public void Can_validate_custom_register_request()
        {
            var client = GetClient();
            var req1 = new CustomRegister()
            {
                Email = "no-password@email.com"
            };
            
            var exception = Assert.Throws<WebServiceException>(() =>
            {
                var response = client.Post(req1);
            });
            
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.StatusCode, Is.EqualTo(500));
            Assert.That(exception.ResponseBody, Contains.Substring("'Password' must not be empty."));

            // Validation error comes from custom ValidationFn
            var req2 = new CustomRegister
            {
                Email = "bademail-",
                Password = "test1234"
            };
            
            exception = Assert.Throws<WebServiceException>(() =>
            {
                var response = client.Post(req2);
            });
            
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.StatusCode, Is.EqualTo(400));
            Assert.That(exception.ResponseBody, Contains.Substring(custonValidationError));
            
            // Validation error coming from default RegistrationValidator
            var req3 = new CustomRegister
            {
                Email = "4@email.com",
                Password = "test1234",
                ConfirmPassword = "1234test"
            };
            
            exception = Assert.Throws<WebServiceException>(() =>
            {
                var response = client.Post(req3);
            });
            
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.StatusCode, Is.EqualTo(500));
            Assert.That(exception.ResponseBody, Contains.Substring("Passwords should match"));
        }

        [Test]
        public void Can_throw_on_duplicate_due_to_default_validator()
        {
            var client = GetClient();
            var req = GetValidRegisterRequest();
            
            var response = client.Post(req);
            
            var exception = Assert.Throws<WebServiceException>(() =>
            {
                client.Post(req);
            });
            
            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void Can_persist_custom_session_data()
        {
            var client = GetClient();
            var req = GetValidRegisterRequest(null,"Test", true);
            var response = client.Post(req);
            
            Assert.That(response.UserId, Is.Not.Null);

            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            var regUser = db.SingleById<CustomRegisterUserAuth>(int.Parse(response.UserId));
            Assert.That(regUser.NetworkName, Is.EqualTo("Test"));

            var sessionResponse = client.Get(new GetRegisteredUserSession());
            
            Assert.That(sessionResponse, Is.Not.Null);
            Assert.That(sessionResponse.Session, Is.Not.Null);
            Assert.That(sessionResponse.Session.NetworkName, Is.Not.Null);
            Assert.That(sessionResponse.Session.NetworkName, Is.EqualTo("Test"));

        }

        private JsonServiceClient GetClient() => new (Config.ListeningOn);

        private CustomRegister GetValidRegisterRequest(string email = null, string networkName = null, bool? autoLogin = null)
        {
            var req = new CustomRegister
            {
                DisplayName = "DisplayName",
                Email = email ?? Guid.NewGuid()+ "@email.com",
                FirstName = "FirstName",
                LastName = "LastName",
                Password = "Password",
                NetworkName = networkName ?? "Test1234",
                AutoLogin = autoLogin
            };
            return req;
        }
    }
    
    public class MyRegistrationService : GenericRegisterService<CustomRegister>
    {}

    public class CustomRegister : Register, IReturn<RegisterResponse>
    {
        public string NetworkName { get; set; }
    }

    public class CustomRegisterUserAuth : UserAuth
    {
        public string NetworkName { get; set; }
    }

    public class CustomRegisterUserSession : AuthUserSession
    {
        public string NetworkName { get; set; }
    }

    public class SessionService : Service
    {
        [Authenticate]
        public object Get(GetRegisteredUserSession request)
        {
            var session = SessionAs<CustomRegisterUserSession>();
            return new GetRegisteredUserSessionResponse()
            {
                Session = session
            };
        }
    }

    public class GetRegisteredUserSession : IReturn<GetRegisteredUserSessionResponse>
    {
        
    }

    public class GetRegisteredUserSessionResponse
    {
        public CustomRegisterUserSession Session { get; set; }
    }
}