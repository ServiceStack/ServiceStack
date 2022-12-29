#if !NETCORE
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture]
    public class RegistrationServiceTests
    {
        static AuthUserSession authUserSession = new AuthUserSession();

        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost
            {
                ConfigureContainer = c =>
                {
                    var authService = new AuthenticateService();
                    c.Register(authService);
                    c.Register<IAuthSession>(authUserSession);
                    AuthenticateService.Init(() => authUserSession, new CredentialsAuthProvider());
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public static IUserAuthRepository GetStubRepo()
        {
            var authRepo = new InMemoryAuthRepository();
            return authRepo;
        }

        public static RegisterService GetRegistrationService(
            AbstractValidator<Register> validator = null,
            IUserAuthRepository authRepo = null,
            string contentType = null)
        {
            var requestContext = new BasicRequest();
            if (contentType != null)
            {
                requestContext.ResponseContentType = contentType;
            }
            var userAuthRepository = authRepo ?? GetStubRepo();
            HostContext.Container.Register<IAuthRepository>(userAuthRepository);

            var service = new RegisterService
            {
                RegistrationValidator = validator ?? new RegistrationValidator(),
                Request = requestContext,
            };

            HostContext.Container.Register(userAuthRepository);
            
            (HostContext.TryResolve<IAuthRepository>() as InMemoryAuthRepository)?.Clear();

            return service;
        }

        [Test]
        public void Empty_Registration_is_invalid()
        {
            var service = GetRegistrationService();

            var response = PostRegistrationError(service, new Register());
            var errors = response.GetFieldErrors();

            Assert.That(errors.Count, Is.EqualTo(3));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[0].FieldName, Is.EqualTo("Password"));
            Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[1].FieldName, Is.EqualTo("UserName"));
            Assert.That(errors[2].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[2].FieldName, Is.EqualTo("Email"));
        }

        private static HttpError PostRegistrationError(RegisterService service, Register register)
        {
#pragma warning disable CS0618
            var response = (HttpError)service.RunAction(register, (svc, req) => svc.Post(req));
#pragma warning restore CS0618
            return response;
        }

        [Test]
        public void Empty_Registration_is_invalid_with_FullRegistrationValidator()
        {
            var service = GetRegistrationService(new FullRegistrationValidator());

            var response = PostRegistrationError(service, new Register());
            var errors = response.GetFieldErrors();

            Assert.That(errors.Count, Is.EqualTo(4));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[0].FieldName, Is.EqualTo("Password"));
            Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[1].FieldName, Is.EqualTo("UserName"));
            Assert.That(errors[2].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[2].FieldName, Is.EqualTo("Email"));
            Assert.That(errors[3].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors[3].FieldName, Is.EqualTo("DisplayName"));
        }

        [Test]
        public async Task Accepts_valid_registration()
        {
            var service = GetRegistrationService();

            var request = GetValidRegistration();

            var response = await service.PostAsync(request);

            Assert.That(response as RegisterResponse, Is.Not.Null);
        }

        public static Register GetValidRegistration(bool autoLogin = false)
        {
            var request = new Register
            {
                DisplayName = "DisplayName",
                Email = "my@email.com",
                FirstName = "FirstName",
                LastName = "LastName",
                Password = "Password",
                UserName = "UserName",
                AutoLogin = autoLogin,
            };
            return request;
        }

        [Test]
        public void Requires_unique_UserName_and_Email()
        {
            ClearSession();
            (HostContext.TryResolve<IAuthRepository>() as InMemoryAuthRepository)?.Clear();

            var authRepo = new InMemoryAuthRepository();
            authRepo.CreateUserAuth(new UserAuth {
                Email = "my@email.com",
                UserName = "UserName",
            }, "password");
            appHost.Register<IAuthRepository>(authRepo);

            var service = new RegisterService
            {
                RegistrationValidator = new RegistrationValidator(),
            };

            var request = new Register
            {
                DisplayName = "DisplayName",
                Email = "my@email.com",
                FirstName = "FirstName",
                LastName = "LastName",
                Password = "Password",
                UserName = "UserName",
            };

            var response = PostRegistrationError(service, request);
            var errors = response.GetFieldErrors();

            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("AlreadyExists"));
            Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
            Assert.That(errors[1].ErrorCode, Is.EqualTo("AlreadyExists"));
            Assert.That(errors[1].FieldName, Is.EqualTo("Email"));
        }

        private void ClearSession()
        {
            authUserSession = new AuthUserSession();
            appHost.Container.Register<IAuthSession>(c => null);
        }

        [Test]
        public async Task Registration_with_Html_ContentType_And_Continue_returns_302_with_Location()
        {
            var service = GetRegistrationService(null, null, MimeTypes.Html);

            var request = GetValidRegistration();

            service.Request.QueryString[Keywords.Continue] = "http://localhost/home";
            var response = (await service.PostAsync(request)) as HttpResult;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(302));
            Assert.That(response.Headers[HttpHeaders.Location], Is.EqualTo("http://localhost/home"));
        }

        [Test]
        public async Task Registration_with_EmptyString_Continue_returns_RegistrationResponse()
        {
            var service = GetRegistrationService(null, null, MimeTypes.Html);

            var request = GetValidRegistration();
            service.Request.QueryString[Keywords.Continue] = string.Empty;

            var response = await service.PostAsync(request);

            Assert.That(response as HttpResult, Is.Null);
            Assert.That(response as RegisterResponse, Is.Not.Null);
        }

        [Test]
        public async Task Registration_with_Json_ContentType_And_Continue_returns_RegistrationResponse_with_ReferrerUrl()
        {
            var service = GetRegistrationService(null, null, MimeTypes.Json);

            var request = GetValidRegistration();
            service.Request.QueryString[Keywords.Continue] = "http://localhost/home";

            var response = await service.PostAsync(request);

            Assert.That(response as HttpResult, Is.Null);
            Assert.That(response as RegisterResponse, Is.Not.Null);
            Assert.That(((RegisterResponse)response).ReferrerUrl, Is.EqualTo("http://localhost/home"));
        }
    }
}
#endif