using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Moq;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class OAuthUserSessionTests
	{
		//Can only use either 1 OrmLiteDialectProvider at 1-time SqlServer or Sqlite.
		public static bool UseSqlServer = true;

		public static AuthUserSession GetNewSession()
		{
			new RedisClient().FlushAll();
			var oAuthUserSession = new AuthUserSession();
			return oAuthUserSession;
		}

		public CredentialsAuthProvider GetCredentialsAuthConfig()
		{
			return new CredentialsAuthProvider(new AppSettings()) {
				AuthHttpGateway = new MockAuthHttpGateway(),
			};
		}

		public TwitterAuthProvider GetTwitterAuthConfig()
		{
			return new TwitterAuthProvider(new AppSettings()) {
				AuthHttpGateway = new MockAuthHttpGateway(),
			};
		}

		public FacebookAuthProvider GetFacebookAuthConfig()
		{
			return new FacebookAuthProvider(new AppSettings()) {
				AuthHttpGateway = new MockAuthHttpGateway(),
			};
		}

		public static IEnumerable UserAuthRepositorys
		{
			get
			{
				var inMemoryRepo = new InMemoryAuthRepository();
				inMemoryRepo.Clear();
				yield return new TestCaseData(inMemoryRepo);

				var redisRepo = new RedisAuthRepository(new BasicRedisClientManager());
				redisRepo.Clear();
				yield return new TestCaseData(redisRepo);

				if (UseSqlServer)
				{
					var connStr = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\App_Data\auth.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";
					var sqlServerFactory = new OrmLiteConnectionFactory(connStr, SqlServerOrmLiteDialectProvider.Instance);
					var sqlServerRepo = new OrmLiteAuthRepository(sqlServerFactory);
					sqlServerRepo.DropAndReCreateTables();
					yield return new TestCaseData(sqlServerRepo);
				}
				else
				{
					var dbFactory = new OrmLiteConnectionFactory(
						":memory:", false, SqliteOrmLiteDialectProvider.Instance);
					var sqliteRepo = new OrmLiteAuthRepository(dbFactory);
					sqliteRepo.CreateMissingTables();
					sqliteRepo.Clear();
					yield return new TestCaseData(sqliteRepo);

					var dbFilePath = "~/App_Data/auth.sqlite".MapProjectPath();
					if (File.Exists(dbFilePath)) File.Delete(dbFilePath);
					var sqliteDbFactory = new OrmLiteConnectionFactory(dbFilePath);
					var sqliteDbRepo = new OrmLiteAuthRepository(sqliteDbFactory);
					sqliteDbRepo.CreateMissingTables();
					yield return new TestCaseData(sqliteDbRepo);
				}
			}
		}

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Does_persist_TwitterOAuth(IUserAuthRepository userAuthRepository)
		{
			((IClearable)userAuthRepository).Clear();

			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var service = mockService.Object;

			MockAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterAuthProvider.Name,
				RequestToken = "JGz2CcwqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qkCdURJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			var oAuthUserSession = GetNewSession();
			var twitterAuth = GetTwitterAuthConfig();
			twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterTokens, authInfo);

			Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo("Demis Bellot TW"));

			var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
			Assert.That(authProviders.Count, Is.EqualTo(1));
			var authProvider = authProviders[0];
			Assert.That(authProvider.UserAuthId, Is.EqualTo(userAuth.Id));
			Assert.That(authProvider.DisplayName, Is.EqualTo("Demis Bellot TW"));
			Assert.That(authProvider.FirstName, Is.Null);
			Assert.That(authProvider.LastName, Is.Null);
			Assert.That(authProvider.RequestToken, Is.EqualTo(twitterTokens.RequestToken));
			Assert.That(authProvider.RequestTokenSecret, Is.EqualTo(twitterTokens.RequestTokenSecret));

			Console.WriteLine(authProviders.Dump());
		}

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Does_persist_FacebookOAuth(IUserAuthRepository userAuthRepository)
		{
			((IClearable)userAuthRepository).Clear();

			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var serviceTokens = MockAuthHttpGateway.Tokens = new OAuthTokens {
				UserId = "623501766",
				DisplayName = "Demis Bellot FB",
				FirstName = "Demis",
				LastName = "Bellot",
				Email = "demis.bellot@gmail.com",
			};

			var service = mockService.Object;
			var facebookTokens = new OAuthTokens {
				Provider = FacebookAuthProvider.Name,
				AccessTokenSecret = "AAADPaOoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};
			var authInfo = new Dictionary<string, string> { };

			var oAuthUserSession = GetNewSession();
			var facebookAuth = GetFacebookAuthConfig();
			facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookTokens, authInfo);

			Assert.That(oAuthUserSession.FacebookUserId, Is.EqualTo(serviceTokens.UserId));

			Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokens.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokens.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(serviceTokens.LastName));
			Assert.That(userAuth.Email, Is.EqualTo(serviceTokens.Email));

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
			Assert.That(authProvider.AccessTokenSecret, Is.EqualTo(facebookTokens.AccessTokenSecret));

			Console.WriteLine(authProviders.Dump());
		}

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Does_merge_FacebookOAuth_TwitterOAuth(IUserAuthRepository userAuthRepository)
		{
			((IClearable)userAuthRepository).Clear();

			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var service = mockService.Object;

			var serviceTokensFb = MockAuthHttpGateway.Tokens = new OAuthTokens {
				UserId = "623501766",
				DisplayName = "Demis Bellot FB",
				FirstName = "Demis",
				LastName = "Bellot",
				Email = "demis.bellot@gmail.com",
			};

			var facebookTokens = new OAuthTokens {
				Provider = FacebookAuthProvider.Name,
				AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};

			var oAuthUserSession = GetNewSession();
			var facebookAuth = GetFacebookAuthConfig();
			facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookTokens, new Dictionary<string, string>());

			var serviceTokensTw = MockAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterAuthProvider.Name,
				RequestToken = "JGGZZ22CCqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qKKCCUUJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			var twitterAuth = GetTwitterAuthConfig();
			twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterTokens, authInfo);

			Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
			Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokensTw.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokensFb.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(serviceTokensFb.LastName));
			Assert.That(userAuth.Email, Is.EqualTo(serviceTokensFb.Email));

			var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
			Assert.That(authProviders.Count, Is.EqualTo(2));

			Console.WriteLine(userAuth.Dump());
			Console.WriteLine(authProviders.Dump());
		}

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Can_login_with_user_created_CreateUserAuth(IUserAuthRepository userAuthRepository)
		{
			((IClearable)userAuthRepository).Clear();

			var request = new Registration {
				UserName = "UserName",
				Password = "p@55word",
				Email = "as@if.com",
				DisplayName = "DisplayName",
				FirstName = "FirstName",
				LastName = "LastName",
			};

			var registrationService = GetRegistrationService(userAuthRepository);

			var responseObj = registrationService.Post(request);

			var httpResult = responseObj as IHttpResult;
			if (httpResult != null)
			{
				Assert.Fail("HttpResult found: " + httpResult.Dump());
			}

			var response = (RegistrationResponse)responseObj;
			Assert.That(response.UserId, Is.Not.Null);

			var userAuth = userAuthRepository.GetUserAuth(response.UserId);
			AssertEqual(userAuth, request);

			userAuth = userAuthRepository.GetUserAuthByUserName(request.UserName);
			AssertEqual(userAuth, request);

			userAuth = userAuthRepository.GetUserAuthByUserName(request.Email);
			AssertEqual(userAuth, request);

			string userId;
			var success = userAuthRepository.TryAuthenticate(request.UserName, request.Password, out userId);
			Assert.That(success, Is.True);
			Assert.That(userId, Is.Not.Null);

			success = userAuthRepository.TryAuthenticate(request.Email, request.Password, out userId);
			Assert.That(success, Is.True);
			Assert.That(userId, Is.Not.Null);

			success = userAuthRepository.TryAuthenticate(request.UserName, "Bad Password", out userId);
			Assert.That(success, Is.False);
			Assert.That(userId, Is.Null);
		}

		[Test, TestCaseSource("UserAuthRepositorys")]
		public void Logging_in_pulls_all_AuthInfo_from_repo_after_logging_in_all_AuthProviders(IUserAuthRepository userAuthRepository)
		{
			((IClearable)userAuthRepository).Clear();

			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var requestContext = new MockRequestContext();
			mockService.Expect(x => x.RequestContext)
				.Returns(requestContext);

			var oAuthUserSession = GetNewSession();

			var service = mockService.Object;

			var serviceTokensFb = MockAuthHttpGateway.Tokens = new OAuthTokens {
				UserId = "623501766",
				DisplayName = "Demis Bellot FB",
				FirstName = "Demis",
				LastName = "Bellot",
				Email = "demis.bellot@gmail.com",
			};

			var facebookTokens = new OAuthTokens {
				Provider = FacebookAuthProvider.Name,
				AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};

			//Facebook
			var facebookAuth = GetFacebookAuthConfig();
			facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookTokens, new Dictionary<string, string>());
			Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

			var serviceTokensTw = MockAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterAuthProvider.Name,
				RequestToken = "JGGZZ22CCqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qKKCCUUJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			//Twitter
			var twitterAuth = GetTwitterAuthConfig();
			twitterAuth.OnAuthenticated(service, oAuthUserSession, twitterTokens, authInfo);
			Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

			//Register
			var request = new Registration {
				UserName = "UserName",
				Password = "p@55word",
				Email = "as@if.com",
				DisplayName = "DisplayName",
				FirstName = "FirstName",
				LastName = "LastName",
			};

			var registrationService = GetRegistrationService(userAuthRepository, oAuthUserSession, requestContext);

			var responseObj = registrationService.Post(request);
			Assert.That(responseObj as IHttpError, Is.Null, responseObj.ToString());

			Console.WriteLine("UserId: " + oAuthUserSession.UserAuthId);

			var credentialsAuth = GetCredentialsAuthConfig();
			var loginResponse = credentialsAuth.Authenticate(service, oAuthUserSession,
				new Auth {
					provider = CredentialsAuthProvider.Name,
					UserName = request.UserName,
					Password = request.Password,
				});

			oAuthUserSession = requestContext.Get<IHttpRequest>().GetSession() as AuthUserSession;

			Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
			Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(CultureInfo.InvariantCulture), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo(request.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(request.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(request.LastName));
			Assert.That(userAuth.Email, Is.EqualTo(request.Email));

			AuthService.Init(new BasicAppHost(), null,
				new IAuthProvider[] {
					facebookAuth, 
					twitterAuth, 
					credentialsAuth
				});
			Console.WriteLine(oAuthUserSession.Dump());
			Assert.That(oAuthUserSession.ProviderOAuthAccess.Count, Is.EqualTo(2));
			Assert.That(oAuthUserSession.IsAuthenticated, Is.True);

			var authProviders = userAuthRepository.GetUserOAuthProviders(oAuthUserSession.UserAuthId);
			Assert.That(authProviders.Count, Is.EqualTo(2));

			Console.WriteLine(userAuth.Dump());
			Console.WriteLine(authProviders.Dump());
		}

		private static RegistrationService GetRegistrationService(
			IUserAuthRepository userAuthRepository,
			AuthUserSession oAuthUserSession = null,
			MockRequestContext requestContext = null)
		{
			if (oAuthUserSession == null)
				oAuthUserSession = GetNewSession();
			if (requestContext == null)
				requestContext = new MockRequestContext();

			var httpReq = requestContext.Get<IHttpRequest>();
			var httpRes = requestContext.Get<IHttpResponse>();
			oAuthUserSession.Id = httpRes.CreateSessionId(httpReq);
			httpReq.Items[ServiceExtensions.RequestItemsSessionKey] = oAuthUserSession;

			var registrationService = new RegistrationService {
				UserAuthRepo = userAuthRepository,
				RequestContext = requestContext,
				RegistrationValidator =
				new RegistrationValidator { UserAuthRepo = RegistrationServiceTests.GetStubRepo() },
			};

			return registrationService;
		}

		private static void AssertEqual(UserAuth userAuth, Registration request)
		{
			Assert.That(userAuth, Is.Not.Null);
			Assert.That(userAuth.UserName, Is.EqualTo(request.UserName));
			Assert.That(userAuth.Email, Is.EqualTo(request.Email));
			Assert.That(userAuth.DisplayName, Is.EqualTo(request.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(request.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(request.LastName));
		}
	}
}