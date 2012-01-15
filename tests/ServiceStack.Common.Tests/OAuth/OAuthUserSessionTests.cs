using System;
using System.Collections;
using System.Collections.Generic;
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
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class OAuthUserSessionTests
	{
		//Can only use either 1 OrmLiteDialectProvider at 1-time SqlServer or Sqlite.
		public static bool UseSqlServer = true;

		public static AuthUserSession GetSession()
		{
			new RedisClient().FlushAll();
			var oAuthUserSession = new AuthUserSession();
			return oAuthUserSession;
		}

		public TwitterAuthConfig GetTwitterAuthConfig()
		{
			return new TwitterAuthConfig(new AppSettings()) {
				AuthHttpGateway = new MockAuthHttpGateway(),
			};
		}

		public FacebookAuthConfig GetFacebookAuthConfig()
		{
			return new FacebookAuthConfig(new AppSettings()) {
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
				Provider = TwitterAuthConfig.Name,
				RequestToken = "JGz2CcwqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qkCdURJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			var oAuthUserSession = GetSession();
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
				Provider = FacebookAuthConfig.Name,
				AccessTokenSecret = "AAADPaOoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};
			var authInfo = new Dictionary<string, string> { };

			var oAuthUserSession = GetSession();
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
				Provider = FacebookAuthConfig.Name,
				AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};

			var oAuthUserSession = GetSession();
			var facebookAuth = GetFacebookAuthConfig();
			facebookAuth.OnAuthenticated(service, oAuthUserSession, facebookTokens, new Dictionary<string, string>());

			var serviceTokensTw = MockAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterAuthConfig.Name,
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
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
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
			var loginService = new RegistrationService {
				UserAuthRepo = userAuthRepository,
				RegistrationValidator = new RegistrationValidator { UserAuthRepo = RegistrationServiceTests.GetStubRepo() },
			};

			var responseObj = loginService.Post(request);

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