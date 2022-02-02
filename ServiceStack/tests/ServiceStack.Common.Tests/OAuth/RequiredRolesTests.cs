#if !NETCORE
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture]
    public class RequiredRolesTests
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost
            {
                ConfigureAppHost = host =>
                {
                    host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), new[] { new CredentialsAuthProvider() })
                    {
                        IncludeRegistrationService = true,
                    });
                },

            }.Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public class MockUserAuthRepository : InMemoryAuthRepository
        {
            private UserAuth userAuth;
            public MockUserAuthRepository(UserAuth userAuth)
            {
                this.userAuth = userAuth;
            }

            public override IUserAuth GetUserAuthByUserName(string userNameOrEmail) => null;

            public override async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token = default)
                => GetUserAuthByUserName(userNameOrEmail);

            public override IUserAuth CreateUserAuth(IUserAuth newUser, string password) => userAuth;
            public override async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default) 
                => CreateUserAuth(newUser, password);

            public override IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens) => userAuth;
            public override async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default) 
                => GetUserAuth(authSession, tokens);

            public override bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
            {
                userAuth = this.userAuth;
                return true;
            }

            public override async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default) => 
                this.userAuth;
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

            registrationService.PostAsync(request);
            return registrationService;
        }

        [Test]
        public async Task Does_validate_RequiredRoles_with_UserAuthRepo_When_Role_not_in_Session()
        {
            var registrationService = GetRegistrationService();

            var requiredRole = new RequiredRoleAttribute(RoleNames.Admin);

            var request = registrationService.Request;
            HostContext.Container.Register(userAuth);
            var httpRes = request.Response;

            await requiredRole.ExecuteAsync(request, request.Response, request.OperationName);

            Assert.That(!httpRes.IsClosed);
        }

        [Test]
        public async Task Does_validate_AssertRequiredRoles_with_UserAuthRepo_When_Role_not_in_Session()
        {
            var registrationService = GetRegistrationService();

            var request = registrationService.Request;
            HostContext.Container.Register(userAuth);

            await RequiredRoleAttribute.AssertRequiredRoleAsync(request, RoleNames.Admin);

            Assert.That(!request.Response.IsClosed);
        }
    }
}
#endif