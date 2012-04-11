using System.Linq;
using Moq;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class RequiredRolesTests
	{
		Mock<IUserAuthRepository> userAuthMock;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			AuthService.Init(() => new AuthUserSession(), new CredentialsAuthProvider());
		}

		[SetUp]
		public void SetUp()
		{
			userAuthMock = new Mock<IUserAuthRepository>();

			userAuthMock.Expect(x => x.GetUserAuthByUserName(It.IsAny<string>()))
				.Returns((UserAuth)null);

			userAuthMock.Expect(x => x.CreateUserAuth(It.IsAny<UserAuth>(), It.IsAny<string>()))
				.Returns(new UserAuth { Id = 1 });
		}

		private RegistrationService GetRegistrationService()
		{
			var registrationService = RegistrationServiceTests.GetRegistrationService(authRepo: userAuthMock.Object);
			var request = RegistrationServiceTests.GetValidRegistration(autoLogin: true);
			registrationService.Execute(request);
			return registrationService;
		}

		[Test]
		public void Does_validate_RequiredRoles_with_UserAuthRepo_When_Role_not_in_Session()
		{
			var userWithAdminRole = new UserAuth { Id = 1, Roles = new[] { RoleNames.Admin }.ToList() };
			userAuthMock.Expect(x => x.GetUserAuth(It.IsAny<IAuthSession>(), It.IsAny<IOAuthTokens>()))
				.Returns(userWithAdminRole);

			var registrationService = GetRegistrationService();

			var requiredRole = new RequiredRoleAttribute(RoleNames.Admin);

			var requestContext = (MockRequestContext)registrationService.RequestContext;
			requestContext.Container.Register(userAuthMock.Object);
			var httpRes = requestContext.Get<IHttpResponse>();

			requiredRole.Execute(
				requestContext.Get<IHttpRequest>(),
				httpRes,
				null);

			Assert.That(!httpRes.IsClosed);
		}

		[Test]
		public void Does_validate_AssertRequiredRoles_with_UserAuthRepo_When_Role_not_in_Session()
		{
			var userWithAdminRole = new UserAuth { Id = 1, Roles = new[] { RoleNames.Admin }.ToList() };
			userAuthMock.Expect(x => x.GetUserAuth(It.IsAny<IAuthSession>(), It.IsAny<IOAuthTokens>()))
				.Returns(userWithAdminRole);

			var registrationService = GetRegistrationService();

			var requestContext = (MockRequestContext)registrationService.RequestContext;
			requestContext.Container.Register(userAuthMock.Object);
			var httpRes = requestContext.Get<IHttpResponse>();

			RequiredRoleAttribute.AssertRequiredRoles(requestContext, RoleNames.Admin);

			Assert.That(!httpRes.IsClosed);
		}
	}
}