#if !NETCORE_SUPPORT
using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests.OAuth
{
    public abstract class AuthUserSessionTestsBase
    {
        public static bool LoadUserAuthRepositorys = true;

        //Can only use either 1 OrmLiteDialectProvider at 1-time SqlServer or Sqlite.
        public static bool UseSqlServer = false;

        public static AuthUserSession GetNewSession2()
        {
            var oAuthUserSession = new AuthUserSession();
            return oAuthUserSession;
        }

        public CredentialsAuthProvider GetCredentialsAuthConfig()
        {
            return new CredentialsAuthProvider(new AppSettings());
        }

        public TwitterAuthProvider GetTwitterAuthProvider()
        {
            return new TwitterAuthProvider(new AppSettings())
            {
                AuthHttpGateway = new MockAuthHttpGateway(),
            };
        }

        public FacebookAuthProvider GetFacebookAuthProvider()
        {
            return new FacebookAuthProvider(new AppSettings())
            {
                AuthHttpGateway = new MockAuthHttpGateway(),
            };
        }

        protected Mock<IServiceBase> mockService;
        protected BasicRequest requestContext;
        protected IServiceBase service;

        protected AuthTokens facebookGatewayTokens = new AuthTokens
        {
            UserId = "623501766",
            DisplayName = "Demis Bellot FB",
            FirstName = "Demis",
            LastName = "Bellot",
            Email = "demis.bellot@gmail.com",
        };
        protected AuthTokens twitterGatewayTokens = new AuthTokens
        {
            DisplayName = "Demis Bellot TW"
        };
        protected AuthTokens facebookAuthTokens = new AuthTokens
        {
            Provider = FacebookAuthProvider.Name,
            AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
        };
        protected AuthTokens twitterAuthTokens = new AuthTokens
        {
            Provider = TwitterAuthProvider.Name,
            RequestToken = "JGGZZ22CCqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
            RequestTokenSecret = "qKKCCUUJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
            UserId = "133371690876022785",
        };
        protected Register RegisterDto;

        protected void InitTest(IUserAuthRepository userAuthRepository)
        {
            try
            {
                ((IClearable)userAuthRepository).Clear();
            }
            catch { /*ignore*/ }

            var appsettingsMock = new Mock<IAppSettings>();
            var appSettings = appsettingsMock.Object;

            new AuthFeature(null, new IAuthProvider[] {
                new CredentialsAuthProvider(),
                new BasicAuthProvider(),
                new FacebookAuthProvider(appSettings),
                new TwitterAuthProvider(appSettings)
            })
            .Register(null);

            mockService = new Mock<IServiceBase>();
            mockService.Expect(x => x.TryResolve<IAuthRepository>()).Returns(userAuthRepository);
            requestContext = new BasicRequest
            {
                Headers = {
                    {"X-ss-id", SessionExtensions.CreateRandomSessionId() }
                }
            };
            mockService.Expect(x => x.Request).Returns(requestContext);
            service = mockService.Object;

            RegisterDto = new Register
            {
                UserName = "UserName",
                Password = "p@55word",
                Email = "as@if.com",
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            };
        }

        public static RegisterService GetRegistrationService(
            IUserAuthRepository userAuthRepository,
            AuthUserSession oAuthUserSession = null,
            BasicRequest request = null)
        {
            if (request == null)
                request = new BasicRequest();
            if (oAuthUserSession == null)
                oAuthUserSession = request.ReloadSession();

            oAuthUserSession.Id = request.Response.CreateSessionId(request);
            request.Items[Keywords.Session] = oAuthUserSession;

            var mockAppHost = new BasicAppHost();

            mockAppHost.Container.Register<IAuthRepository>(userAuthRepository);

            var authService = new AuthenticateService
            {
                Request = request,
            };
            authService.SetResolver(mockAppHost);
            mockAppHost.Register(authService);

            var registrationService = new RegisterService
            {
                Request = request,
                RegistrationValidator =
                    new RegistrationValidator(),
            };
            registrationService.SetResolver(mockAppHost);

            return registrationService;
        }

        public static void AssertEqual(IUserAuth userAuth, Register request)
        {
            Assert.That(userAuth, Is.Not.Null);
            Assert.That(userAuth.UserName, Is.EqualTo(request.UserName));
            Assert.That(userAuth.Email, Is.EqualTo(request.Email));
            Assert.That(userAuth.DisplayName, Is.EqualTo(request.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(request.LastName));
        }

        protected AuthUserSession RegisterAndLogin(IUserAuthRepository userAuthRepository, AuthUserSession oAuthUserSession)
        {
            Register(userAuthRepository, oAuthUserSession);

            Login(RegisterDto.UserName, RegisterDto.Password, oAuthUserSession);

            oAuthUserSession = requestContext.ReloadSession();
            return oAuthUserSession;
        }

        protected object Login(string userName, string password, AuthUserSession oAuthUserSession = null)
        {
            if (oAuthUserSession == null)
                oAuthUserSession = requestContext.ReloadSession();

            var credentialsAuth = GetCredentialsAuthConfig();
            return credentialsAuth.Authenticate(service, oAuthUserSession,
                new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = RegisterDto.UserName,
                    Password = RegisterDto.Password,
                });
        }

        protected object Register(IUserAuthRepository userAuthRepository, AuthUserSession oAuthUserSession, Register register = null)
        {
            if (register == null)
                register = RegisterDto;

            var registrationService = GetRegistrationService(userAuthRepository, oAuthUserSession, requestContext);
            var response = registrationService.Post(register);
            Assert.That(response as IHttpError, Is.Null);
            return response;
        }

        protected void LoginWithFacebook(AuthUserSession oAuthUserSession)
        {
            MockAuthHttpGateway.Tokens = facebookGatewayTokens;
            var facebookAuth = GetFacebookAuthProvider();
            facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookAuthTokens, new Dictionary<string, string>());
            Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);
        }
    }
}
#endif