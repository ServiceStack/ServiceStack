using Moq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class RegistrationServiceTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			AuthService.Init(() => new AuthUserSession(),
				new CredentialsAuthProvider());
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

		public static RegistrationService GetRegistrationService(
			AbstractValidator<Registration> validator = null, 
			IUserAuthRepository authRepo=null)
		{
			var requestContext = new MockRequestContext();
			var service = new RegistrationService {
				RegistrationValidator = validator ?? new RegistrationValidator { UserAuthRepo = GetStubRepo() },
				UserAuthRepo = authRepo ?? GetStubRepo(),
				RequestContext = requestContext
			};
			return service;
		}

		[Test]
		public void Empty_Registration_is_invalid()
		{
			var service = GetRegistrationService();

			var response = (HttpError)service.Post(new Registration());
			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(3));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[0].FieldName, Is.EqualTo("Password"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[1].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[2].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[2].FieldName, Is.EqualTo("Email"));
		}

		[Test]
		public void Empty_Registration_is_invalid_with_FullRegistrationValidator()
		{
			var service = GetRegistrationService(new FullRegistrationValidator());

			var response = (HttpError)service.Post(new Registration());
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

			Assert.That(response as RegistrationResponse, Is.Not.Null);
		}

		public static Registration GetValidRegistration(bool autoLogin=false)
		{
			var request = new Registration {
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

			var service = new RegistrationService {
				RegistrationValidator = new RegistrationValidator { UserAuthRepo = mock.Object },
				UserAuthRepo = mock.Object
			};

			var request = new Registration {
				DisplayName = "DisplayName",
				Email = "my@email.com",
				FirstName = "FirstName",
				LastName = "LastName",
				Password = "Password",
				UserName = "UserName",
			};

			var response = (HttpError)service.Post(request);
			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(2));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("AlreadyExists"));
			Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("AlreadyExists"));
			Assert.That(errors[1].FieldName, Is.EqualTo("Email"));
		}


	}
}