﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
	public class OrmLiteAuthRepository : IUserAuthRepository, IClearable
	{
		//http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
		public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

		private readonly IDbConnectionFactory dbFactory;

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

		public void DropAndReCreateTables()
		{
			dbFactory.Exec(dbCmd => {
				dbCmd.CreateTable<UserAuth>(true);
				dbCmd.CreateTable<UserOAuthProvider>(true);
			});
		}

		private void ValidateNewUser(UserAuth newUser, string password)
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
		}

		public UserAuth CreateUserAuth(UserAuth newUser, string password)
		{
			ValidateNewUser(newUser, password);

			return dbFactory.Exec(dbCmd => {
				AssertNoExistingUser(dbCmd, newUser);

				var saltedHash = new SaltedHash();
				string salt;
				string hash;
				saltedHash.GetHashAndSaltString(password, out hash, out salt);

				newUser.PasswordHash = hash;
				newUser.Salt = salt;
				newUser.CreatedDate = DateTime.UtcNow;
				newUser.ModifiedDate = newUser.CreatedDate;

				dbCmd.Insert(newUser);

				newUser = dbCmd.GetById<UserAuth>(dbCmd.GetLastInsertId());
				return newUser;
			});
		}

		private static void AssertNoExistingUser(IDbCommand dbCmd, UserAuth newUser, UserAuth exceptForExistingUser = null)
		{
			if (newUser.UserName != null)
			{
				var existingUser = GetUserAuthByUserName(dbCmd, newUser.UserName);
				if (existingUser != null
					&& (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
					throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
			}
			if (newUser.Email != null)
			{
				var existingUser = GetUserAuthByUserName(dbCmd, newUser.Email);
				if (existingUser != null
					&& (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
					throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
			}
		}

		public UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password)
		{
			ValidateNewUser(newUser, password);

			return dbFactory.Exec(dbCmd => {
				AssertNoExistingUser(dbCmd, newUser, existingUser);

				var hash = existingUser.PasswordHash;
				var salt = existingUser.Salt;
				if (password != null)
				{
					var saltedHash = new SaltedHash();
					saltedHash.GetHashAndSaltString(password, out hash, out salt);
				}

				newUser.Id = existingUser.Id;
				newUser.PasswordHash = hash;
				newUser.Salt = salt;
				newUser.CreatedDate = existingUser.CreatedDate;
				newUser.ModifiedDate = DateTime.UtcNow;

				dbCmd.Save(newUser);

				return newUser;
			});
		}

		public UserAuth GetUserAuthByUserName(string userNameOrEmail)
		{
			return dbFactory.Exec(dbCmd => GetUserAuthByUserName(dbCmd, userNameOrEmail));
		}

		private static UserAuth GetUserAuthByUserName(IDbCommand dbCmd, string userNameOrEmail)
		{
			var isEmail = userNameOrEmail.Contains("@");
			var userAuth = isEmail
				? dbCmd.Select<UserAuth>(q => q.Email == userNameOrEmail).FirstOrDefault()
				: dbCmd.Select<UserAuth>(q => q.UserName == userNameOrEmail).FirstOrDefault();

			return userAuth;
		}

		public bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
		{
			//userId = null;
			userAuth = GetUserAuthByUserName(userName);
			if (userAuth == null) return false;

			var saltedHash = new SaltedHash();
			if (saltedHash.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
			{
				//userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
				return true;
			}
			return false;
		}

		public void LoadUserAuth(IAuthSession session, IOAuthTokens tokens)
		{
			session.ThrowIfNull("session");

			var userAuth = GetUserAuth(session, tokens);
			LoadUserAuth(session, userAuth);
		}

		private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
		{
			if (userAuth == null) return;

			session.PopulateWith(userAuth);
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserOAuthProviders(session.UserAuthId)
				.ConvertAll(x => (IOAuthTokens)x);
			
		}

		public UserAuth GetUserAuth(string userAuthId)
		{
			return dbFactory.Exec(dbCmd => dbCmd.GetByIdOrDefault<UserAuth>(userAuthId));
		}

		public void SaveUserAuth(IAuthSession authSession)
		{
			dbFactory.Exec(dbCmd => {

				var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
					? dbCmd.GetByIdOrDefault<UserAuth>(authSession.UserAuthId)
					: authSession.TranslateTo<UserAuth>();

				if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
					userAuth.Id = int.Parse(authSession.UserAuthId);

				userAuth.ModifiedDate = userAuth.ModifiedDate;
				if (userAuth.CreatedDate == default(DateTime))
					userAuth.CreatedDate = userAuth.ModifiedDate;

				dbCmd.Save(userAuth);
			});
		}

		public void SaveUserAuth(UserAuth userAuth)
		{
			userAuth.ModifiedDate = DateTime.UtcNow;
			if (userAuth.CreatedDate == default(DateTime))
				userAuth.CreatedDate = userAuth.ModifiedDate;

			dbFactory.Exec(dbCmd => dbCmd.Save(userAuth));
		}

		public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
		{
			var id = int.Parse(userAuthId);
			return dbFactory.Exec(dbCmd =>
				dbCmd.Select<UserOAuthProvider>(q => q.UserAuthId == id)).OrderBy(x => x.ModifiedDate).ToList();
		}

		public UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
		{
			if (!authSession.UserAuthId.IsNullOrEmpty())
			{
				var userAuth = GetUserAuth(authSession.UserAuthId);
				if (userAuth != null) return userAuth;
			}
			if (!authSession.UserAuthName.IsNullOrEmpty())
			{
				var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
				if (userAuth != null) return userAuth;
			}

			if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
				return null;

			return dbFactory.Exec(dbCmd => {
				var oAuthProvider = dbCmd.Select<UserOAuthProvider>(q => 
					q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

				if (oAuthProvider != null)
				{
					var userAuth = dbCmd.GetByIdOrDefault<UserAuth>(oAuthProvider.UserAuthId);
					return userAuth;
				}
				return null;
			});
		}

		public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
		{
			var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();

			return dbFactory.Exec(dbCmd => {

				var oAuthProvider = dbCmd.Select<UserOAuthProvider>(q =>
					q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

				if (oAuthProvider == null)
				{
					oAuthProvider = new UserOAuthProvider {
						Provider = tokens.Provider,
						UserId = tokens.UserId,
					};
				}

				oAuthProvider.PopulateMissing(tokens);
				userAuth.PopulateMissing(oAuthProvider);

				userAuth.ModifiedDate = DateTime.UtcNow;
				if (userAuth.CreatedDate == default(DateTime))
					userAuth.CreatedDate = userAuth.ModifiedDate;

				dbCmd.Save(userAuth);

				oAuthProvider.UserAuthId = userAuth.Id != default(int)
					? userAuth.Id
					: (int)dbCmd.GetLastInsertId();

				if (oAuthProvider.CreatedDate == default(DateTime))
					oAuthProvider.CreatedDate = userAuth.ModifiedDate;
				oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

				dbCmd.Save(oAuthProvider);

				return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
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
