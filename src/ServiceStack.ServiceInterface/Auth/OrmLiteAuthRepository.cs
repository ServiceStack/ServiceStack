using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Common;
using ServiceStack.OrmLite;

namespace ServiceStack.ServiceInterface.Auth
{
	public class OrmLiteAuthRepository : IUserAuthRepository, IClearable
	{
		//http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
		public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

		private IDbConnectionFactory dbFactory;

		public OrmLiteAuthRepository(IDbConnectionFactory dbFactory)
		{
			this.dbFactory = dbFactory;
		}

		public void CreateMissingTables()
		{
			dbFactory.Exec(dbCmd => {
				dbCmd.CreateTable<UserAuth>(false);
				dbCmd.CreateTable<UserOAuthProvider>(false);
			});
		}

		public UserAuth CreateUserAuth(UserAuth newUser, string password)
		{
			newUser.ThrowIfNull("newUser");
			newUser.UserName.ThrowIfNullOrEmpty("UserName");
			password.ThrowIfNullOrEmpty("PasswordHash");
			if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
				throw new ArgumentException("UserName contains invalid characters", "UserName");

			return dbFactory.Exec(dbCmd => {
				var existingUser = dbCmd.FirstOrDefault<UserAuth>("UserName = {0}", newUser.UserName);
				if (existingUser != null)
					throw new ArgumentException("User already exists", "UserName");

				var saltedHash = new SaltedHash();
				string salt;
				string hash;
				saltedHash.GetHashAndSaltString(password, out hash, out salt);

				newUser.PasswordHash = hash;
				newUser.Salt = salt;

				dbCmd.Insert(newUser);

				newUser = dbCmd.GetById<UserAuth>(dbCmd.GetLastInsertId());
				return newUser;
			});
		}

		public UserAuth GetUserAuthByUserName(string userNameOrEmail)
		{
			return dbFactory.Exec(dbCmd => {
				var isEmail = userNameOrEmail.Contains("@");
				var userAuth = isEmail
					? dbCmd.FirstOrDefault<UserAuth>("Email = {0}", userNameOrEmail)
					: dbCmd.FirstOrDefault<UserAuth>("UserName = {0}", userNameOrEmail);

				return userAuth;
			});
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

		public void LoadUserAuth(IOAuthSession session, IOAuthTokens tokens)
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

		public UserAuth GetUserAuth(string userAuthId)
		{
			return dbFactory.Exec(dbCmd => dbCmd.GetByIdOrDefault<UserAuth>(userAuthId));
		}

		public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
		{
			return dbFactory.Exec(dbCmd =>
				dbCmd.Select<UserOAuthProvider>("UserAuthId = {0}", userAuthId));
		}

		public UserAuth GetUserAuth(IOAuthSession authSession, IOAuthTokens tokens)
		{
			if (tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty()) return null;

			if (!authSession.UserAuthId.IsNullOrEmpty())
			{
				var userAuth = GetUserAuth(authSession.UserAuthId);
				if (userAuth != null) return userAuth;
			}

			return dbFactory.Exec(dbCmd => {
				var oAuthProvider = dbCmd.FirstOrDefault<UserOAuthProvider>(
					"Provider = {0} AND UserId = {1}", tokens.Provider, tokens.UserId);

				if (oAuthProvider != null)
				{
					var userAuth = dbCmd.GetByIdOrDefault<UserAuth>(oAuthProvider.UserAuthId);
					return userAuth;
				}
				return null;
			});
		}

		public string CreateOrMergeAuthSession(IOAuthSession oAuthSession, IOAuthTokens tokens)
		{
			var userAuth = GetUserAuth(oAuthSession, tokens) ?? new UserAuth();

			return dbFactory.Exec(dbCmd => {
				var oAuthProvider = dbCmd.FirstOrDefault<UserOAuthProvider>(
				"Provider = {0} AND UserId = {1}", tokens.Provider, tokens.UserId);

				if (oAuthProvider == null)
				{
					oAuthProvider = new UserOAuthProvider {
						Provider = tokens.Provider,
						UserId = tokens.UserId,
					};
				}

				oAuthProvider.PopulateMissing(tokens);
				userAuth.PopulateMissing(oAuthProvider);

				dbCmd.Save(userAuth);
				
				oAuthProvider.UserAuthId = userAuth.Id != default(int) 
					? userAuth.Id 
					: (int) dbCmd.GetLastInsertId();

				dbCmd.Save(oAuthProvider);

				return oAuthProvider.UserAuthId.ToString();
			});
		}

		public void Clear()
		{
			dbFactory.Exec(dbCmd => {
				dbCmd.DeleteAll<UserAuth>();
				dbCmd.DeleteAll<UserOAuthProvider>();
			});
		}
	}
}