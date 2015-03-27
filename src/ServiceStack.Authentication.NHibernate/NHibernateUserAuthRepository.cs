using System;
using System.Collections.Generic;
using System.Linq;
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
        public static Func<ISessionFactory, ISession> GetCurrentSessionFn = GetCurrentSession;

        public static ISession GetCurrentSession(ISessionFactory sessionFactory)
        {
            return sessionFactory.GetCurrentSession();
        }

        public NHibernateUserAuthRepository(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
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

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                var oAuthProvider = nhSession.QueryOver<UserAuthDetailsNHibernate>()
                    .Where(x => x.Provider == tokens.Provider)
                    .And(x => x.UserId == tokens.UserId)
                    .SingleOrDefault();

                if (oAuthProvider != null)
                {
                    return nhSession.QueryOver<UserAuthNHibernate>()
                        .Where(x => x.Id == oAuthProvider.UserAuthId)
                        .SingleOrDefault();
                }
            }

            return null;
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                int authId = int.Parse(userAuthId);
                return nhSession.QueryOver<UserAuthNHibernate>()
                    .Where(x => x.Id == authId)
                    .SingleOrDefault();
            }
        }

        private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
        {
            session.PopulateSession(userAuth,
                GetUserAuthDetails(session.UserAuthId).ConvertAll(x => (IAuthTokens)x));
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
            if (userNameOrEmail == null)
                return null;

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                UserAuthNHibernate user;
                if (userNameOrEmail.Contains("@"))
                {
                    user = nhSession.QueryOver<UserAuthNHibernate>()
                                    .Where(x => x.Email == userNameOrEmail)
                                    .SingleOrDefault();
                }
                else
                {
                    user = nhSession.QueryOver<UserAuthNHibernate>()
                                    .Where(x => x.UserName == userNameOrEmail)
                                    .SingleOrDefault();
                }
                return user;
            }
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuthNHibernate();

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                var authDetails = nhSession.QueryOver<UserAuthDetailsNHibernate>()
                    .Where(x => x.Provider == tokens.Provider)
                    .And(x => x.UserId == tokens.UserId)
                    .SingleOrDefault();

                if (authDetails == null)
                {
                    authDetails = new UserAuthDetailsNHibernate
                    {
                        Provider = tokens.Provider,
                        UserId = tokens.UserId,
                    };
                }

                authDetails.PopulateMissing(tokens);
                userAuth.PopulateMissingExtended(authDetails);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                nhSession.Save(userAuth);

                authDetails.UserAuthId = userAuth.Id;

                if (authDetails.CreatedDate == default(DateTime))
                    authDetails.CreatedDate = userAuth.ModifiedDate;
                authDetails.ModifiedDate = userAuth.ModifiedDate;

                nhSession.Save(authDetails);

                return authDetails;
            }
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                int authId = int.Parse(userAuthId);
                var value = nhSession.QueryOver<UserAuthDetailsNHibernate>()
                    .Where(x => x.UserAuthId == authId)
                    .OrderBy(x => x.ModifiedDate).Asc
                    .List();

                return value.Cast<IUserAuthDetails>().ToList();
            }
        }

        public void DeleteUserAuth(string userAuthId)
        {
            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                int authId = int.Parse(userAuthId);

                nhSession.Delete(nhSession.QueryOver<UserAuthNHibernate>()
                    .Where(x => x.Id == authId));

                nhSession.Delete(nhSession.QueryOver<UserAuthDetailsNHibernate>()
                    .Where(x => x.UserAuthId == authId));
            }
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

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                nhSession.Save(new UserAuthNHibernate(newUser));
            }

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

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                nhSession.Save(new UserAuthNHibernate(userAuth));
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                var userAuth = !string.IsNullOrEmpty(authSession.UserAuthId)
                    ? nhSession.Load<UserAuthNHibernate>(int.Parse(authSession.UserAuthId))
                    : authSession.ConvertTo<UserAuth>();

                if (userAuth.Id == default(int) && !string.IsNullOrEmpty(authSession.UserAuthId))
                    userAuth.Id = int.Parse(authSession.UserAuthId);

                userAuth.ModifiedDate = userAuth.ModifiedDate;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                nhSession.Save(new UserAuthNHibernate(userAuth));
            }
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

            using (var nhSession = GetCurrentSessionFn(sessionFactory))
            {
                nhSession.Save(new UserAuthNHibernate(newUser));
            }

            return newUser;
        }
    }
}