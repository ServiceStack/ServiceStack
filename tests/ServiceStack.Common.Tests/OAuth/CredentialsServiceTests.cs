using NUnit.Framework;
using ServiceStack.Common.Web;
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
			AuthService.Init(new TestAppHost(), () => new AuthUserSession(),
				new CredentialsAuthConfig());
		}

		[Test]
		public void Empty_request_invalidates_all_fields()
		{
			var authService = new AuthService();

			var response = (HttpError)authService.Get(new Auth());
			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(3));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("InvalidProvider"));
			Assert.That(errors[0].FieldName, Is.EqualTo("provider"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[1].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[2].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[2].FieldName, Is.EqualTo("Password"));
		}

		[Test]
		public void Requires_UserName_and_Password()
		{
			var authService = new AuthService();

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