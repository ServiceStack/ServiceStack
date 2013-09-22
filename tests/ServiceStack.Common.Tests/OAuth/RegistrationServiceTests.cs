using Moq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class RegistrationServiceTests
	{
        static BasicAppHost _appHost = null;
        static readonly AuthUserSession authUserSession = new AuthUserSession();

        static IAppHost GetAppHost()
        {
            if (_appHost == null)
            {
                _appHost = new BasicAppHost();
                var authService = new AuthenticateService();
                authService.SetAppHost(_appHost);
                _appHost.Container.Register(authService);
                _appHost.Container.Register<IAuthSession>(authUserSession);
            }
            return _appHost;
        }
        
        [TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
            AuthenticateService.Init(() => authUserSession, new CredentialsAuthProvider());            
		}

		public static IUserAuthRepository GetStubRepo()
		{
			var mock = new Mock<IUserAuthRepository>();
			mock.Expect(x => x.GetUserAuthByUserName(It.IsAny<string>()))
				.Returns((UserAuth)null);
			mock.Expect(x => x.CreateUserAuth(It.IsAny<UserAuth>(), It.IsAny<string>()))
				.Returns(new UserAuth { Id = 1 });

			return mock.Object;
		}

		public static RegisterService GetRegistrationService(
			AbstractValidator<Register> validator = null, 
			IUserAuthRepository authRepo=null,
			string contentType=null)
		{
			var requestContext = new MockRequestContext();
			if (contentType != null)
			{
				requestContext.ResponseContentType = contentType;
			}
		    var userAuthRepository = authRepo ?? GetStubRepo();
		    var service = new RegisterService {
                RegistrationValidator = validator ?? new RegistrationValidator { UserAuthRepo = userAuthRepository },
				UserAuthRepo = userAuthRepository,
				RequestContext = requestContext,
			};

		    var appHost = GetAppHost();
            appHost.Register(userAuthRepository);
		    service.SetAppHost(appHost);

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
	        var response = (HttpError) service.RunAction(register, (svc, req) => svc.Post(req));
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
		public void Accepts_valid_registration()
		{
			var service = GetRegistrationService();

			var request = GetValidRegistration();

			var response = service.Post(request);

			Assert.That(response as RegisterResponse, Is.Not.Null);
		}

		public static Register GetValidRegistration(bool autoLogin=false)
		{
			var request = new Register {
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
			var mock = new Mock<IUserAuthRepository>();
			var mockExistingUser = new UserAuth();
			mock.Expect(x => x.GetUserAuthByUserName(It.IsAny<string>()))
				.Returns(mockExistingUser);

			var service = new RegisterService {
				RegistrationValidator = new RegistrationValidator { UserAuthRepo = mock.Object },
				UserAuthRepo = mock.Object,
            };

			var request = new Register {
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

		[Test]
		public void Registration_with_Html_ContentType_And_Continue_returns_302_with_Location()
		{
            var service = GetRegistrationService(null, null, MimeTypes.Html);

			var request = GetValidRegistration();
			request.Continue = "http://localhost/home";

			var response = service.Post(request) as HttpResult;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Status, Is.EqualTo(302));
			Assert.That(response.Headers[HttpHeaders.Location], Is.EqualTo("http://localhost/home"));
		}

		[Test]
		public void Registration_with_EmptyString_Continue_returns_RegistrationResponse()
		{
            var service = GetRegistrationService(null, null, MimeTypes.Html);

			var request = GetValidRegistration();
			request.Continue = string.Empty;

			var response = service.Post(request);

			Assert.That(response as HttpResult, Is.Null);
			Assert.That(response as RegisterResponse, Is.Not.Null);
		}

		[Test]
		public void Registration_with_Json_ContentType_And_Continue_returns_RegistrationResponse_with_ReferrerUrl()
		{
            var service = GetRegistrationService(null, null, MimeTypes.Json);

			var request = GetValidRegistration();
			request.Continue = "http://localhost/home";

			var response = service.Post(request);

			Assert.That(response as HttpResult, Is.Null);
			Assert.That(response as RegisterResponse, Is.Not.Null);
			Assert.That(((RegisterResponse)response).ReferrerUrl, Is.EqualTo("http://localhost/home"));
		}
	}
}