using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class RedisAuthRepository : RedisAuthRepository<UserAuth, UserAuthProvider>
    {
        public RedisAuthRepository(IRedisClientsManager factory) : base(factory) { }

        public RedisAuthRepository(IRedisClientManagerFacade factory) : base(factory) { }
    }

    public class RedisAuthRepository<TUserAuth, TUserAuthProvider> : IUserAuthRepository<TUserAuth>, IClearable
        where TUserAuth : class, IUserAuth, new()
        where TUserAuthProvider : class, IUserAuthProvider, new()
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly IRedisClientManagerFacade factory;

        public RedisAuthRepository(IRedisClientsManager factory)
            : this(new RedisClientManagerFacade(factory)) { }

        public RedisAuthRepository(IRedisClientManagerFacade factory) { this.factory = factory; }

        public string NamespacePrefix { get; set; }

        private string UsePrefix { get { return NamespacePrefix ?? ""; } }

        private string IndexUserAuthAndProviderIdsSet(long userAuthId) { return UsePrefix + "urn:UserAuth>UserOAuthProvider:" + userAuthId; }

        private string IndexProviderToUserIdHash(string provider) { return UsePrefix + "hash:ProviderUserId>OAuthProviderId:" + provider; }

        private string IndexUserNameToUserId { get { return UsePrefix + "hash:UserAuth:UserName>UserId"; } }

        private string IndexEmailToUserId { get { return UsePrefix + "hash:UserAuth:Email>UserId"; } }

        private void ValidateNewUser(TUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty()) {
                throw new ArgumentNullException("UserName or Email is required");
            }

            if (!newUser.UserName.IsNullOrEmpty()) {
                if (!ValidUserNameRegEx.IsMatch(newUser.UserName)) {
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
                }
            }
        }

        private void AssertNoExistingUser(IRedisClientFacade redis, TUserAuth newUser, TUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null) {
                var existingUser = GetUserAuthByUserName(redis, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id)) {
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
                }
            }
            if (newUser.Email != null) {
                var existingUser = GetUserAuthByUserName(redis, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id)) {
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
                }
            }
        }

        public virtual TUserAuth CreateUserAuth(TUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            using (var redis = factory.GetClient()) {
                AssertNoExistingUser(redis, newUser);

                var saltedHash = new SaltedHash();
                string salt;
                string hash;
                saltedHash.GetHashAndSaltString(password, out hash, out salt);

                newUser.Id = redis.As<TUserAuth>().GetNextSequence();
                newUser.PasswordHash = hash;
                newUser.Salt = salt;
                var digestHelper = new DigestAuthFunctions();
                newUser.DigestHA1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                newUser.CreatedDate = DateTime.UtcNow;
                newUser.ModifiedDate = newUser.CreatedDate;

                var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
                if (!newUser.UserName.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexUserNameToUserId, newUser.UserName, userId);
                }
                if (!newUser.Email.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexEmailToUserId, newUser.Email, userId);
                }

                redis.Store(newUser);

                return newUser;
            }
        }

        public TUserAuth UpdateUserAuth(TUserAuth existingUser, TUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            using (var redis = factory.GetClient()) {
                AssertNoExistingUser(redis, newUser, existingUser);

                if (existingUser.UserName != newUser.UserName && existingUser.UserName != null) {
                    redis.RemoveEntryFromHash(IndexUserNameToUserId, existingUser.UserName);
                }
                if (existingUser.Email != newUser.Email && existingUser.Email != null) {
                    redis.RemoveEntryFromHash(IndexEmailToUserId, existingUser.Email);
                }

                var hash = existingUser.PasswordHash;
                var salt = existingUser.Salt;
                if (password != null) {
                    var saltedHash = new SaltedHash();
                    saltedHash.GetHashAndSaltString(password, out hash, out salt);
                }
                // If either one changes the digest hash has to be recalculated
                var digestHash = existingUser.DigestHA1Hash;
                if (password != null || existingUser.UserName != newUser.UserName) {
                    var digestHelper = new DigestAuthFunctions();
                    digestHash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }

                newUser.Id = existingUser.Id;
                newUser.PasswordHash = hash;
                newUser.Salt = salt;
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
                if (!newUser.UserName.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexUserNameToUserId, newUser.UserName, userId);
                }
                if (!newUser.Email.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexEmailToUserId, newUser.Email, userId);
                }

                redis.Store(newUser);

                return newUser;
            }
        }

        public virtual TUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            using (var redis = factory.GetClient()) {
                return GetUserAuthByUserName(redis, userNameOrEmail);
            }
        }

        private TUserAuth GetUserAuthByUserName(IRedisClientFacade redis, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var userId = isEmail
                ? redis.GetValueFromHash(IndexEmailToUserId, userNameOrEmail)
                : redis.GetValueFromHash(IndexUserNameToUserId, userNameOrEmail);

            return userId == null ? null : redis.As<TUserAuth>().GetById(userId);
        }

        public virtual bool TryAuthenticate(string userName, string password, out TUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) {
                return false;
            }

            var saltedHash = new SaltedHash();
            if (saltedHash.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt)) {
                return true;
            }

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out TUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) {
                return false;
            }

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHA1Hash, sequence)) {
                return true;
            }
            userAuth = null;
            return false;
        }

        public virtual void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, TUserAuth userAuth)
        {
            if (userAuth == null) {
                return;
            }

            session.PopulateWith(userAuth);
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserOAuthProviders(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens) x);
        }

        private TUserAuth GetUserAuth(IRedisClientFacade redis, string userAuthId)
        {
            long longId;
            if (userAuthId == null || !long.TryParse(userAuthId, out longId)) {
                return null;
            }

            return redis.As<TUserAuth>().GetById(longId);
        }

        public TUserAuth GetUserAuth(string userAuthId)
        {
            using (var redis = factory.GetClient()) {
                return GetUserAuth(redis, userAuthId);
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using (var redis = factory.GetClient()) {
                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? GetUserAuth(redis, authSession.UserAuthId)
                    : authSession.ConvertTo<TUserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty()) {
                    userAuth.Id = int.Parse(authSession.UserAuthId);
                }

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime)) {
                    userAuth.CreatedDate = userAuth.ModifiedDate;
                }

                redis.Store(userAuth);
            }
        }

        public void SaveUserAuth(TUserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime)) {
                userAuth.CreatedDate = userAuth.ModifiedDate;
            }

            using (var redis = factory.GetClient()) {
                redis.Store(userAuth);

                var userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                if (!userAuth.UserName.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexUserNameToUserId, userAuth.UserName, userId);
                }
                if (!userAuth.Email.IsNullOrEmpty()) {
                    redis.SetEntryInHash(IndexEmailToUserId, userAuth.Email, userId);
                }
            }
        }

        public List<IUserAuthProvider> GetUserOAuthProviders(string userAuthId)
        {
            userAuthId.ThrowIfNullOrEmpty("userAuthId");

            using (var redis = factory.GetClient()) {
                var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
                var authProiverIds = redis.GetAllItemsFromSet(idx);
                return redis.As<TUserAuthProvider>().GetByIds(authProiverIds).OrderBy(x => x.ModifiedDate).Cast<IUserAuthProvider>().ToList();
            }
        }

        public virtual TUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            using (var redis = factory.GetClient()) {
                return GetUserAuth(redis, authSession, tokens);
            }
        }

        private TUserAuth GetUserAuth(IRedisClientFacade redis, IAuthSession authSession, IAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty()) {
                var userAuth = GetUserAuth(redis, authSession.UserAuthId);
                if (userAuth != null) {
                    return userAuth;
                }
            }
            if (!authSession.UserAuthName.IsNullOrEmpty()) {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null) {
                    return userAuth;
                }
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty()) {
                return null;
            }

            var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
            if (!oAuthProviderId.IsNullOrEmpty()) {
                var oauthProvider = redis.As<TUserAuthProvider>().GetById(oAuthProviderId);
                if (oauthProvider != null) {
                    return redis.As<TUserAuth>().GetById(oauthProvider.UserAuthId);
                }
            }
            return null;
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            using (var redis = factory.GetClient()) {
                TUserAuthProvider authProvider = null;

                var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
                if (!oAuthProviderId.IsNullOrEmpty()) {
                    authProvider = redis.As<TUserAuthProvider>().GetById(oAuthProviderId);
                }

                var userAuth = GetUserAuth(redis, authSession, tokens)
                               ?? new TUserAuth {Id = redis.As<TUserAuth>().GetNextSequence(),};

                if (authProvider == null) {
                    authProvider = new TUserAuthProvider {
                        Id = redis.As<TUserAuthProvider>().GetNextSequence(),
                        UserAuthId = userAuth.Id,
                        Provider = tokens.Provider,
                        UserId = tokens.UserId,
                    };
                    var idx = IndexProviderToUserIdHash(tokens.Provider);
                    redis.SetEntryInHash(idx, tokens.UserId, authProvider.Id.ToString(CultureInfo.InvariantCulture));
                }

                authProvider.PopulateMissing(tokens);
                userAuth.PopulateMissing(authProvider);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (authProvider.CreatedDate == default(DateTime)) {
                    authProvider.CreatedDate = userAuth.ModifiedDate;
                }
                authProvider.ModifiedDate = userAuth.ModifiedDate;

                redis.Store(userAuth);
                redis.Store(authProvider);
                redis.AddItemToSet(IndexUserAuthAndProviderIdsSet(userAuth.Id), authProvider.Id.ToString(CultureInfo.InvariantCulture));

                return userAuth.Id.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string GetAuthProviderByUserId(IRedisClientFacade redis, string provider, string userId)
        {
            var idx = IndexProviderToUserIdHash(provider);
            var oAuthProviderId = redis.GetValueFromHash(idx, userId);
            return oAuthProviderId;
        }

        public void Clear() { factory.Clear(); }

        IUserAuth IAuthRepository.GetUserAuth(IAuthSession authSession, IAuthTokens tokens) { return GetUserAuth(authSession, tokens); }

        IUserAuth IAuthRepository.GetUserAuthByUserName(string userNameOrEmail) { return GetUserAuthByUserName(userNameOrEmail); }

        void IAuthRepository.SaveUserAuth(IUserAuth userAuth) { SaveUserAuth((TUserAuth) userAuth); }

        bool IAuthRepository.TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            TUserAuth auth;
            if (TryAuthenticate(userName, password, out auth)) {
                userAuth = auth;
                return true;
            }
            userAuth = null;
            return false;
        }

        bool IAuthRepository.TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            TUserAuth auth;
            if (TryAuthenticate(digestHeaders, privateKey, nonceTimeOut, sequence, out auth)) {
                userAuth = auth;
                return true;
            }
            userAuth = null;
            return false;
        }
    }
}