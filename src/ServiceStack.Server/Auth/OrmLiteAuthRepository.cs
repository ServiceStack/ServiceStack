using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Auth
{
    public class OrmLiteAuthRepository : OrmLiteAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory) { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher) : base(dbFactory, passwordHasher) { }
    }

    public class OrmLiteAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IRequiresSchema, IClearable, IManageRoles
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        public int? MaxLoginAttempts { get; set; }

        private readonly IDbConnectionFactory dbFactory;
        private readonly IHashProvider passwordHasher;
        private bool hasInitSchema;

        public bool UseDistinctRoleTables { get; set; }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory)
            : this(dbFactory, new SaltedHash()) { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher)
        {
            this.dbFactory = dbFactory;
            this.passwordHasher = passwordHasher;
        }

        public void InitSchema()
        {
            hasInitSchema = true;
            using (var db = dbFactory.Open())
            {
                db.CreateTable<TUserAuth>();
                db.CreateTable<TUserAuthDetails>();
                db.CreateTable<UserAuthRole>();
            }
        }

        public void DropAndReCreateTables()
        {
            using (var db = dbFactory.Open())
            {
                db.DropAndCreateTable<TUserAuth>();
                db.DropAndCreateTable<TUserAuthDetails>();
                db.DropAndCreateTable<UserAuthRole>();
            }
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            ValidateNewUser(newUser);
        }

        private void ValidateNewUser(IUserAuth newUser)
        {
            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException(ErrorMessages.UsernameOrEmailRequired);

            if (!newUser.UserName.IsNullOrEmpty() && !ValidUserNameRegEx.IsMatch(newUser.UserName))
                throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
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

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            ValidateNewUser(newUser);

            using (var db = dbFactory.Open())
            {
                AssertNoExistingUser(db, newUser, existingUser);

                newUser.Id = existingUser.Id;
                newUser.PasswordHash = existingUser.PasswordHash;
                newUser.Salt = existingUser.Salt;
                newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                db.Save((TUserAuth)newUser);

                return newUser;
            }
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            if (!hasInitSchema)
            {
                using (var db = dbFactory.Open())
                {
                    hasInitSchema = db.TableExists<TUserAuth>();
                }
                if (!hasInitSchema) throw new Exception("OrmLiteAuthRepository Db tables have not been initialized. Try calling 'InitSchema()' in your AppHost Configure method.");
            }
            using (var db = dbFactory.Open())
            {
                return GetUserAuthByUserName(db, userNameOrEmail);
            }
        }

        private static TUserAuth GetUserAuthByUserName(IDbConnection db, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var userAuth = isEmail
                ? db.Select<TUserAuth>(q => q.Email.ToLower() == userNameOrEmail.ToLower()).FirstOrDefault()
                : db.Select<TUserAuth>(q => q.UserName.ToLower() == userNameOrEmail.ToLower()).FirstOrDefault();

            return userAuth;
        }

        protected virtual void RecordInvalidLoginAttempt(IUserAuth userAuth)
        {
            if (MaxLoginAttempts == null) return;

            userAuth.InvalidLoginAttempts += 1;
            userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.InvalidLoginAttempts >= MaxLoginAttempts.Value)
            {
                userAuth.LockedDate = userAuth.LastLoginAttempt;
            }
            SaveUserAuth(userAuth);
        }

        protected virtual void RecordSuccessfulLogin(IUserAuth userAuth)
        {
            if (MaxLoginAttempts == null) return;

            userAuth.InvalidLoginAttempts = 0;
            userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            SaveUserAuth(userAuth);
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null)
            {
                return false;
            }

            if (passwordHasher.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                RecordSuccessfulLogin(userAuth);

                return true;
            }

            RecordInvalidLoginAttempt(userAuth);

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
                RecordSuccessfulLogin(userAuth);

                return true;
            }

            RecordInvalidLoginAttempt(userAuth);

            userAuth = null;
            return false;
        }

        public void DeleteUserAuth(string userAuthId)
        {
            using (var db = dbFactory.Open())
            using (var trans = db.OpenTransaction())
            {
                var userId = int.Parse(userAuthId);

                db.Delete<TUserAuth>(x => x.Id == userId);
                db.Delete<TUserAuthDetails>(x => x.UserAuthId == userId);
                db.Delete<UserAuthRole>(x => x.UserAuthId == userId);

                trans.Commit();                
            }
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, IUserAuth userAuth)
        {
            session.PopulateSession(userAuth,
                GetUserAuthDetails(session.UserAuthId).ConvertAll(x => (IAuthTokens)x));
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using (var db = dbFactory.Open())
            {
                return db.SingleById<TUserAuth>(int.Parse(userAuthId));
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            if (authSession == null)
                throw new ArgumentNullException("authSession");

            using (var db = dbFactory.Open())
            {
                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? db.SingleById<TUserAuth>(int.Parse(authSession.UserAuthId))
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
            if (userAuth == null)
                throw new ArgumentNullException("userAuth");

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
                return db.Select<TUserAuthDetails>(q => q.UserAuthId == id).OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
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
                var oAuthProvider = db.Select<TUserAuthDetails>(
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

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            TUserAuth userAuth = (TUserAuth)GetUserAuth(authSession, tokens)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            using (var db = dbFactory.Open())
            {
                var authDetails = db.Select<TUserAuthDetails>(
                    q => q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (authDetails == null)
                {
                    authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
                    authDetails.Provider = tokens.Provider;
                    authDetails.UserId = tokens.UserId;
                }

                authDetails.PopulateMissing(tokens, overwriteReserved: true);
                userAuth.PopulateMissingExtended(authDetails);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                {
                    userAuth.CreatedDate = userAuth.ModifiedDate;
                }

                db.Save(userAuth);

                authDetails.UserAuthId = userAuth.Id;

                if (authDetails.CreatedDate == default(DateTime))
                {
                    authDetails.CreatedDate = userAuth.ModifiedDate;
                }
                authDetails.ModifiedDate = userAuth.ModifiedDate;

                db.Save(authDetails);

                return authDetails;
            }
        }

        public void Clear()
        {
            using (var db = dbFactory.Open())
            {
                db.DeleteAll<TUserAuth>();
                db.DeleteAll<TUserAuthDetails>();
            }
        }

        public ICollection<string> GetRoles(string userAuthId)
        {
            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Roles;
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    return db.Select<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Role != null).ConvertAll(x => x.Role);
                }
            }
        }

        public ICollection<string> GetPermissions(string userAuthId)
        {
            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Permissions;
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    return db.Select<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Permission != null).ConvertAll(x => x.Permission);
                }
            }
        }

        public bool HasRole(string userAuthId, string role)
        {
            if (role == null)
                throw new ArgumentNullException("role");

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Roles != null && userAuth.Roles.Contains(role);
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    return db.Count<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Role == role) > 0;
                }
            }
        }

        public bool HasPermission(string userAuthId, string permission)
        {
            if (permission == null)
                throw new ArgumentNullException("permission");

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Permissions != null && userAuth.Permissions.Contains(permission);
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    return db.Count<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Permission == permission) > 0;
                }
            }
        }

        public void AssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var userAuth = GetUserAuth(userAuthId);
            if (!UseDistinctRoleTables)
            {
                if (!roles.IsEmpty())
                {
                    foreach (var missingRole in roles.Where(x => !userAuth.Roles.Contains(x)))
                    {
                        userAuth.Roles.Add(missingRole);
                    }
                }

                if (!permissions.IsEmpty())
                {
                    foreach (var missingPermission in permissions.Where(x => !userAuth.Permissions.Contains(x)))
                    {
                        userAuth.Permissions.Add(missingPermission);
                    }
                }

                SaveUserAuth(userAuth);
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    var now = DateTime.UtcNow;
                    var userRoles = db.Select<UserAuthRole>(q => q.UserAuthId == userAuth.Id);

                    if (!roles.IsEmpty())
                    {
                        var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToHashSet();
                        foreach (var role in roles)
                        {
                            if (!roleSet.Contains(role))
                            {
                                db.Insert(new UserAuthRole
                                {
                                    UserAuthId = userAuth.Id,
                                    Role = role,
                                    CreatedDate = now,
                                    ModifiedDate = now,
                                });
                            }
                        }
                    }

                    if (!permissions.IsEmpty())
                    {
                        var permissionSet = userRoles.Where(x => x.Permission != null).Select(x => x.Permission).ToHashSet();
                        foreach (var permission in permissions)
                        {
                            if (!permissionSet.Contains(permission))
                            {
                                db.Insert(new UserAuthRole
                                {
                                    UserAuthId = userAuth.Id,
                                    Permission = permission,
                                    CreatedDate = now,
                                    ModifiedDate = now,
                                });
                            }
                        }
                    }
                }
            }
        }

        public void UnAssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var userAuth = GetUserAuth(userAuthId);
            if (!UseDistinctRoleTables)
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    SaveUserAuth(userAuth);
                }
            }
            else
            {
                using (var db = dbFactory.Open())
                {
                    if (!roles.IsEmpty())
                    {
                        db.Delete<UserAuthRole>(q => q.UserAuthId == userAuth.Id && roles.Contains(q.Role));
                    }
                    if (!permissions.IsEmpty())
                    {
                        db.Delete<UserAuthRole>(q => q.UserAuthId == userAuth.Id && permissions.Contains(q.Permission));
                    }
                }
            }
        }
    }
}