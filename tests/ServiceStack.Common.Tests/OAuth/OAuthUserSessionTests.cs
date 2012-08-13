using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture]
    public class OAuthUserSessionTests : OAuthUserSessionTestsBase
    {
        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Does_persist_TwitterOAuth(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            MockAuthHttpGateway.Tokens = twitterGatewayTokens;

            var authInfo = new Dictionary<string, string> {
                {"user_id", "133371690876022785"},
                {"screen_name", "demisbellot"},				
            };

            var oAuthUserSession = requestContext.ReloadSession();

            var twitterAuth = GetTwitterAuthProvider();
            twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterAuthTokens, authInfo);

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo("Demis Bellot TW"));

            var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(1));
            var authProvider = authProviders[0];
            Assert.That(authProvider.UserAuthId, Is.EqualTo(userAuth.Id));
            Assert.That(authProvider.DisplayName, Is.EqualTo("Demis Bellot TW"));
            Assert.That(authProvider.FirstName, Is.Null);
            Assert.That(authProvider.LastName, Is.Null);
            Assert.That(authProvider.RequestToken, Is.EqualTo(twitterAuthTokens.RequestToken));
            Assert.That(authProvider.RequestTokenSecret, Is.EqualTo(twitterAuthTokens.RequestTokenSecret));

            Console.WriteLine(authProviders.Dump());
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Does_persist_FacebookOAuth(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var serviceTokens = MockAuthHttpGateway.Tokens = facebookGatewayTokens;

            var oAuthUserSession = requestContext.ReloadSession();
            var authInfo = new Dictionary<string, string> { };
            var facebookAuth = GetFacebookAuthProvider();
            facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookAuthTokens, authInfo);

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.FacebookUserId, Is.EqualTo(serviceTokens.UserId));

            Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokens.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokens.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(serviceTokens.LastName));
            Assert.That(userAuth.PrimaryEmail, Is.EqualTo(serviceTokens.Email));

            var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(1));
            var authProvider = authProviders[0];
            Assert.That(authProvider.UserAuthId, Is.EqualTo(userAuth.Id));
            Assert.That(authProvider.DisplayName, Is.EqualTo(serviceTokens.DisplayName));
            Assert.That(authProvider.FirstName, Is.EqualTo(serviceTokens.FirstName));
            Assert.That(authProvider.LastName, Is.EqualTo(serviceTokens.LastName));
            Assert.That(authProvider.Email, Is.EqualTo(serviceTokens.Email));
            Assert.That(authProvider.RequestToken, Is.Null);
            Assert.That(authProvider.RequestTokenSecret, Is.Null);
            Assert.That(authProvider.AccessToken, Is.Null);
            Assert.That(authProvider.AccessTokenSecret, Is.EqualTo(facebookAuthTokens.AccessTokenSecret));

            Console.WriteLine(authProviders.Dump());
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Does_merge_FacebookOAuth_TwitterOAuth(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var serviceTokensFb = MockAuthHttpGateway.Tokens = facebookGatewayTokens;

            var oAuthUserSession = requestContext.ReloadSession();
            var facebookAuth = GetFacebookAuthProvider();
            facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookAuthTokens, new Dictionary<string, string>());

            oAuthUserSession = requestContext.ReloadSession();

            var serviceTokensTw = MockAuthHttpGateway.Tokens = twitterGatewayTokens;
            var authInfo = new Dictionary<string, string> {
                {"user_id", "133371690876022785"},
                {"screen_name", "demisbellot"},				
            };
            var twitterAuth = GetTwitterAuthProvider();
            twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterAuthTokens, authInfo);

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
            Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokensTw.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokensFb.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(serviceTokensFb.LastName));
            Assert.That(userAuth.PrimaryEmail, Is.EqualTo(serviceTokensFb.Email));

            var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(2));

            Console.WriteLine(userAuth.Dump());
            Console.WriteLine(authProviders.Dump());
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Can_login_with_user_created_CreateUserAuth(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var registrationService = GetRegistrationService(userAuthRepository);

            var responseObj = registrationService.Post(registrationDto);

            var httpResult = responseObj as IHttpResult;
            if (httpResult != null)
            {
                Assert.Fail("HttpResult found: " + httpResult.Dump());
            }

            var response = (RegistrationResponse)responseObj;
            Assert.That(response.UserId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(response.UserId);
            AssertEqual(userAuth, registrationDto);

            userAuth = userAuthRepository.GetUserAuthByUserName(registrationDto.UserName);
            AssertEqual(userAuth, registrationDto);

            userAuth = userAuthRepository.GetUserAuthByUserName(registrationDto.Email);
            AssertEqual(userAuth, registrationDto);

            UserAuth userId;
            var success = userAuthRepository.TryAuthenticate(registrationDto.UserName, registrationDto.Password, out userId);
            Assert.That(success, Is.True);
            Assert.That(userId, Is.Not.Null);

            success = userAuthRepository.TryAuthenticate(registrationDto.Email, registrationDto.Password, out userId);
            Assert.That(success, Is.True);
            Assert.That(userId, Is.Not.Null);

            success = userAuthRepository.TryAuthenticate(registrationDto.UserName, "Bad Password", out userId);
            Assert.That(success, Is.False);
            Assert.That(userId, Is.Null);
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Logging_in_pulls_all_AuthInfo_from_repo_after_logging_in_all_AuthProviders(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var oAuthUserSession = requestContext.ReloadSession();

            //Facebook
            LoginWithFacebook(oAuthUserSession);

            //Twitter
            MockAuthHttpGateway.Tokens = twitterGatewayTokens;
            var authInfo = new Dictionary<string, string> {
                {"user_id", "133371690876022785"},
                {"screen_name", "demisbellot"},				
            };
            var twitterAuth = GetTwitterAuthProvider();
            twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterAuthTokens, authInfo);
            Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

            //Register
            var registrationService = GetRegistrationService(userAuthRepository, oAuthUserSession, requestContext);

            var responseObj = registrationService.Post(registrationDto);
            Assert.That(responseObj as IHttpError, Is.Null, responseObj.ToString());

            Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

            var credentialsAuth = GetCredentialsAuthConfig();
            var loginResponse = credentialsAuth.Authenticate(service, oAuthUserSession,
                new Auth
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = registrationDto.UserName,
                    Password = registrationDto.Password,
                });

			loginResponse.PrintDump();
            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
            Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(registrationDto.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(registrationDto.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(registrationDto.LastName));
            Assert.That(userAuth.Email, Is.EqualTo(registrationDto.Email));

            Console.WriteLine(oAuthUserSession.Dump());
            Assert.That(oAuthUserSession.ProviderOAuthAccess.Count, Is.EqualTo(2));
            Assert.That(oAuthUserSession.IsAuthenticated, Is.True);

            var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(2));

            Console.WriteLine(userAuth.Dump());
            Console.WriteLine(authProviders.Dump());
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Registering_twice_creates_two_registrations(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var oAuthUserSession = requestContext.ReloadSession();

            RegisterAndLogin(userAuthRepository, oAuthUserSession);

            requestContext.RemoveSession();

            var userName1 = registrationDto.UserName;
            var userName2 = "UserName2";
            registrationDto.UserName = userName2;
            registrationDto.Email = "as@if2.com";

            var userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            Assert.That(userAuth1, Is.Not.Null);

            Register(userAuthRepository, oAuthUserSession, registrationDto);

            userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            var userAuth2 = userAuthRepository.GetUserAuthByUserName(userName2);

            Assert.That(userAuth1, Is.Not.Null);
            Assert.That(userAuth2, Is.Not.Null);
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Registering_twice_in_same_session_updates_registration(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var oAuthUserSession = requestContext.ReloadSession();

            oAuthUserSession = RegisterAndLogin(userAuthRepository, oAuthUserSession);

            var userName1 = registrationDto.UserName;
            var userName2 = "UserName2";
            registrationDto.UserName = userName2;

            Register(userAuthRepository, oAuthUserSession, registrationDto);

            var userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            var userAuth2 = userAuthRepository.GetUserAuthByUserName(userName2);

            Assert.That(userAuth1, Is.Null);
            Assert.That(userAuth2, Is.Not.Null);
        }

        [Test, TestCaseSource("UserAuthRepositorys")]
        public void Connecting_to_facebook_whilst_authenticated_connects_account(IUserAuthRepository userAuthRepository)
        {
            InitTest(userAuthRepository);

            var oAuthUserSession = requestContext.ReloadSession();

            oAuthUserSession = RegisterAndLogin(userAuthRepository, oAuthUserSession);

            LoginWithFacebook(oAuthUserSession);

            var userAuth = userAuthRepository.GetUserAuthByUserName(registrationDto.UserName);

            Assert.That(userAuth.UserName, Is.EqualTo(registrationDto.UserName));

            var userAuthProviders = userAuthRepository.GetUserOAuthProviders(userAuth.Id.ToString(CultureInfo.InvariantCulture));
            Assert.That(userAuthProviders.Count, Is.EqualTo(1));
        }

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Can_AutoLogin_whilst_Registering(IUserAuthRepository userAuthRepository)
		{
			InitTest(userAuthRepository);
			var oAuthUserSession = requestContext.ReloadSession();
			registrationDto.AutoLogin = true;
			Register(userAuthRepository, oAuthUserSession, registrationDto);

			oAuthUserSession = requestContext.ReloadSession();
			Assert.That(oAuthUserSession.IsAuthenticated, Is.True);
		}

    }
}