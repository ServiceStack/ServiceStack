using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using ServiceStack.Auth;
using NHibernate;

namespace ServiceStack.Authentication.NHibernate
{
    /// <summary>
    /// Originally from: https://gist.github.com/2000863 by https://github.com/joshilewis
    /// </summary>
    public class NHibernateUserAuthRepository : IUserAuthRepository
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly ISessionFactory sessionFactory;

        public NHibernateUserAuthRepository(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        protected ISession Session
        {
            get { return sessionFactory.GetCurrentSession(); }
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, (UserAuth)userAuth);
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!string.IsNullOrEmpty(authSession.UserAuthId))
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null) return userAuth;
            }

            if (!string.IsNullOrEmpty(authSession.UserAuthName))
            {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null) return userAuth;
            }

            if (tokens == null || string.IsNullOrEmpty(tokens.Provider) || string.IsNullOrEmpty(tokens.UserId))
                return null;

            var oAuthProvider = Session.QueryOver<UserAuthDetailsNHibernate>()
                .Where(x => x.Provider == tokens.Provider)
                .And(x => x.UserId == tokens.UserId)
                .SingleOrDefault();

            if (oAuthProvider != null)
            {
                return Session.QueryOver<UserAuthNHibernate>()
                    .Where(x => x.Id == oAuthProvider.UserAuthId)
                    .SingleOrDefault();
            }

            return null;
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            int authId = int.Parse(userAuthId);
            return Session.QueryOver<UserAuthNHibernate>()
                .Where(x => x.Id == authId)
                .SingleOrDefault();
        }

        private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
        {
            if (userAuth == null) return;

            session.PopulateWith(userAuth);
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserAuthDetails(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens)x);
        }

        public bool TryAuthenticate(string userName, string password, out string userId)
        {
            userId = null;
            IUserAuth userAuth;
            if (TryAuthenticate(userName, password, out userAuth))
            {
                userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            return false;  
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) return false;

            var saltedHash = new SaltedHash();
            return saltedHash.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt);
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) return false;

            var digestHelper = new DigestAuthFunctions();
            return digestHelper.ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHa1Hash, sequence);
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            UserAuthNHibernate user = null;
            if (userNameOrEmail.Contains("@"))
            {
                user = Session.QueryOver<UserAuthNHibernate>()
                    .Where(x => x.Email == userNameOrEmail)
                    .SingleOrDefault();
            }
            else
            {
                user = Session.QueryOver<UserAuthNHibernate>()
                    .Where(x => x.UserName == userNameOrEmail)
                    .SingleOrDefault();
            }

            return user;
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuthNHibernate();

            var oAuthProvider = Session.QueryOver<UserAuthDetailsNHibernate>()
                .Where(x => x.Provider == tokens.Provider)
                .And(x => x.UserId == tokens.UserId)
                .SingleOrDefault();

            if (oAuthProvider == null)
            {
                oAuthProvider = new UserAuthDetailsNHibernate {
                    Provider = tokens.Provider,
                    UserId = tokens.UserId,
                };
            }

            oAuthProvider.PopulateMissing(tokens);
            userAuth.PopulateMissingExtended(oAuthProvider);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Session.Save(userAuth);

            oAuthProvider.UserAuthId = userAuth.Id;

            if (oAuthProvider.CreatedDate == default(DateTime))
                oAuthProvider.CreatedDate = userAuth.ModifiedDate;
            oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

            Session.Save(oAuthProvider);

            return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            int authId = int.Parse(userAuthId);
            var value = Session.QueryOver<UserAuthDetailsNHibernate>()
                .Where(x => x.UserAuthId == authId)
                .OrderBy(x => x.ModifiedDate).Asc
                .List();

            var providerList = new List<IUserAuthDetails>();
            foreach (var item in value)
            {
                providerList.Add(item);
            }

            return providerList;
            //return new List<UserAuthDetails>(value);
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(newUser);

            var saltedHash = new SaltedHash();
            string salt;
            string hash;
            saltedHash.GetHashAndSaltString(password, out hash, out salt);

            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            Session.Save(new UserAuthNHibernate(newUser));
            return newUser;
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (string.IsNullOrEmpty(newUser.UserName) && string.IsNullOrEmpty(newUser.Email))
                throw new ArgumentNullException("UserName or Email is required");

            if (!string.IsNullOrEmpty(newUser.UserName))
            {
                if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
            }
        }

        private void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format("User {0} already exists", newUser.UserName));
            }

            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format("Email {0} already exists", newUser.Email));
            }
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Session.Save(new UserAuthNHibernate(userAuth));
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !string.IsNullOrEmpty(authSession.UserAuthId)
                ? Session.Load<UserAuthNHibernate>(int.Parse(authSession.UserAuthId))
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default(int) && !string.IsNullOrEmpty(authSession.UserAuthId))
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = userAuth.ModifiedDate;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Session.Save(new UserAuthNHibernate(userAuth));
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(newUser, existingUser);

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

            Session.Save(new UserAuthNHibernate(newUser));

            return newUser;
        }
    }
}