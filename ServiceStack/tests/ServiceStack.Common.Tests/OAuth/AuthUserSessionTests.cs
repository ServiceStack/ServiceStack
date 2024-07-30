#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests.OAuth
{
    public class InMemoryAuthUserSessionTests : AuthUserSessionTests
    {
        public override IUserAuthRepository CreateAuthRepo()
        {
            var inMemoryRepo = new InMemoryAuthRepository();
            inMemoryRepo.Clear();
            InitTest(inMemoryRepo);
            return inMemoryRepo;
        }
    }

    public class RedisAuthUserSessionTests : AuthUserSessionTests
    {
        public override IUserAuthRepository CreateAuthRepo()
        {
            var appSettings = new AppSettings();
            var redisRepo = new RedisAuthRepository(
                new BasicRedisClientManager(appSettings.GetString("Redis.Host") ?? "localhost"));
            redisRepo.Clear();
            InitTest(redisRepo);
            return redisRepo;
        }
    }

    [Ignore("Integration Test")]
    public class DynamoDbAuthUserSessionTests : AuthUserSessionTests
    {
        public override IUserAuthRepository CreateAuthRepo()
        {
            var db = new PocoDynamo(TestsConfig.CreateDynamoDBClient());
            db.DeleteAllTables();
            var dynamoDbRepo = new DynamoDbAuthRepository(db);
            dynamoDbRepo.Clear();
            InitTest(dynamoDbRepo);
            dynamoDbRepo.InitSchema();
            return dynamoDbRepo;
        }
    }

    public class OrmLiteSqlServerAuthUserSessionTests : AuthUserSessionTests
    {
        public override IUserAuthRepository CreateAuthRepo()
        {
            var sqlServerFactory = new OrmLiteConnectionFactory(
                TestsConfig.SqlServerConnString, 
                SqlServerDialect.Provider);
            var sqlServerRepo = new OrmLiteAuthRepository(sqlServerFactory);
            try { sqlServerRepo.Clear(); } catch {}
            sqlServerRepo.InitSchema();
            InitTest(sqlServerRepo);
            return sqlServerRepo;
        }
    }

    public class OrmLiteSqliteMemoryAuthUserSessionTests : AuthUserSessionTests
    {
        public override IUserAuthRepository CreateAuthRepo()
        {
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            var sqliteRepo = new OrmLiteAuthRepository(dbFactory);
            sqliteRepo.InitSchema();
            InitTest(sqliteRepo);
            return sqliteRepo;
        }
    }

    [TestFixture]
    public abstract class AuthUserSessionTests : AuthUserSessionTestsBase
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            RegisterService.AllowUpdates = true;
            appHost = new BasicAppHost {
                ConfigureAppHost = host => {
                    host.Plugins.Add(new AuthFeature(new CredentialsAuthProvider(host.AppSettings)));
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            RegisterService.AllowUpdates = false;
            appHost.Dispose();
        }

        public abstract IUserAuthRepository CreateAuthRepo();

        public IUserAuthRepository InitAuthRepo()
        {
            var authRepo = CreateAuthRepo();
            appHost.Container.Register<IAuthRepository>(authRepo);
            return authRepo;
        }

        [Test]
        public async Task Does_persist_TwitterOAuth()
        {
            var userAuthRepository = InitAuthRepo();

            MockAuthHttpGateway.Tokens = twitterGatewayTokens;

            var authInfo = new Dictionary<string, string> {
                {"user_id", "133371690876022785"},
                {"screen_name", "demisbellot"},
            };

            var oAuthUserSession = requestContext.ReloadSession();

            var twitterAuth = GetTwitterAuthProvider();
            await twitterAuth.OnAuthenticatedAsync(service, oAuthUserSession, twitterAuthTokens, authInfo);

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo("Demis Bellot TW"));

            var authProviders = userAuthRepository.GetUserAuthDetails(oAuthUserSession.UserAuthId);
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

        [Test]
        public async Task Does_persist_FacebookOAuth()
        {
            var userAuthRepository = InitAuthRepo();

            var serviceTokens = MockAuthHttpGateway.Tokens = facebookGatewayTokens;

            var oAuthUserSession = requestContext.ReloadSession();
            var facebookAuth = GetFacebookAuthProvider();
            await facebookAuth.OnAuthenticatedAsync(service, oAuthUserSession, facebookAuthTokens,
                JsonObject.Parse(facebookAuth.AuthHttpGateway.DownloadFacebookUserInfo("facebookCode")));

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.FacebookUserId, Is.EqualTo(serviceTokens.UserId));

            Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokens.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokens.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(serviceTokens.LastName));
            Assert.That(userAuth.PrimaryEmail, Is.EqualTo(serviceTokens.Email));

            var authProviders = userAuthRepository.GetUserAuthDetails(oAuthUserSession.UserAuthId);
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

        [Test]
        public async Task Does_merge_FacebookOAuth_TwitterOAuth()
        {
            var userAuthRepository = InitAuthRepo();

            var serviceTokensFb = MockAuthHttpGateway.Tokens = facebookGatewayTokens;

            var oAuthUserSession = requestContext.ReloadSession();
            var facebookAuth = GetFacebookAuthProvider();
            await facebookAuth.OnAuthenticatedAsync(service, oAuthUserSession, facebookAuthTokens,
                JsonObject.Parse(facebookAuth.AuthHttpGateway.DownloadFacebookUserInfo("facebookCode")));

            oAuthUserSession = requestContext.ReloadSession();

            var serviceTokensTw = MockAuthHttpGateway.Tokens = twitterGatewayTokens;
            var authInfo = new Dictionary<string, string> {
                {"user_id", "17575623"},
                {"screen_name", "demisbellot"},
            };
            var twitterAuth = GetTwitterAuthProvider();
            await twitterAuth.OnAuthenticatedAsync(service, oAuthUserSession, twitterAuthTokens, authInfo);

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
            Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokensFb.DisplayName));
            Assert.That(userAuth.PrimaryEmail, Is.EqualTo(serviceTokensFb.Email));
            Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokensFb.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(serviceTokensFb.LastName));

            var authProviders = userAuthRepository.GetUserAuthDetails(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(2));

            Console.WriteLine(userAuth.Dump());
            Console.WriteLine(authProviders.Dump());
        }

        [Test]
        public async Task Can_login_with_user_created_CreateUserAuth()
        {
            var userAuthRepository = InitAuthRepo();

            var registrationService = GetRegistrationService(userAuthRepository);

            var responseObj = await registrationService.PostAsync(RegisterDto);

            if (responseObj is IHttpResult httpResult)
            {
                Assert.Fail("HttpResult found: " + httpResult.Dump());
            }

            var response = (RegisterResponse)responseObj;
            Assert.That(response.UserId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(response.UserId);
            AssertEqual(userAuth, RegisterDto);

            userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.UserName);
            AssertEqual(userAuth, RegisterDto);

            var success = userAuthRepository.TryAuthenticate(RegisterDto.UserName, RegisterDto.Password, out var userId);
            Assert.That(success, Is.True);
            Assert.That(userId, Is.Not.Null);

            //DynamoDb can't support both UserName and Email
            if (!(userAuthRepository is DynamoDbAuthRepository))
            {
                userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.Email);
                AssertEqual(userAuth, RegisterDto);

                success = userAuthRepository.TryAuthenticate(RegisterDto.Email, RegisterDto.Password, out userId);
                Assert.That(success, Is.True);
                Assert.That(userId, Is.Not.Null);
            }

            success = userAuthRepository.TryAuthenticate(RegisterDto.UserName, "Bad Password", out userId);
            Assert.That(success, Is.False);
            Assert.That(userId, Is.Null);
        }


        [Test]
        public async Task Can_login_with_user_created_CreateUserAuth_Email()
        {
            var userAuthRepository = InitAuthRepo();

            //Clear Username so only Email is registered
            RegisterDto.UserName = null;

            var registrationService = GetRegistrationService(userAuthRepository);

            var responseObj = await registrationService.PostAsync(RegisterDto);

            if (responseObj is IHttpResult httpResult)
            {
                Assert.Fail("HttpResult found: " + httpResult.Dump());
            }

            var response = (RegisterResponse)responseObj;
            Assert.That(response.UserId, Is.Not.Null);

            var userAuth = userAuthRepository.GetUserAuth(response.UserId);
            AssertEqual(userAuth, RegisterDto);

            userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.Email);
            AssertEqual(userAuth, RegisterDto);

            var success = userAuthRepository.TryAuthenticate(RegisterDto.Email, RegisterDto.Password, out var userId);
            Assert.That(success, Is.True);
            Assert.That(userId, Is.Not.Null);

            success = userAuthRepository.TryAuthenticate(RegisterDto.Email, "Bad Password", out userId);
            Assert.That(success, Is.False);
            Assert.That(userId, Is.Null);
        }

        [Test]
        public async Task Logging_in_pulls_all_AuthInfo_from_repo_after_logging_in_all_AuthProviders()
        {
            var userAuthRepository = InitAuthRepo();

            var oAuthUserSession = requestContext.ReloadSession();

            //Facebook
            LoginWithFacebook(oAuthUserSession);

            //Twitter
            MockAuthHttpGateway.Tokens = twitterGatewayTokens;
            var authInfo = new Dictionary<string, string> {
                {"user_id", "17575623"},
                {"screen_name", "demisbellot"},
            };
            var twitterAuth = GetTwitterAuthProvider();
            await twitterAuth.OnAuthenticatedAsync(service, oAuthUserSession, twitterAuthTokens, authInfo);
            Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

            //Register
            var registrationService = GetRegistrationService(userAuthRepository, oAuthUserSession, requestContext);

            var responseObj = await registrationService.PostAsync(RegisterDto);
            Assert.That(responseObj as IHttpError, Is.Null, responseObj.ToString());

            Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

            var credentialsAuth = GetCredentialsAuthConfig();
            var loginResponse = await credentialsAuth.AuthenticateAsync(service, oAuthUserSession,
                new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = RegisterDto.UserName,
                    Password = RegisterDto.Password,
                });

            oAuthUserSession = requestContext.ReloadSession();

            Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
            Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

            var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
            Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
            Assert.That(userAuth.DisplayName, Is.EqualTo(RegisterDto.DisplayName));
            Assert.That(userAuth.FirstName, Is.EqualTo(RegisterDto.FirstName));
            Assert.That(userAuth.LastName, Is.EqualTo(RegisterDto.LastName));
            Assert.That(userAuth.Email, Is.EqualTo(RegisterDto.Email));

            Console.WriteLine(oAuthUserSession.Dump());
            Assert.That(oAuthUserSession.ProviderOAuthAccess.Count, Is.EqualTo(2));
            Assert.That(oAuthUserSession.IsAuthenticated, Is.True);

            var authProviders = userAuthRepository.GetUserAuthDetails(oAuthUserSession.UserAuthId);
            Assert.That(authProviders.Count, Is.EqualTo(2));

            Console.WriteLine(userAuth.Dump());
            Console.WriteLine(authProviders.Dump());
        }

        [Test]
        public void Registering_twice_creates_two_registrations()
        {
            var userAuthRepository = InitAuthRepo();

            var oAuthUserSession = requestContext.ReloadSession();

            RegisterAndLogin(userAuthRepository, oAuthUserSession);

            requestContext.RemoveSession();

            var userName1 = RegisterDto.UserName;
            var userName2 = "UserName2";
            RegisterDto.UserName = userName2;
            RegisterDto.Email = "as@if2.com";

            var userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            Assert.That(userAuth1, Is.Not.Null);

            Register(userAuthRepository, null, RegisterDto);

            userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            var userAuth2 = userAuthRepository.GetUserAuthByUserName(userName2);

            Assert.That(userAuth1, Is.Not.Null);
            Assert.That(userAuth2, Is.Not.Null);
        }

        [Test]
        public void Registering_twice_in_same_session_updates_registration()
        {
            var userAuthRepository = InitAuthRepo();

            var oAuthUserSession = requestContext.ReloadSession();

            oAuthUserSession = RegisterAndLogin(userAuthRepository, oAuthUserSession);

            var userName1 = RegisterDto.UserName;
            var userName2 = "UserName2";
            RegisterDto.UserName = userName2;

            Register(userAuthRepository, oAuthUserSession, RegisterDto);

            var userAuth1 = userAuthRepository.GetUserAuthByUserName(userName1);
            var userAuth2 = userAuthRepository.GetUserAuthByUserName(userName2);

            Assert.That(userAuth1, Is.Null);
            Assert.That(userAuth2, Is.Not.Null);
        }

        [Test]
        public void Connecting_to_facebook_whilst_authenticated_connects_account()
        {
            var userAuthRepository = InitAuthRepo();

            var oAuthUserSession = requestContext.ReloadSession();

            oAuthUserSession = RegisterAndLogin(userAuthRepository, oAuthUserSession);

            LoginWithFacebook(oAuthUserSession);

            var userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.UserName);

            Assert.That(userAuth.UserName, Is.EqualTo(RegisterDto.UserName));

            var userAuthProviders = userAuthRepository.GetUserAuthDetails(userAuth.Id.ToString(CultureInfo.InvariantCulture));
            Assert.That(userAuthProviders.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_AutoLogin_whilst_Registering()
        {
            var userAuthRepository = InitAuthRepo();
            var oAuthUserSession = requestContext.ReloadSession();
            RegisterDto.AutoLogin = true;
            Register(userAuthRepository, oAuthUserSession, RegisterDto);

            oAuthUserSession = requestContext.ReloadSession();
            Assert.That(oAuthUserSession.IsAuthenticated, Is.True);
        }

        [Test]
        public void Can_DeleteUserAuth()
        {
            var userAuthRepository = InitAuthRepo();

            var oAuthUserSession = requestContext.ReloadSession();
            oAuthUserSession = RegisterAndLogin(userAuthRepository, oAuthUserSession);

            var userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.UserName);
            Assert.That(userAuth, Is.Not.Null);

            userAuthRepository.DeleteUserAuth(userAuth.Id.ToString());
            userAuth = userAuthRepository.GetUserAuthByUserName(RegisterDto.UserName);
            Assert.That(userAuth, Is.Null);
        }

    }
}
#endif