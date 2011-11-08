using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Redis;

namespace ServiceStack.ServiceInterface.Auth
{
	public class RedisAuthRepository : IUserAuthRepository, IClearable
	{
		private readonly IRedisClientManagerFacade factory;

		public RedisAuthRepository(IRedisClientsManager factory)
			: this(new RedisClientManagerFacade(factory)) {}

		public RedisAuthRepository(IRedisClientManagerFacade factory)
		{
			this.factory = factory;
		}

		private string IndexUserAuthAndProviderIdsSet(long userAuthId)
		{
			return "urn:UserAuth>UserOAuthProvider:" + userAuthId;
		}

		private string IndexProviderUserIdHash(string provider)
		{
			return "hash:ProviderUserId>OAuthProviderId:" + provider;
		}

		public virtual void LoadUserAuth(IOAuthSession session, IOAuthTokens tokens)
		{
			session.ThrowIfNull("session");

			var userAuth = GetUserAuth(session, tokens);
			LoadUserAuth(session, userAuth);
		}

		private void LoadUserAuth(IOAuthSession session, UserAuth userAuth)
		{
			if (userAuth == null) return;

			session.UserAuthId = userAuth.Id.ToString();
			session.DisplayName = userAuth.DisplayName;
			session.FirstName = userAuth.FirstName;
			session.LastName = userAuth.LastName;
			session.Email = userAuth.Email;
		}

		private UserAuth GetUserAuth(IRedisClientFacade redis, string userAuthId)
		{
			long longId;
			if (userAuthId == null || !long.TryParse(userAuthId, out longId)) return null;

			return redis.As<UserAuth>().GetById(longId);
		}

		public UserAuth GetUserAuth(string userAuthId)
		{
			using (var redis = factory.GetClient())
				return GetUserAuth(redis, userAuthId);
		}

		public List<UserOAuthProvider> GetUserAuthProviders(string userAuthId)
		{
			userAuthId.ThrowIfNullOrEmpty("userAuthId");

			using (var redis = factory.GetClient())
			{
				var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
				var authProiverIds = redis.GetAllItemsFromSet(idx);
				return redis.As<UserOAuthProvider>().GetByIds(authProiverIds).ToList();
			}
		}

		public UserAuth GetUserAuth(IOAuthSession authSession, IOAuthTokens tokens)
		{
			using (var redis = factory.GetClient())
				return GetUserAuth(redis, authSession, tokens);
		}

		private UserAuth GetUserAuth(IRedisClientFacade redis, IOAuthSession authSession, IOAuthTokens tokens)
		{
			if (tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty()) return null;

			if (!authSession.UserAuthId.IsNullOrEmpty())
			{
				var userAuth = GetUserAuth(redis, authSession.UserAuthId);
				if (userAuth != null) return userAuth;
			}

			var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
			if (!oAuthProviderId.IsNullOrEmpty())
			{
				var oauthProvider = redis.As<UserOAuthProvider>().GetById(oAuthProviderId);
				if (oauthProvider != null)
					return redis.As<UserAuth>().GetById(oauthProvider.UserAuthId);
			}
			return null;
		}

		public string CreateOrMergeAuthSession(IOAuthSession oAuthSession, IOAuthTokens tokens)
		{
			using (var redis = factory.GetClient())
			{
				UserOAuthProvider oauthProvider = null;

				var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
				if (!oAuthProviderId.IsNullOrEmpty())
					oauthProvider = redis.As<UserOAuthProvider>().GetById(oAuthProviderId);

				var userAuth = GetUserAuth(redis, oAuthSession, tokens);

				if (userAuth == null)
				{
					userAuth = new UserAuth {
						Id = redis.As<UserAuth>().GetNextSequence(),
					};
				}

				if (oauthProvider == null)
				{
					oauthProvider = new UserOAuthProvider {
						Id = redis.As<UserOAuthProvider>().GetNextSequence(),
						UserAuthId = userAuth.Id,
						Provider = tokens.Provider,
						UserId = tokens.UserId,
					};
					var idx = IndexProviderUserIdHash(tokens.Provider);
					redis.SetEntryInHash(idx, tokens.UserId, oauthProvider.Id.ToString());
				}

				oauthProvider.PopulateMissing(tokens);
				userAuth.PopulateMissing(oauthProvider);

				redis.Store(userAuth);
				redis.Store(oauthProvider);
				redis.AddItemToSet(IndexUserAuthAndProviderIdsSet(userAuth.Id), oauthProvider.Id.ToString());

				return userAuth.Id.ToString();
			}
		}

		private string GetAuthProviderByUserId(IRedisClientFacade redis, string provider, string userId)
		{
			var idx = IndexProviderUserIdHash(provider);
			var oAuthProviderId = redis.GetValueFromHash(idx, userId);
			return oAuthProviderId;
		}

		public void Clear()
		{
			this.factory.Clear();
		}
	}

}