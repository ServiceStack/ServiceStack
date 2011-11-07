using System;
using System.Collections;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ServiceStack.Redis;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class OAuthUserSessionTests
	{
		private static OAuthUserSession GetSession()
		{
			new RedisClient().FlushAll();
			var oAuthUserSession = new OAuthUserSession {
				AuthHttpGateway = new MockOAuthHttpGateway(),
			};
			return oAuthUserSession;
		}

		public static IEnumerable UserAuthRepositorys
		{
			get
			{
				var redisManager = new BasicRedisClientManager();
				redisManager.Exec(x => x.FlushAll());
				yield return new TestCaseData(new RedisAuthRepository(redisManager));
			}
		}

		[Test, TestCaseSource(typeof(OAuthUserSessionTests), "UserAuthRepositorys")]
		public void Does_persist_TwitterOAuth(IUserAuthRepository userAuthRepository)
		{
			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var service = mockService.Object;

			MockOAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterOAuthConfig.Name,
				RequestToken = "JGz2CcwqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qkCdURJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			var oAuthUserSession = GetSession();
			oAuthUserSession.OnAuthenticated(service, twitterTokens, authInfo);

			Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo("Demis Bellot TW"));

			var authProviders = userAuthRepository.GetUserAuthProviders(oAuthUserSession.UserAuthId);
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

		[Test, TestCaseSource(typeof(OAuthUserSessionTests), "UserAuthRepositorys")]
		public void Does_persist_FacebookOAuth(IUserAuthRepository userAuthRepository)
		{
			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var serviceTokens = MockOAuthHttpGateway.Tokens = new OAuthTokens {
				UserId = "623501766",
				DisplayName = "Demis Bellot FB",
				FirstName = "Demis",
				LastName = "Bellot",
				Email = "demis.bellot@gmail.com",
			};

			var service = mockService.Object;
			var facebookTokens = new OAuthTokens {
				Provider = FacebookOAuthConfig.Name,
				AccessTokenSecret = "AAADPaOoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};
			var authInfo = new Dictionary<string, string> { };

			var oAuthUserSession = GetSession();
			oAuthUserSession.OnAuthenticated(service, facebookTokens, authInfo);
			Assert.That(oAuthUserSession.FacebookUserId, Is.EqualTo(serviceTokens.UserId));

			Assert.That(oAuthUserSession.UserAuthId, Is.Not.Null);

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokens.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokens.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(serviceTokens.LastName));
			Assert.That(userAuth.Email, Is.EqualTo(serviceTokens.Email));

			var authProviders = userAuthRepository.GetUserAuthProviders(oAuthUserSession.UserAuthId);
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

		[Test, TestCaseSource(typeof(OAuthUserSessionTests), "UserAuthRepositorys")]
		public void Does_merge_FacebookOAuth_TwitterOAuth(IUserAuthRepository userAuthRepository)
		{
			var mockService = new Mock<IServiceBase>();
			mockService.Expect(x => x.TryResolve<IUserAuthRepository>())
				.Returns(userAuthRepository);

			var service = mockService.Object;

			var serviceTokensFb = MockOAuthHttpGateway.Tokens = new OAuthTokens {
				UserId = "623501766",
				DisplayName = "Demis Bellot FB",
				FirstName = "Demis",
				LastName = "Bellot",
				Email = "demis.bellot@gmail.com",
			};

			var facebookTokens = new OAuthTokens {
				Provider = FacebookOAuthConfig.Name,
				AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
			};

			var oAuthUserSession = GetSession();
			oAuthUserSession.OnAuthenticated(service, facebookTokens, new Dictionary<string, string>());

			var serviceTokensTw = MockOAuthHttpGateway.Tokens = new OAuthTokens { DisplayName = "Demis Bellot TW" };

			var twitterTokens = new OAuthTokens {
				Provider = TwitterOAuthConfig.Name,
				RequestToken = "JGGZZ22CCqgB1GR5e0EmGFxzyxGTw2rwEFFcC8a9o7g",
				RequestTokenSecret = "qKKCCUUJ2R10bMieVQZZad7iSwWkPYJmtBYzPoM9q0",
				UserId = "133371690876022785",
			};
			var authInfo = new Dictionary<string, string> {
				{"user_id", "133371690876022785"},
				{"screen_name", "demisbellot"},				
			};

			oAuthUserSession.OnAuthenticated(service, twitterTokens, authInfo);

			Assert.That(oAuthUserSession.TwitterUserId, Is.EqualTo(authInfo["user_id"]));
			Assert.That(oAuthUserSession.TwitterScreenName, Is.EqualTo(authInfo["screen_name"]));

			var userAuth = userAuthRepository.GetUserAuth(oAuthUserSession.UserAuthId);
			Assert.That(userAuth.Id.ToString(), Is.EqualTo(oAuthUserSession.UserAuthId));
			Assert.That(userAuth.DisplayName, Is.EqualTo(serviceTokensTw.DisplayName));
			Assert.That(userAuth.FirstName, Is.EqualTo(serviceTokensFb.FirstName));
			Assert.That(userAuth.LastName, Is.EqualTo(serviceTokensFb.LastName));
			Assert.That(userAuth.Email, Is.EqualTo(serviceTokensFb.Email));

			var authProviders = userAuthRepository.GetUserAuthProviders(oAuthUserSession.UserAuthId);
			Assert.That(authProviders.Count, Is.EqualTo(2));

			Console.WriteLine(userAuth.Dump());
			Console.WriteLine(authProviders.Dump());
		}

	}
}