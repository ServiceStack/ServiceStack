using Moq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class CredentialsServiceTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			AuthService.Init(() => new AuthUserSession(),
				new CredentialsAuthProvider());
		}

		public AuthService GetAuthService()
		{
			return new AuthService {
				RequestContext = new MockRequestContext()
			};
		}

		[Test]
		public void Empty_request_invalidates_all_fields()
		{
			var authService = GetAuthService();

			var response = (HttpError)authService.Get(new Auth());
			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(2));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
		}

		[Test]
		public void Requires_UserName_and_Password()
		{
			var authService = GetAuthService();

			var response = (HttpError)authService.Get(
				new Auth { provider = AuthService.CredentialsProvider });

			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(2));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
		}
	}
}