#if NET472 || NETSTANDARD2_0
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
    public partial class OrmLiteAuthRepository<TUserAuth, TUserAuthDetails> : OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails>
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        protected async Task<IDbConnection> OpenDbConnectionAsync()
        {
            return this.NamedConnection != null
                ? await dbFactory.OpenDbConnectionAsync(NamedConnection)
                : await dbFactory.OpenDbConnectionAsync();
        }

        public override async Task ExecAsync(Func<IDbConnection,Task> fn)
        {
            using var db = await OpenDbConnectionAsync();
            await fn(db);
        }

        public override async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> fn)
        {
            using var db = await OpenDbConnectionAsync();
            return await fn(db);
        }
    }

    public partial class OrmLiteAuthRepositoryMultitenancy<TUserAuth, TUserAuthDetails> 
        : OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails>, IAsyncDisposable
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public override async Task ExecAsync(Func<IDbConnection,Task> fn)
        {
            if (db == null)
                throw new NotSupportedException("This operation can only be called within context of a Request");

            await fn(db);
        }

        public override async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> fn)
        {
            if (db == null)
                throw new NotSupportedException("This operation can only be called within context of a Request");

            return await fn(db);
        }

        public async Task EachDb(Func<IDbConnection,Task> fn)
        {
            if (dbFactory == null)
                throw new NotSupportedException("This operation can only be called on Startup");

            var ormLiteDbFactory = (OrmLiteConnectionFactory)dbFactory;

            foreach (var connStr in connectionStrings)
            {
                //Required by In Memory Sqlite
                var dbConn = connStr == ormLiteDbFactory.ConnectionString
                    ? await dbFactory.OpenDbConnectionAsync()
                    : await dbFactory.OpenDbConnectionStringAsync(connStr);

                using (dbConn)
                {
                    await fn(dbConn);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (db is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                Dispose();
        }
    }

    public abstract partial class OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails> 
        : IUserAuthRepositoryAsync, IRequiresSchema, IClearableAsync, IManageRolesAsync, IManageApiKeysAsync, IQueryUserAuthAsync
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public abstract Task ExecAsync(Func<IDbConnection, Task> fn);

        public abstract Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> fn);

        public virtual async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token=default)
        {
            newUser.ValidateNewUser(password);

            return await ExecAsync(async db =>
            {
                await AssertNoExistingUserAsync(db, newUser, token: token);

                newUser.PopulatePasswordHashes(password);
                newUser.CreatedDate = DateTime.UtcNow;
                newUser.ModifiedDate = newUser.CreatedDate;

                await db.SaveAsync((TUserAuth)newUser, token: token);

                newUser = await db.SingleByIdAsync<TUserAuth>(newUser.Id, token);
                return newUser;
            });
        }

        protected virtual async Task AssertNoExistingUserAsync(IDbConnection db, IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token=default)
        {
            if (newUser.UserName != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(db, newUser.UserName, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(db, newUser.Email, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
            }
        }

        public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default)
        {
            newUser.ValidateNewUser(password);

            return await ExecAsync(async db =>
            {
                await AssertNoExistingUserAsync(db, newUser, existingUser, token);

                newUser.Id = existingUser.Id;
                newUser.PopulatePasswordHashes(password, existingUser);
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                await db.SaveAsync((TUserAuth)newUser, token: token);

                return newUser;
            });
        }

        public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default)
        {
            newUser.ValidateNewUser();

            return await ExecAsync(async db =>
            {
                await AssertNoExistingUserAsync(db, newUser, existingUser, token);

                newUser.Id = existingUser.Id;
                newUser.PasswordHash = existingUser.PasswordHash;
                newUser.Salt = existingUser.Salt;
                newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                await db.SaveAsync((TUserAuth)newUser, token: token);

                return newUser;
            });
        }

        public virtual async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default)
        {
            if (userNameOrEmail == null)
                return null;

            return await ExecAsync(async db =>
            {
                if (!hasInitSchema)
                {
                    hasInitSchema = await db.TableExistsAsync<TUserAuth>(token); 

                    if (!hasInitSchema)
                        throw new Exception("OrmLiteAuthRepository Db tables have not been initialized. Try calling 'InitSchema()' in your AppHost Configure method.");
                }
                return await GetUserAuthByUserNameAsync(db, userNameOrEmail, token);
            });
        }

        private async Task<TUserAuth> GetUserAuthByUserNameAsync(IDbConnection db, string userNameOrEmail, CancellationToken token=default)
        {
            var isEmail = userNameOrEmail.Contains("@");
            var lowerUserName = userNameOrEmail.ToLower();
            
            TUserAuth userAuth = null;

            // Usernames/Emails are saved in Lower Case so we can do an exact search using lowerUserName
            if (HostContext.GetPlugin<AuthFeature>()?.SaveUserNamesInLowerCase == true)
            {
                return isEmail
                    ? (await db.SelectAsync<TUserAuth>(q => q.Email == lowerUserName, token)).FirstOrDefault()
                    : (await db.SelectAsync<TUserAuth>(q => q.UserName == lowerUserName, token)).FirstOrDefault();
            }
            
            // Try an exact search using index first
            userAuth = isEmail
                ? (await db.SelectAsync<TUserAuth>(q => q.Email == userNameOrEmail, token)).FirstOrDefault()
                : (await db.SelectAsync<TUserAuth>(q => q.UserName == userNameOrEmail, token)).FirstOrDefault();

            if (userAuth != null)
                return userAuth;

            // Fallback to a non-index search if no exact match is found
            if (ForceCaseInsensitiveUserNameSearch)
            {
                userAuth = isEmail
                    ? (await db.SelectAsync<TUserAuth>(q => q.Email.ToLower() == lowerUserName, token)).FirstOrDefault()
                    : (await db.SelectAsync<TUserAuth>(q => q.UserName.ToLower() == lowerUserName, token)).FirstOrDefault();
            }
            
            return userAuth;
        }

        public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            return await ExecAsync(async db => {
                var q = db.From<TUserAuth>();
                if (orderBy != null)
                    q.OrderBy(orderBy);
                if (skip != null || take != null)
                    q.Limit(skip, take);
                return (await db.SelectAsync(q, token)).ConvertAll(x => (IUserAuth)x);
            });
        }

        public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            return await ExecAsync(async db => {
                var q = db.From<TUserAuth>();
                if (!string.IsNullOrEmpty(query))
                {
                    q.Where(x => x.UserName.Contains(query) ||
                                 x.PrimaryEmail.Contains(query) ||
                                 x.Email.Contains(query) ||
                                 x.DisplayName.Contains(query) ||
                                 x.Company.Contains(query));
                }
                if (orderBy != null)
                    q.OrderBy(orderBy);
                if (skip != null || take != null)
                    q.Limit(skip, take);
                return (await db.SelectAsync(q, token)).ConvertAll(x => (IUserAuth)x);
            });
        }
        
        public virtual async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token=default)
        {
            var userAuth = await GetUserAuthByUserNameAsync(userName, token);
            if (userAuth == null)
                return null;

            if (userAuth.VerifyPassword(password, out var needsRehash))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, needsRehash, password, token: token);
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token: token);

            return null;
        }

        public virtual async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, CancellationToken token=default)
        {
            var userAuth = await GetUserAuthByUserNameAsync(digestHeaders["username"], token);
            if (userAuth == null)
                return null;

            if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, token);
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token: token);
            return null;
        }

        public virtual async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token=default)
        {
            await ExecAsync(async db => {
                
                using var trans = db.OpenTransaction();
                var userId = int.Parse(userAuthId);

                await db.DeleteAsync<TUserAuth>(x => x.Id == userId, token: token);
                await db.DeleteAsync<TUserAuthDetails>(x => x.UserAuthId == userId, token: token);
                await db.DeleteAsync<UserAuthRole>(x => x.UserAuthId == userId, token: token);

                trans.Commit();
            });
        }

        public virtual async Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token=default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var userAuth = await GetUserAuthAsync(session, tokens, token);
            await LoadUserAuthAsync(session, userAuth, token);
        }

        private async Task LoadUserAuthAsync(IAuthSession session, IUserAuth userAuth, CancellationToken token=default)
        {
            await session.PopulateSessionAsync(userAuth, this, token: token);
        }

        public virtual async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token=default)
        {
            if (string.IsNullOrEmpty(userAuthId))
                throw new ArgumentNullException(nameof(userAuthId));
            
            return await ExecAsync(async db => await db.SingleByIdAsync<TUserAuth>(int.Parse(userAuthId), token));
        }

        public virtual async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token=default)
        {
            if (authSession == null)
                throw new ArgumentNullException(nameof(authSession));

            await ExecAsync(async db =>
            {
                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? await db.SingleByIdAsync<TUserAuth>(int.Parse(authSession.UserAuthId), token)
                    : authSession.ConvertTo<TUserAuth>();

                if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                    userAuth.Id = int.Parse(authSession.UserAuthId);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default)
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                await db.SaveAsync(userAuth, token: token);
            });
        }

        public virtual async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token=default)
        {
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await ExecAsync(async db =>
            {
                await db.SaveAsync((TUserAuth) userAuth, token: token);
            });
        }

        public virtual async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token=default)
        {
            var id = int.Parse(userAuthId);
            return await ExecAsync(async db =>
            {
                return (await db.SelectAsync<TUserAuthDetails>(q => q.UserAuthId == id, token))
                    .OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
            });
        }

        public virtual async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthAsync(authSession.UserAuthId, token);
                if (userAuth != null)
                    return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthByUserNameAsync(authSession.UserAuthName, token);
                if (userAuth != null)
                    return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            return await ExecAsync(async db =>
            {
                var oAuthProvider = (await db.SelectAsync<TUserAuthDetails>(q =>
                    q.Provider == tokens.Provider && q.UserId == tokens.UserId, token)).FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = await db.SingleByIdAsync<TUserAuth>(oAuthProvider.UserAuthId, token);
                    return userAuth;
                }
                return null;
            });
        }

        public virtual async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
        {
            TUserAuth userAuth = (TUserAuth)await GetUserAuthAsync(authSession, tokens, token)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            return await ExecAsync(async db =>
            {
                var authDetails = (await db.SelectAsync<TUserAuthDetails>(
                    q => q.Provider == tokens.Provider && q.UserId == tokens.UserId, token)).FirstOrDefault();

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

                await db.SaveAsync(userAuth, token: token);

                authDetails.UserAuthId = userAuth.Id;

                authDetails.ModifiedDate = userAuth.ModifiedDate;
                if (authDetails.CreatedDate == default(DateTime))
                    authDetails.CreatedDate = userAuth.ModifiedDate;

                await db.SaveAsync(authDetails, token: token);

                return authDetails;
            });
        }

        public virtual async Task ClearAsync(CancellationToken token=default)
        {
            await ExecAsync(async db =>
            {
                await db.DeleteAllAsync<TUserAuth>(token);
                await db.DeleteAllAsync<TUserAuthDetails>(token);
            });
        }

        public virtual async Task<ICollection<string>> GetRolesAsync(string userAuthId, CancellationToken token=default)
        {
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = await GetUserAuthAsync(userAuthId, token);
                if (userAuth == null)
                    return TypeConstants.EmptyStringArray;

                return userAuth.Roles;
            }
            else
            {
                return await ExecAsync(async db =>
                {
                    return (await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Role != null, token))
                        .ConvertAll(x => x.Role);
                });
            }
        }

        public virtual async Task<ICollection<string>> GetPermissionsAsync(string userAuthId, CancellationToken token=default)
        {
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = await GetUserAuthAsync(userAuthId, token);
                if (userAuth == null)
                    return TypeConstants.EmptyStringArray;

                return userAuth.Permissions;
            }
            else
            {
                return await ExecAsync(async db =>
                {
                    return (await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Permission != null, token))
                        .ConvertAll(x => x.Permission);
                });
            }
        }

        public virtual async Task<Tuple<ICollection<string>, ICollection<string>>> GetRolesAndPermissionsAsync(string userAuthId, CancellationToken token=default)
        {
            ICollection<string> roles;
            ICollection<string> permissions;
            
            AssertUserAuthId(userAuthId);
            if (!UseDistinctRoleTables)
            {
                var userAuth = await GetUserAuthAsync(userAuthId, token);
                if (userAuth != null)
                {
                    roles = userAuth.Roles;
                    permissions = userAuth.Permissions;
                }
                else
                {
                    roles = permissions = TypeConstants.EmptyStringArray;
                }
            }
            else
            {
                roles = new List<string>();
                permissions = new List<string>();
                
                var rolesAndPerms = await ExecAsync(async db =>
                {
                    return await db.KeyValuePairsAsync<string,string>(db.From<UserAuthRole>()
                        .Where(x => x.UserAuthId == int.Parse(userAuthId))
                        .Select(x => new { x.Role, x.Permission }), token); 
                });

                foreach (var kvp in rolesAndPerms)
                {
                    if (kvp.Key != null)
                        roles.Add(kvp.Key);
                    if (kvp.Value != null)
                        permissions.Add(kvp.Value);
                }                
            }
            return Tuple.Create(roles, permissions);
        }

        public virtual async Task<bool> HasRoleAsync(string userAuthId, string role, CancellationToken token=default)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = await GetUserAuthAsync(userAuthId, token);
                return userAuth.Roles != null && userAuth.Roles.Contains(role);
            }
            else
            {
                return await ExecAsync(async db =>
                {
                    return await db.CountAsync<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Role == role, token) > 0;
                });
            }
        }

        public virtual async Task<bool> HasPermissionAsync(string userAuthId, string permission, CancellationToken token=default)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            if (userAuthId == null)
                return false;

            if (!UseDistinctRoleTables)
            {
                var userAuth = await GetUserAuthAsync(userAuthId, token);
                return userAuth.Permissions != null && userAuth.Permissions.Contains(permission);
            }
            else
            {
                return await ExecAsync(async db =>
                {
                    return await db.CountAsync<UserAuthRole>(q =>
                        q.UserAuthId == int.Parse(userAuthId) && q.Permission == permission, token) > 0;
                });
            }
        }

        public virtual async Task AssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            var userAuth = await GetUserAuthAsync(userAuthId, token);
            if (!UseDistinctRoleTables)
            {
                if (!roles.IsEmpty())
                {
                    foreach (var missingRole in roles.Where(x => userAuth.Roles == null || !userAuth.Roles.Contains(x)))
                    {
                        if (userAuth.Roles == null)
                            userAuth.Roles = new List<string>();

                        userAuth.Roles.Add(missingRole);
                    }
                }

                if (!permissions.IsEmpty())
                {
                    foreach (var missingPermission in permissions.Where(x => userAuth.Permissions == null || !userAuth.Permissions.Contains(x)))
                    {
                        if (userAuth.Permissions == null)
                            userAuth.Permissions = new List<string>();

                        userAuth.Permissions.Add(missingPermission);
                    }
                }

                await SaveUserAuthAsync(userAuth, token);
            }
            else
            {
                await ExecAsync(async db =>
                {
                    var now = DateTime.UtcNow;
                    var userRoles = await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id, token);

                    if (!roles.IsEmpty())
                    {
                        var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToHashSet();
                        foreach (var role in roles)
                        {
                            if (!roleSet.Contains(role))
                            {
                                await db.InsertAsync(new UserAuthRole
                                {
                                    UserAuthId = userAuth.Id,
                                    Role = role,
                                    CreatedDate = now,
                                    ModifiedDate = now,
                                }, token: token);
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
                                await db.InsertAsync(new UserAuthRole
                                {
                                    UserAuthId = userAuth.Id,
                                    Permission = permission,
                                    CreatedDate = now,
                                    ModifiedDate = now,
                                }, token: token);
                            }
                        }
                    }
                });
            }
        }

        public virtual async Task UnAssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            var userAuth = await GetUserAuthAsync(userAuthId, token);
            if (!UseDistinctRoleTables)
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    await SaveUserAuthAsync(userAuth, token);
                }
            }
            else
            {
                await ExecAsync(async db =>
                {
                    if (!roles.IsEmpty())
                    {
                        await db.DeleteAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id && roles.Contains(q.Role), token: token);
                    }
                    if (!permissions.IsEmpty())
                    {
                        await db.DeleteAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id && permissions.Contains(q.Permission), token: token);
                    }
                });
            }
        }

        public async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token=default)
        {
            return await ExecAsync(async db =>
            {
                return await db.ExistsAsync<ApiKey>(x => x.Id == apiKey, token: token);
            });
        }

        public async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token=default)
        {
            return await ExecAsync(async db => await db.SingleByIdAsync<ApiKey>(apiKey, token));
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token=default)
        {
            return await ExecAsync(async db =>
            {
                var q = db.From<ApiKey>()
                    .Where(x => x.UserAuthId == userId)
                    .And(x => x.CancelledDate == null)
                    .And(x => x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow)
                    .OrderByDescending(x => x.CreatedDate);

                return await db.SelectAsync(q, token);
            });
        }

        public async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token=default)
        {
            await ExecAsync(async db =>
            {
                await db.SaveAllAsync(apiKeys, token);
            });
        }
    }
}
#endif