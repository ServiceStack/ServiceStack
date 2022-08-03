using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Auth
{
    public class OrmLiteAuthRepository : OrmLiteAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory) { }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, string namedConnection = null) 
            : base(dbFactory, namedConnection) {}
    }

    public partial class OrmLiteAuthRepository<TUserAuth, TUserAuthDetails> : OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails>
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        private readonly IDbConnectionFactory dbFactory;

        public string NamedConnection { get; private set; }

        public OrmLiteAuthRepository(IDbConnectionFactory dbFactory, string namedConnection = null)
        {
            this.dbFactory = dbFactory;
            this.NamedConnection = namedConnection;
        }

        protected IDbConnection OpenDbConnection()
        {
            return this.NamedConnection != null
                ? dbFactory.OpenDbConnection(NamedConnection)
                : dbFactory.OpenDbConnection();
        }

        public override void Exec(Action<IDbConnection> fn)
        {
            using var db = OpenDbConnection();
            fn(db);
        }

        public override T Exec<T>(Func<IDbConnection, T> fn)
        {
            using var db = OpenDbConnection();
            return fn(db);
        }
    }

    public class OrmLiteAuthRepositoryMultitenancy : OrmLiteAuthRepositoryMultitenancy<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public OrmLiteAuthRepositoryMultitenancy(IDbConnection db) : base(db) { }

        public OrmLiteAuthRepositoryMultitenancy(IDbConnectionFactory dbFactory, params string[] connectionStrings)
            : base(dbFactory, connectionStrings) { }
    }

    public partial class OrmLiteAuthRepositoryMultitenancy<TUserAuth, TUserAuthDetails> : OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails>, IDisposable
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        private readonly IDbConnection db;

        public OrmLiteAuthRepositoryMultitenancy(IDbConnection db)
        {
            this.db = db;
        }

        private readonly IDbConnectionFactory dbFactory;
        private readonly string[] connectionStrings;

        public OrmLiteAuthRepositoryMultitenancy(IDbConnectionFactory dbFactory, params string[] connectionStrings)
        {
            this.dbFactory = dbFactory;
            this.connectionStrings = connectionStrings;
        }

        public override void Exec(Action<IDbConnection> fn)
        {
            if (db == null)
                throw new NotSupportedException("This operation can only be called within context of a Request");

            fn(db);
        }

        public override T Exec<T>(Func<IDbConnection, T> fn)
        {
            if (db == null)
                throw new NotSupportedException("This operation can only be called within context of a Request");

            return fn(db);
        }

        public void EachDb(Action<IDbConnection> fn)
        {
            if (dbFactory == null)
                throw new NotSupportedException("This operation can only be called on Startup");

            var ormLiteDbFactory = (OrmLiteConnectionFactory)dbFactory;

            foreach (var connStr in connectionStrings)
            {
                //Required by In Memory Sqlite
                var db = connStr == ormLiteDbFactory.ConnectionString
                    ? dbFactory.OpenDbConnection()
                    : dbFactory.OpenDbConnectionString(connStr);

                using (db)
                {
                    fn(db);
                }
            }
        }

        public override void InitSchema()
        {
            hasInitSchema = true;

            EachDb(db =>
            {
                db.CreateTableIfNotExists<TUserAuth>();
                db.CreateTableIfNotExists<TUserAuthDetails>();
                if (UseDistinctRoleTables)
                    db.CreateTableIfNotExists<UserAuthRole>();
            });
        }

        public override void DropAndReCreateTables()
        {
            hasInitSchema = true;

            EachDb(db =>
            {
                db.DropAndCreateTable<TUserAuth>();
                db.DropAndCreateTable<TUserAuthDetails>();
                if (UseDistinctRoleTables)
                    db.DropAndCreateTable<UserAuthRole>();
            });
        }

        public override void InitApiKeySchema()
        {
            EachDb(db =>
            {
                db.CreateTableIfNotExists<ApiKey>();
            });
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }

    public abstract partial class OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails> 
        : IUserAuthRepository, IRequiresSchema, IClearable, IManageRoles, IManageApiKeys, ICustomUserAuth, IQueryUserAuth
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public bool hasInitSchema;

        public bool UseDistinctRoleTables { get; set; }

        public bool ForceCaseInsensitiveUserNameSearch { get; set; } = true;

        public abstract void Exec(Action<IDbConnection> fn);

        public abstract T Exec<T>(Func<IDbConnection, T> fn);

        public virtual void InitSchema()
        {
            hasInitSchema = true;
            Exec(db =>
            {
                db.CreateTableIfNotExists<TUserAuth>();
                db.CreateTableIfNotExists<TUserAuthDetails>();
                if (UseDistinctRoleTables)
                    db.CreateTableIfNotExists<UserAuthRole>();
            });
        }

        public virtual void DropAndReCreateTables()
        {
            hasInitSchema = true;
            Exec(db =>
            {
                db.DropAndCreateTable<TUserAuth>();
                db.DropAndCreateTable<TUserAuthDetails>();
                if (UseDistinctRoleTables)
                    db.DropAndCreateTable<UserAuthRole>();
            });
        }

        public virtual IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            return Exec(db =>
            {
                AssertNoExistingUser(db, newUser);

                newUser.PopulatePasswordHashes(password);
                newUser.CreatedDate = DateTime.UtcNow;
                newUser.ModifiedDate = newUser.CreatedDate;

                db.Save((TUserAuth)newUser);

                newUser = db.SingleById<TUserAuth>(newUser.Id);
                return newUser;
            });
        }

        protected virtual void AssertNoExistingUser(IDbConnection db, IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
            }
        }

        public virtual IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            return Exec(db =>
            {
                AssertNoExistingUser(db, newUser, existingUser);

                newUser.Id = existingUser.Id;
                newUser.PopulatePasswordHashes(password, existingUser);
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                db.Save((TUserAuth)newUser);

                return newUser;
            });
        }

        public virtual IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            newUser.ValidateNewUser();

            return Exec(db =>
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
            });
        }

        public virtual IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            return Exec(db =>
            {
                if (!hasInitSchema)
                {
                    hasInitSchema = db.TableExists<TUserAuth>();

                    if (!hasInitSchema)
                        throw new Exception("OrmLiteAuthRepository Db tables have not been initialized. Try calling 'InitSchema()' in your AppHost Configure method.");
                }
                return GetUserAuthByUserName(db, userNameOrEmail);
            });
        }

        private TUserAuth GetUserAuthByUserName(IDbConnection db, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var lowerUserName = userNameOrEmail.ToLower();
            
            TUserAuth userAuth = null;

            // Usernames/Emails are saved in Lower Case so we can do an exact search using lowerUserName
            if (HostContext.GetPlugin<AuthFeature>()?.SaveUserNamesInLowerCase == true)
            {
                return isEmail
                    ? db.Select<TUserAuth>(q => q.Email == lowerUserName).FirstOrDefault()
                    : db.Select<TUserAuth>(q => q.UserName == lowerUserName).FirstOrDefault();
            }
            
            // Try an exact search using index first
            userAuth = isEmail
                ? db.Select<TUserAuth>(q => q.Email == userNameOrEmail).FirstOrDefault()
                : db.Select<TUserAuth>(q => q.UserName == userNameOrEmail).FirstOrDefault();

            if (userAuth != null)
                return userAuth;

            // Fallback to a non-index search if no exact match is found
            if (ForceCaseInsensitiveUserNameSearch)
            {
                userAuth = isEmail
                    ? db.Select<TUserAuth>(q => q.Email.ToLower() == lowerUserName).FirstOrDefault()
                    : db.Select<TUserAuth>(q => q.UserName.ToLower() == lowerUserName).FirstOrDefault();
            }
            
            return userAuth;
        }

        private void SetOrderBy(SqlExpression<TUserAuth> q, string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return;
            
            var orderByField = AuthRepositoryUtils.ParseOrderBy(orderBy, out var desc);
            if (!desc)
                q.OrderBy(orderByField);
            else
                q.OrderByDescending(orderByField);
        }

        public List<IUserAuth> GetUserAuths(string orderBy = null, int? skip = null, int? take = null)
        {
            return Exec(db => {
                var q = db.From<TUserAuth>();
                SetOrderBy(q, orderBy);
                if (skip != null || take != null)
                    q.Limit(skip, take);
                return db.Select(q).ConvertAll(x => (IUserAuth)x);
            });
        }

        public List<IUserAuth> SearchUserAuths(string query, string orderBy = null, int? skip = null, int? take = null)
        {
            return Exec(db => {
                var q = db.From<TUserAuth>();
                if (!string.IsNullOrEmpty(query))
                {
                    q.Where(x => x.UserName.Contains(query) ||
                                 x.PrimaryEmail.Contains(query) ||
                                 x.Email.Contains(query) ||
                                 x.DisplayName.Contains(query) ||
                                 x.Company.Contains(query));
                }
                SetOrderBy(q, orderBy);
                if (skip != null || take != null)
                    q.Limit(skip, take);
                return db.Select(q).ConvertAll(x => (IUserAuth)x);
            });
        }
        
        public virtual bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null)
                return false;

            if (userAuth.VerifyPassword(password, out var needsRehash))
            {
                this.RecordSuccessfulLogin(userAuth, needsRehash, password);
                return true;
            }

            this.RecordInvalidLoginAttempt(userAuth);

            userAuth = null;
            return false;
        }

        public virtual bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null)
                return false;

            if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
            {
                this.RecordSuccessfulLogin(userAuth);
                return true;
            }

            this.RecordInvalidLoginAttempt(userAuth);

            userAuth = null;
            return false;
        }

        public virtual void DeleteUserAuth(string userAuthId)
        {
            Exec(db => {
                using var trans = db.OpenTransaction();
                var userId = int.Parse(userAuthId);

                db.Delete<TUserAuth>(x => x.Id == userId);
                db.Delete<TUserAuthDetails>(x => x.UserAuthId == userId);
                if (UseDistinctRoleTables)
                    db.Delete<UserAuthRole>(x => x.UserAuthId == userId);

                trans.Commit();
            });
        }

        public virtual void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, IUserAuth userAuth)
        {
            session.PopulateSession(userAuth, this);
        }

        public virtual IUserAuth GetUserAuth(string userAuthId)
        {
            if (string.IsNullOrEmpty(userAuthId))
                throw new ArgumentNullException(nameof(userAuthId));
            
            return Exec(db => db.SingleById<TUserAuth>(int.Parse(userAuthId)));
        }

        public virtual void SaveUserAuth(IAuthSession authSession)
        {
            if (authSession == null)
                throw new ArgumentNullException(nameof(authSession));

            Exec(db =>
            {
                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? db.SingleById<TUserAuth>(int.Parse(authSession.UserAuthId))
                    : authSession.ConvertTo<TUserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                    userAuth.Id = int.Parse(authSession.UserAuthId);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                db.Save(userAuth);
            });
        }

        public virtual void SaveUserAuth(IUserAuth userAuth)
        {
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Exec(db =>
            {
                db.Save((TUserAuth) userAuth);
            });
        }

        public virtual List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            var id = int.Parse(userAuthId);
            return Exec(db =>
            {
                return db.Select<TUserAuthDetails>(q => q.UserAuthId == id).OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
            });
        }

        public virtual IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null)
                    return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null)
                    return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            return Exec(db =>
            {
                var oAuthProvider = db.Select<TUserAuthDetails>(q =>
                    q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = db.SingleById<TUserAuth>(oAuthProvider.UserAuthId);
                    return userAuth;
                }
                return null;
            });
        }

        public virtual IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            TUserAuth userAuth = (TUserAuth)GetUserAuth(authSession, tokens)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            return Exec(db =>
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
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                db.Save(userAuth);

                authDetails.UserAuthId = userAuth.Id;

                authDetails.ModifiedDate = userAuth.ModifiedDate;
                if (authDetails.CreatedDate == default(DateTime))
                    authDetails.CreatedDate = userAuth.ModifiedDate;

                db.Save(authDetails);

                return authDetails;
            });
        }

        public virtual void Clear()
        {
            Exec(db =>
            {
                db.DeleteAll<TUserAuth>();
                db.DeleteAll<TUserAuthDetails>();
                if (UseDistinctRoleTables)
                    db.DeleteAll<UserAuthRole>();
            });
        }

        string AssertUserAuthId(string userAuthId)
        {
            if (userAuthId == null)
                throw new ArgumentNullException(nameof(userAuthId));
            return userAuthId;
        }

        public virtual ICollection<string> GetRoles(string userAuthId)
        {
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth?.Roles ?? TypeConstants.EmptyStringList;;
            }
            else
            {
                return Exec(db =>
                {
                    return db.Select<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Role != null).ConvertAll(x => x.Role);
                });
            }
        }

        public virtual ICollection<string> GetPermissions(string userAuthId)
        {
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth?.Permissions ?? TypeConstants.EmptyStringList;;
            }
            else
            {
                return Exec(db =>
                {
                    return db.Select<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Permission != null).ConvertAll(x => x.Permission);
                });
            }
        }

        public virtual void GetRolesAndPermissions(string userAuthId, out ICollection<string> roles, out ICollection<string> permissions)
        {
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                if (userAuth == null)
                {
                    roles = permissions = TypeConstants.EmptyStringArray;
                    return;
                }

                roles = userAuth.Roles;
                permissions = userAuth.Permissions;
            }
            else
            {
                roles = new List<string>();
                permissions = new List<string>();
                
                var rolesAndPerms = Exec(db =>
                {
                    return db.KeyValuePairs<string,string>(db.From<UserAuthRole>()
                        .Where(x => x.UserAuthId == int.Parse(userAuthId))
                        .Select(x => new { x.Role, x.Permission })); 
                });

                foreach (var kvp in rolesAndPerms)
                {
                    if (kvp.Key != null)
                        roles.Add(kvp.Key);
                    if (kvp.Value != null)
                        permissions.Add(kvp.Value);
                }                
            }
        }

        public virtual bool HasRole(string userAuthId, string role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Roles != null && userAuth.Roles.Contains(role);
            }
            else
            {
                return Exec(db =>
                {
                    return db.Count<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Role == role) > 0;
                });
            }
        }

        public virtual bool HasPermission(string userAuthId, string permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = GetUserAuth(userAuthId);
                return userAuth.Permissions != null && userAuth.Permissions.Contains(permission);
            }
            else
            {
                return Exec(db =>
                {
                    return db.Count<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Permission == permission) > 0;
                });
            }
        }
        
        public virtual void AssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var userAuth = GetUserAuth(userAuthId);
            if (!UseDistinctRoleTables)
            {
                if (!roles.IsEmpty())
                {
                    foreach (var missingRole in roles.Where(x => userAuth.Roles == null || !userAuth.Roles.Contains(x)))
                    {
                        userAuth.Roles ??= new List<string>();
                        userAuth.Roles.Add(missingRole);
                    }
                }

                if (!permissions.IsEmpty())
                {
                    foreach (var missingPermission in permissions.Where(x => userAuth.Permissions == null || !userAuth.Permissions.Contains(x)))
                    {
                        userAuth.Permissions ??= new List<string>();
                        userAuth.Permissions.Add(missingPermission);
                    }
                }

                SaveUserAuth(userAuth);
            }
            else
            {
                Exec(db =>
                {
                    var now = DateTime.UtcNow;
                    var userRoles = db.Select<UserAuthRole>(q => q.UserAuthId == userAuth.Id);

                    if (!roles.IsEmpty())
                    {
                        var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToSet();
                        foreach (var role in roles)
                        {
                            if (!roleSet.Contains(role))
                            {
                                db.Insert(new UserAuthRole {
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
                        var permissionSet = userRoles.Where(x => x.Permission != null).Select(x => x.Permission).ToSet();
                        foreach (var permission in permissions)
                        {
                            if (!permissionSet.Contains(permission))
                            {
                                db.Insert(new UserAuthRole {
                                    UserAuthId = userAuth.Id,
                                    Permission = permission,
                                    CreatedDate = now,
                                    ModifiedDate = now,
                                });
                            }
                        }
                    }
                });
            }
        }

        public virtual void UnAssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
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
                Exec(db =>
                {
                    if (!roles.IsEmpty())
                    {
                        db.Delete<UserAuthRole>(q => q.UserAuthId == userAuth.Id && roles.Contains(q.Role));
                    }
                    if (!permissions.IsEmpty())
                    {
                        db.Delete<UserAuthRole>(q => q.UserAuthId == userAuth.Id && permissions.Contains(q.Permission));
                    }
                });
            }
        }

        public virtual void InitApiKeySchema()
        {
            Exec(db => 
            {
                db.CreateTableIfNotExists<ApiKey>();
            });
        }

        public bool ApiKeyExists(string apiKey)
        {
            return Exec(db =>
            {
                return db.Exists<ApiKey>(x => x.Id == apiKey);
            });
        }

        public ApiKey GetApiKey(string apiKey)
        {
            return Exec(db => db.SingleById<ApiKey>(apiKey));
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            return Exec(db =>
            {
                var q = db.From<ApiKey>()
                    .Where(x => x.UserAuthId == userId)
                    .And(x => x.CancelledDate == null)
                    .And(x => x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow)
                    .OrderByDescending(x => x.CreatedDate);

                return db.Select(q);
            });
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            Exec(db =>
            {
                db.SaveAll(apiKeys);
            });
        }

        public IUserAuth CreateUserAuth() => (IUserAuth) typeof(TUserAuth).CreateInstance();

        public IUserAuthDetails CreateUserAuthDetails() => (IUserAuthDetails)typeof(TUserAuthDetails).CreateInstance();
    }
}
