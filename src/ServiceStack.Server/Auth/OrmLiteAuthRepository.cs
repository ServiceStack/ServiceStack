using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class OrmLiteAuthRepository : OrmLiteAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory) { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher) : base(dbFactory, passwordHasher) { }
    }

    public class OrmLiteAuthRepository<TUserAuth, TUserAuthProvider> : IUserAuthRepository, IClearable
        where TUserAuth : class, IUserAuth
        where TUserAuthProvider : class, IUserAuthDetails
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly IDbConnectionFactory dbFactory;
        private readonly IHashProvider passwordHasher;

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory)
            : this(dbFactory, new SaltedHash()) { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher)
        {
            this.dbFactory = dbFactory;
            this.passwordHasher = passwordHasher;
        }

        public void CreateMissingTables()
        {
            using (var db = dbFactory.Open())
            {
                db.CreateTable<TUserAuth>();
                db.CreateTable<TUserAuthProvider>();
            }
        }

        public void DropAndReCreateTables()
        {
            using (var db = dbFactory.Open())
            {
                db.DropAndCreateTable<TUserAuth>();
                db.DropAndCreateTable<TUserAuthProvider>();
            }
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
            {
                throw new ArgumentNullException("UserName or Email is required");
            }

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
                {
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
                }
            }
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            using (var db = dbFactory.Open())
            {
                AssertNoExistingUser(db, newUser);

                string salt;
                string hash;
                passwordHasher.GetHashAndSaltString(password, out hash, out salt);
                var digestHelper = new DigestAuthFunctions();
                newUser.DigestHa1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                newUser.PasswordHash = hash;
                newUser.Salt = salt;
                newUser.CreatedDate = DateTime.UtcNow;
                newUser.ModifiedDate = newUser.CreatedDate;

                db.Save((TUserAuth)newUser);

                newUser = db.SingleById<TUserAuth>(newUser.Id);
                return newUser;
            }
        }

        private static void AssertNoExistingUser(IDbConnection db, IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                {
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
                }
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                {
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
                }
            }
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            using (var db = dbFactory.Open())
            {
                AssertNoExistingUser(db, newUser, existingUser);

                var hash = existingUser.PasswordHash;
                var salt = existingUser.Salt;
                if (password != null)
                {
                    passwordHasher.GetHashAndSaltString(password, out hash, out salt);
                }
                // If either one changes the digest hash has to be recalculated
                var digestHash = existingUser.DigestHa1Hash;
                if (password != null || existingUser.UserName != newUser.UserName)
                {
                    var digestHelper = new DigestAuthFunctions();
                    digestHash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
                newUser.Id = existingUser.Id;
                newUser.PasswordHash = hash;
                newUser.Salt = salt;
                newUser.DigestHa1Hash = digestHash;
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                db.Save((TUserAuth)newUser);

                return newUser;
            }
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            using (var db = dbFactory.Open())
            {
                return GetUserAuthByUserName(db, userNameOrEmail);
            }
        }

        private static TUserAuth GetUserAuthByUserName(IDbConnection db, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var userAuth = isEmail
                ? db.Select<TUserAuth>(q => q.Email == userNameOrEmail).FirstOrDefault()
                : db.Select<TUserAuth>(q => q.UserName == userNameOrEmail).FirstOrDefault();

            return userAuth;
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null)
            {
                return false;
            }

            if (passwordHasher.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null)
            {
                return false;
            }

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHa1Hash, sequence))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            userAuth = null;
            return false;
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, IUserAuth userAuth)
        {
            if (userAuth == null)
            {
                return;
            }

            var idSesije = session.Id; //first record session Id (original session Id)
            session.PopulateWith(userAuth); //here, original sessionId is overwritten with facebook user Id
            session.Id = idSesije; //we return Id of original session here

            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserAuthDetails(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens)x);
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using (var db = dbFactory.Open())
            {
                return db.SingleById<TUserAuth>(userAuthId);
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using (var db = dbFactory.Open())
            {
                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? db.SingleById<TUserAuth>(authSession.UserAuthId)
                    : authSession.ConvertTo<TUserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                {
                    userAuth.Id = int.Parse(authSession.UserAuthId);
                }

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                {
                    userAuth.CreatedDate = userAuth.ModifiedDate;
                }

                db.Save(userAuth);
            }
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
            {
                userAuth.CreatedDate = userAuth.ModifiedDate;
            }

            using (var db = dbFactory.Open())
            {
                db.Save((TUserAuth)userAuth);
            }
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            var id = int.Parse(userAuthId);
            using (var db = dbFactory.Open())
            {
                return db.Select<TUserAuthProvider>(q => q.UserAuthId == id).OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
            }
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null)
                {
                    return userAuth;
                }
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null)
                {
                    return userAuth;
                }
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
            {
                return null;
            }

            using (var db = dbFactory.Open())
            {
                var oAuthProvider = db.Select<TUserAuthProvider>(
                    q =>
                        q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = db.SingleById<TUserAuth>(oAuthProvider.UserAuthId);
                    return userAuth;
                }
                return null;
            }
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            TUserAuth userAuth = (TUserAuth)GetUserAuth(authSession, tokens) 
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            using (var db = dbFactory.Open())
            {
                var oAuthProvider = db.Select<TUserAuthProvider>(
                    q => q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider == null)
                {
                    oAuthProvider = typeof(TUserAuthProvider).CreateInstance<TUserAuthProvider>();
                    oAuthProvider.Provider = tokens.Provider;
                    oAuthProvider.UserId = tokens.UserId;
                }

                oAuthProvider.PopulateMissing(tokens, overwriteReserved:true);
                userAuth.PopulateMissingExtended(oAuthProvider);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                {
                    userAuth.CreatedDate = userAuth.ModifiedDate;
                }

                db.Save(userAuth);

                oAuthProvider.UserAuthId = userAuth.Id;

                if (oAuthProvider.CreatedDate == default(DateTime))
                {
                    oAuthProvider.CreatedDate = userAuth.ModifiedDate;
                }
                oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

                db.Save(oAuthProvider);

                return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
            }
        }

        public void Clear()
        {
            using (var db = dbFactory.Open())
            {
                db.DeleteAll<TUserAuth>();
                db.DeleteAll<TUserAuthProvider>();
            }
        }

    }
}