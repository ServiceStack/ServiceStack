using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Common;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
	public class RedisAuthRepository : IUserAuthRepository, IClearable
	{
		//http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
		public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

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

		private string IndexProviderToUserIdHash(string provider)
		{
			return "hash:ProviderUserId>OAuthProviderId:" + provider;
		}

		private string IndexUserNameToUserId
		{
			get
			{
				return "hash:UserAuth:UserName>UserId";
			}
		}

		private string IndexEmailToUserId
		{
			get
			{
				return "hash:UserAuth:Email>UserId";
			}
		}

		public UserAuth CreateUserAuth(UserAuth newUser, string password)
		{
			newUser.ThrowIfNull("newUser");
			password.ThrowIfNullOrEmpty("password");

			if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
				throw new ArgumentNullException("UserName or Email is required");

			if (!newUser.UserName.IsNullOrEmpty())
			{
				if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
					throw new ArgumentException("UserName contains invalid characters", "UserName");
			}

			using (var redis = factory.GetClient())
			{
				var effectiveUserName = newUser.UserName ?? newUser.Email;
				var userAuth = GetUserAuthByUserName(redis, newUser.UserName);
				if (userAuth != null)
					throw new ArgumentException("User {0} already exists".Fmt(effectiveUserName));

				var saltedHash = new SaltedHash();
				string salt;
				string hash;
				saltedHash.GetHashAndSaltString(password, out hash, out salt);

				newUser.PasswordHash = hash;
				newUser.Salt = salt;

				newUser.Id = redis.As<UserAuth>().GetNextSequence();
	
				if (!newUser.UserName.IsNullOrEmpty())
					redis.SetEntryInHash(IndexUserNameToUserId, newUser.UserName, newUser.Id.ToString());
				if (!newUser.Email.IsNullOrEmpty())
					redis.SetEntryInHash(IndexEmailToUserId, newUser.Email, newUser.Id.ToString());

				redis.Store(newUser);

			    return newUser;
			}
		}

		public UserAuth GetUserAuthByUserName(string userNameOrEmail)
		{
			using (var redis = factory.GetClient())
			{
				return GetUserAuthByUserName(redis, userNameOrEmail);
			}
		} 

		private UserAuth GetUserAuthByUserName(IRedisClientFacade redis, string userNameOrEmail)
		{
			var isEmail = userNameOrEmail.Contains("@");
			var userId = isEmail
				? redis.GetValueFromHash(IndexEmailToUserId, userNameOrEmail)
				: redis.GetValueFromHash(IndexUserNameToUserId, userNameOrEmail);

			return userId == null ? null : redis.As<UserAuth>().GetById(userId);
		}

		public bool TryAuthenticate(string userName, string password, out string userId)
		{
			userId = null;
			var userAuth = GetUserAuthByUserName(userName);
			if (userAuth == null) return false;

			var saltedHash = new SaltedHash();
			if (saltedHash.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
			{
				userId = userAuth.Id.ToString();
				return true;
			}
			return false;
		}

		public virtual void LoadUserAuth(IAuthSession session, IOAuthTokens tokens)
		{
			session.ThrowIfNull("session");

			var userAuth = GetUserAuth(session, tokens);
			LoadUserAuth(session, userAuth);
		}

		private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
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

		public void SaveUserAuth(IAuthSession authSession)
		{
			using (var redis = factory.GetClient())
			{
				var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
					? GetUserAuth(redis, authSession.UserAuthId)
					: authSession.TranslateTo<UserAuth>();

				if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
					userAuth.Id = int.Parse(authSession.UserAuthId);

				redis.Store(userAuth);
			}
		}

		public void SaveUserAuth(UserAuth userAuth)
		{
			using (var redis = factory.GetClient())
				redis.Store(userAuth);
		}

		public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
		{
			userAuthId.ThrowIfNullOrEmpty("userAuthId");

			using (var redis = factory.GetClient())
			{
				var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
				var authProiverIds = redis.GetAllItemsFromSet(idx);
				return redis.As<UserOAuthProvider>().GetByIds(authProiverIds).ToList();
			}
		}

		public UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
		{
			using (var redis = factory.GetClient())
				return GetUserAuth(redis, authSession, tokens);
		}

		private UserAuth GetUserAuth(IRedisClientFacade redis, IAuthSession authSession, IOAuthTokens tokens)
		{
			if (!authSession.UserAuthId.IsNullOrEmpty())
			{
				var userAuth = GetUserAuth(redis, authSession.UserAuthId);
				if (userAuth != null) return userAuth;
			}
			if (!authSession.UserName.IsNullOrEmpty())
			{
				var userAuth = GetUserAuthByUserName(authSession.UserName);
				if (userAuth != null) return userAuth;
			}

			if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty()) return null;

			var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
			if (!oAuthProviderId.IsNullOrEmpty())
			{
				var oauthProvider = redis.As<UserOAuthProvider>().GetById(oAuthProviderId);
				if (oauthProvider != null)
					return redis.As<UserAuth>().GetById(oauthProvider.UserAuthId);
			}
			return null;
		} 

		public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
		{
			using (var redis = factory.GetClient())
			{
				UserOAuthProvider oAuthProvider = null;

				var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
				if (!oAuthProviderId.IsNullOrEmpty())
					oAuthProvider = redis.As<UserOAuthProvider>().GetById(oAuthProviderId);

				var userAuth = GetUserAuth(redis, authSession, tokens) 
					?? new UserAuth { Id = redis.As<UserAuth>().GetNextSequence(), };
				 
				if (oAuthProvider == null)
				{
					oAuthProvider = new UserOAuthProvider {
						Id = redis.As<UserOAuthProvider>().GetNextSequence(),
						UserAuthId = userAuth.Id,
						Provider = tokens.Provider,
						UserId = tokens.UserId,
					};
					var idx = IndexProviderToUserIdHash(tokens.Provider);
					redis.SetEntryInHash(idx, tokens.UserId, oAuthProvider.Id.ToString());
				}

				oAuthProvider.PopulateMissing(tokens);
				userAuth.PopulateMissing(oAuthProvider);

				redis.Store(userAuth);
				redis.Store(oAuthProvider);
				redis.AddItemToSet(IndexUserAuthAndProviderIdsSet(userAuth.Id), oAuthProvider.Id.ToString());

				return userAuth.Id.ToString();
			}
		}

		private string GetAuthProviderByUserId(IRedisClientFacade redis, string provider, string userId)
		{
			var idx = IndexProviderToUserIdHash(provider);
			var oAuthProviderId = redis.GetValueFromHash(idx, userId);
			return oAuthProviderId;
		}

		public void Clear()
		{
			this.factory.Clear();
		}
	}

}