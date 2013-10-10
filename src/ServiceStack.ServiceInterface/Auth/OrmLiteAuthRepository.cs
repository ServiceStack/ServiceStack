using System;
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
        private readonly IHashProvider passwordHasher;

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory)
            : this(dbFactory, new SaltedHash())
        { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher)
        {
            this.dbFactory = dbFactory;
            this.passwordHasher = passwordHasher;
        }

        public void CreateMissingTables()
        {
            dbFactory.Run(db => {
                db.CreateTable<UserAuth>(false);
                db.CreateTable<UserOAuthProvider>(false);
            });
        }

        public void DropAndReCreateTables()
        {
            dbFactory.Run(db =>
            {
                db.CreateTable<UserAuth>(true);
                db.CreateTable<UserOAuthProvider>(true);
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

            return dbFactory.Run(db => {
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

                db.Insert(newUser);

                newUser = db.GetById<UserAuth>(db.GetLastInsertId());
                return newUser;
            });
        }

        private static void AssertNoExistingUser(IDbConnection db, UserAuth newUser, UserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
            }
        }

        public UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            return dbFactory.Run(db => {
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

                db.Save(newUser);

                return newUser;
            });
        }

        public UserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            return dbFactory.Run(db => GetUserAuthByUserName(db, userNameOrEmail));
        }

        private static UserAuth GetUserAuthByUserName(IDbConnection db, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var userAuth = isEmail
                ? db.Select<UserAuth>(q => q.Email == userNameOrEmail).FirstOrDefault()
                : db.Select<UserAuth>(q => q.UserName == userNameOrEmail).FirstOrDefault();

            return userAuth;
        }

        public bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) return false;

            if (passwordHasher.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string,string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out UserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) return false;

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders,PrivateKey,NonceTimeOut,userAuth.DigestHa1Hash,sequence))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            userAuth = null;
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

            var idSesije = session.Id;  //first record session Id (original session Id)
            session.PopulateWith(userAuth); //here, original sessionId is overwritten with facebook user Id
            session.Id = idSesije;  //we return Id of original session here

            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserOAuthProviders(session.UserAuthId)
                .ConvertAll(x => (IOAuthTokens)x);
            
        }

        public UserAuth GetUserAuth(string userAuthId)
        {
            return dbFactory.Run(db => db.GetByIdOrDefault<UserAuth>(userAuthId));
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            dbFactory.Run(db => {

                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? db.GetByIdOrDefault<UserAuth>(authSession.UserAuthId)
                    : authSession.TranslateTo<UserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                    userAuth.Id = int.Parse(authSession.UserAuthId);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                db.Save(userAuth);
            });
        }

        public void SaveUserAuth(UserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            dbFactory.Run(db => db.Save(userAuth));
        }

        public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
        {
            var id = int.Parse(userAuthId);
            return dbFactory.Run(db =>
                db.Select<UserOAuthProvider>(q => q.UserAuthId == id)).OrderBy(x => x.ModifiedDate).ToList();
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

            return dbFactory.Run(db => {
                var oAuthProvider = db.Select<UserOAuthProvider>(q => 
                    q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = db.GetByIdOrDefault<UserAuth>(oAuthProvider.UserAuthId);
                    return userAuth;
                }
                return null;
            });
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();

            return dbFactory.Run(db => {

                var oAuthProvider = db.Select<UserOAuthProvider>(q =>
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

                db.Save(userAuth);

                oAuthProvider.UserAuthId = userAuth.Id != default(int)
                    ? userAuth.Id
                    : (int)db.GetLastInsertId();

                if (oAuthProvider.CreatedDate == default(DateTime))
                    oAuthProvider.CreatedDate = userAuth.ModifiedDate;
                oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

                db.Save(oAuthProvider);

                return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
            });
        }

        public void Clear()
        {
            dbFactory.Run(db => {
                db.DeleteAll<UserAuth>();
                db.DeleteAll<UserOAuthProvider>();
            });
        }
    }
}
