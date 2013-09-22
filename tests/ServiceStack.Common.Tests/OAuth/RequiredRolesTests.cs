using System.Linq;
using NUnit.Framework;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture]
    public class RequiredRolesTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            AuthenticateService.Init(() => new AuthUserSession(), new CredentialsAuthProvider());
        }

        public class MockUserAuthRepository : InMemoryAuthRepository
        {
            private UserAuth userAuth;
            public MockUserAuthRepository(UserAuth userAuth)
            {
                this.userAuth = userAuth;
            }

            public override UserAuth GetUserAuthByUserName(string userNameOrEmail)
            {
                return null;
            }

            public override UserAuth CreateUserAuth(UserAuth newUser, string password)
            {
                return userAuth;
            }

            public override UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
            {
                return userAuth;
            }

            public override bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
            {
                userAuth = this.userAuth;
                return true;
            }
        }

        private MockUserAuthRepository userAuth;

        [SetUp]
        public void SetUp()
        {
            var userWithAdminRole = new UserAuth { Id = 1, Roles = new[] { RoleNames.Admin }.ToList() };
            userAuth = new MockUserAuthRepository(userWithAdminRole);
        }

        private RegisterService GetRegistrationService()
        {
            var registrationService = RegistrationServiceTests.GetRegistrationService(authRepo: userAuth);
            var request = RegistrationServiceTests.GetValidRegistration(autoLogin: true);

            registrationService.Post(request);
            return registrationService;
        }

        [Test]
        public void Does_validate_RequiredRoles_with_UserAuthRepo_When_Role_not_in_Session()
        {
            var registrationService = GetRegistrationService();

            var requiredRole = new RequiredRoleAttribute(RoleNames.Admin);

            var requestContext = (MockRequestContext)registrationService.RequestContext;
            requestContext.Container.Register(userAuth);
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
            var registrationService = GetRegistrationService();

            var requestContext = (MockRequestContext)registrationService.RequestContext;
            requestContext.Container.Register(userAuth);
            var httpRes = requestContext.Get<IHttpResponse>();

            RequiredRoleAttribute.AssertRequiredRoles(requestContext, RoleNames.Admin);

            Assert.That(!httpRes.IsClosed);
        }
    }
}