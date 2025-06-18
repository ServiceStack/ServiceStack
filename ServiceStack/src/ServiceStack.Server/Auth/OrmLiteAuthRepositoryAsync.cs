using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Text;
using ServiceStack.OrmLite;

namespace ServiceStack.Auth;

public partial class OrmLiteAuthRepository<TUserAuth, TUserAuthDetails> : OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails>
    where TUserAuth : class, IUserAuth
    where TUserAuthDetails : class, IUserAuthDetails
{
    protected async Task<IDbConnection> OpenDbConnectionAsync()
    {
        return this.NamedConnection != null
            ? await dbFactory.OpenDbConnectionAsync(NamedConnection,ConfigureDb).ConfigAwait()
            : await dbFactory.OpenDbConnectionAsync(ConfigureDb).ConfigAwait();
    }

    public override async Task ExecAsync(Func<IDbConnection,Task> fn)
    {
        using var db = await OpenDbConnectionAsync().ConfigAwait();
        await fn(db).ConfigAwait();
    }

    public override async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> fn)
    {
        using var db = await OpenDbConnectionAsync().ConfigAwait();
        return await fn(db).ConfigAwait();
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

        await fn(db).ConfigAwait();
    }

    public override async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> fn)
    {
        if (db == null)
            throw new NotSupportedException("This operation can only be called within context of a Request");

        return await fn(db).ConfigAwait();
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
                ? await dbFactory.OpenDbConnectionAsync(ConfigureDb).ConfigAwait()
                : await dbFactory.OpenDbConnectionStringAsync(connStr,ConfigureDb).ConfigAwait();

            using (dbConn)
            {
                await fn(dbConn).ConfigAwait();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (db is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            Dispose();
    }
}

public abstract partial class OrmLiteAuthRepositoryBase<TUserAuth, TUserAuthDetails> 
    : IUserAuthRepositoryAsync, IRequiresSchema, IClearableAsync, IManageRolesAsync, IManageApiKeysAsync, IQueryUserAuthAsync, IManageSourceRolesAsync
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
            await AssertNoExistingUserAsync(db, newUser, token: token).ConfigAwait();

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            await db.SaveAsync((TUserAuth)newUser, token: token).ConfigAwait();

            newUser = await db.SingleByIdAsync<TUserAuth>(newUser.Id, token).ConfigAwait();
            return newUser;
        }).ConfigAwait();
    }

    protected virtual async Task AssertNoExistingUserAsync(IDbConnection db, IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token=default)
    {
        if (newUser.UserName != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(db, newUser.UserName, token).ConfigAwait();
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
        }
        if (newUser.Email != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(db, newUser.Email, token).ConfigAwait();
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
        }
    }

    public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default)
    {
        newUser.ValidateNewUser(password);

        return await ExecAsync(async db =>
        {
            await AssertNoExistingUserAsync(db, newUser, existingUser, token).ConfigAwait();

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            await db.SaveAsync((TUserAuth)newUser, token: token).ConfigAwait();

            return newUser;
        }).ConfigAwait();
    }

    public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default)
    {
        newUser.ValidateNewUser();

        return await ExecAsync(async db =>
        {
            await AssertNoExistingUserAsync(db, newUser, existingUser, token).ConfigAwait();

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            await db.SaveAsync((TUserAuth)newUser, token: token).ConfigAwait();

            return newUser;
        }).ConfigAwait();
    }

    public virtual async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default)
    {
        if (userNameOrEmail == null)
            return null;

        return await ExecAsync(async db =>
        {
            if (!hasInitSchema)
            {
                hasInitSchema = await db.TableExistsAsync<TUserAuth>(token).ConfigAwait(); 

                if (!hasInitSchema)
                    throw new Exception("OrmLiteAuthRepository Db tables have not been initialized. Try calling 'InitSchema()' in your AppHost Configure method.");
            }
            return await GetUserAuthByUserNameAsync(db, userNameOrEmail, token).ConfigAwait();
        }).ConfigAwait();
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
                ? (await db.SelectAsync<TUserAuth>(q => q.Email == lowerUserName, token).ConfigAwait()).FirstOrDefault()
                : (await db.SelectAsync<TUserAuth>(q => q.UserName == lowerUserName, token).ConfigAwait()).FirstOrDefault();
        }
            
        // Try an exact search using index first
        userAuth = isEmail
            ? (await db.SelectAsync<TUserAuth>(q => q.Email == userNameOrEmail, token).ConfigAwait()).FirstOrDefault()
            : (await db.SelectAsync<TUserAuth>(q => q.UserName == userNameOrEmail, token).ConfigAwait()).FirstOrDefault();

        if (userAuth != null)
            return userAuth;

        // Fallback to a non-index search if no exact match is found
        if (ForceCaseInsensitiveUserNameSearch)
        {
            userAuth = isEmail
                ? (await db.SelectAsync<TUserAuth>(q => q.Email.ToLower() == lowerUserName, token).ConfigAwait()).FirstOrDefault()
                : (await db.SelectAsync<TUserAuth>(q => q.UserName.ToLower() == lowerUserName, token).ConfigAwait()).FirstOrDefault();
        }
            
        return userAuth;
    }

    public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
    {
        return await ExecAsync(async db => {
            var q = db.From<TUserAuth>();
            SetOrderBy(q, orderBy);
            if (skip != null || take != null)
                q.Limit(skip, take);
            return (await db.SelectAsync(q, token).ConfigAwait()).ConvertAll(x => (IUserAuth)x);
        }).ConfigAwait();
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
            SetOrderBy(q, orderBy);
            if (skip != null || take != null)
                q.Limit(skip, take);
            return (await db.SelectAsync(q, token).ConfigAwait()).ConvertAll(x => (IUserAuth)x);
        }).ConfigAwait();
    }
        
    public virtual async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token=default)
    {
        var userAuth = await GetUserAuthByUserNameAsync(userName, token).ConfigAwait();
        if (userAuth == null)
            return null;

        if (userAuth.VerifyPassword(password, out var needsRehash))
        {
            await this.RecordSuccessfulLoginAsync(userAuth, needsRehash, password, token).ConfigAwait();
            return userAuth;
        }

        await this.RecordInvalidLoginAttemptAsync(userAuth, token).ConfigAwait();

        return null;
    }

    public virtual async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, CancellationToken token=default)
    {
        var userAuth = await GetUserAuthByUserNameAsync(digestHeaders["username"], token).ConfigAwait();
        if (userAuth == null)
            return null;

        if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
        {
            await this.RecordSuccessfulLoginAsync(userAuth, token).ConfigAwait();
            return userAuth;
        }

        await this.RecordInvalidLoginAttemptAsync(userAuth, token).ConfigAwait();
        return null;
    }

    public virtual async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token=default)
    {
        await ExecAsync(async db => {
                
            using var trans = db.OpenTransaction();
            var userId = int.Parse(userAuthId);

            await db.DeleteAsync<TUserAuth>(x => x.Id == userId, token: token).ConfigAwait();
            await db.DeleteAsync<TUserAuthDetails>(x => x.UserAuthId == userId, token: token).ConfigAwait();
            if (UseDistinctRoleTables)
                await db.DeleteAsync<UserAuthRole>(x => x.UserAuthId == userId, token: token).ConfigAwait();

            trans.Commit();
        }).ConfigAwait();
    }

    public virtual async Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token=default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var userAuth = await GetUserAuthAsync(session, tokens, token).ConfigAwait();
        await LoadUserAuthAsync(session, userAuth, token).ConfigAwait();
    }

    private async Task LoadUserAuthAsync(IAuthSession session, IUserAuth userAuth, CancellationToken token=default)
    {
        await session.PopulateSessionAsync(userAuth, this, token).ConfigAwait();
    }

    public virtual async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token=default)
    {
        if (string.IsNullOrEmpty(userAuthId))
            throw new ArgumentNullException(nameof(userAuthId));
            
        return await ExecAsync(async db => 
            await db.SingleByIdAsync<TUserAuth>(int.Parse(userAuthId), token).ConfigAwait()).ConfigAwait();
    }

    public virtual async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token=default)
    {
        if (authSession == null)
            throw new ArgumentNullException(nameof(authSession));

        await ExecAsync(async db =>
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? await db.SingleByIdAsync<TUserAuth>(int.Parse(authSession.UserAuthId), token).ConfigAwait()
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await db.SaveAsync(userAuth, token: token).ConfigAwait();
        }).ConfigAwait();
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
            await db.SaveAsync((TUserAuth) userAuth, token: token).ConfigAwait();
        }).ConfigAwait();
    }

    public virtual async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token=default)
    {
        var id = int.Parse(userAuthId);
        return await ExecAsync(async db =>
        {
            return (await db.SelectAsync<TUserAuthDetails>(q => q.UserAuthId == id, token).ConfigAwait())
                .OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
        }).ConfigAwait();
    }

    public virtual async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
    {
        if (!authSession.UserAuthId.IsNullOrEmpty())
        {
            var userAuth = await GetUserAuthAsync(authSession.UserAuthId, token).ConfigAwait();
            if (userAuth != null)
                return userAuth;
        }
        if (!authSession.UserAuthName.IsNullOrEmpty())
        {
            var userAuth = await GetUserAuthByUserNameAsync(authSession.UserAuthName, token).ConfigAwait();
            if (userAuth != null)
                return userAuth;
        }

        if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
            return null;

        return await ExecAsync(async db =>
        {
            var oAuthProvider = (await db.SelectAsync<TUserAuthDetails>(q =>
                q.Provider == tokens.Provider && q.UserId == tokens.UserId, token).ConfigAwait()).FirstOrDefault();

            if (oAuthProvider != null)
            {
                var userAuth = await db.SingleByIdAsync<TUserAuth>(oAuthProvider.UserAuthId, token).ConfigAwait();
                return userAuth;
            }
            return null;
        });
    }

    public virtual async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
    {
        TUserAuth userAuth = (TUserAuth)await GetUserAuthAsync(authSession, tokens, token).ConfigAwait()
                             ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

        return await ExecAsync(async db =>
        {
            var authDetails = (await db.SelectAsync<TUserAuthDetails>(
                q => q.Provider == tokens.Provider && q.UserId == tokens.UserId, token).ConfigAwait()).FirstOrDefault();

            if (authDetails == null)
            {
                authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
                authDetails.Provider = tokens.Provider;
                authDetails.UserId = tokens.UserId;
            }

            authDetails.PopulateMissing(tokens, overwriteReserved: true);
            userAuth.PopulateMissingExtended(authDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await db.SaveAsync(userAuth, token: token).ConfigAwait();

            authDetails.UserAuthId = userAuth.Id;

            authDetails.ModifiedDate = userAuth.ModifiedDate;
            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;

            await db.SaveAsync(authDetails, token: token).ConfigAwait();

            return authDetails;
        }).ConfigAwait();
    }

    public virtual async Task ClearAsync(CancellationToken token=default)
    {
        await ExecAsync(async db =>
        {
            await db.DeleteAllAsync<TUserAuth>(token).ConfigAwait();
            await db.DeleteAllAsync<TUserAuthDetails>(token).ConfigAwait();
            if (UseDistinctRoleTables)
                await db.DeleteAllAsync<UserAuthRole>(token).ConfigAwait();
        }).ConfigAwait();
    }

    public virtual async Task<ICollection<string>> GetRolesAsync(string userAuthId, CancellationToken token=default)
    {
        AssertUserAuthId(userAuthId);
        if (!UseDistinctRoleTables)
        {
            var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
            return userAuth?.Roles ?? TypeConstants.EmptyStringList;
        }
        else
        {
            return await ExecAsync(async db =>
            {
                return (await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Role != null, token).ConfigAwait())
                    .ConvertAll(x => x.Role);
            }).ConfigAwait();
        }
    }

    public virtual async Task<ICollection<string>> GetPermissionsAsync(string userAuthId, CancellationToken token=default)
    {
        AssertUserAuthId(userAuthId);
        if (!UseDistinctRoleTables)
        {
            var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
            return userAuth?.Permissions ?? TypeConstants.EmptyStringList;
        }
        return await ExecAsync(async db => {
            return (await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == int.Parse(userAuthId) && q.Permission != null, token).ConfigAwait())
                .ConvertAll(x => x.Permission);
        }).ConfigAwait();
    }

    public async Task<Tuple<ICollection<string>, ICollection<string>>> GetLocalRolesAndPermissionsAsync(string userAuthId, CancellationToken token = default)
    {
        if (UseDistinctRoleTables)
        {
            ICollection<string> roles = new List<string>();
            ICollection<string> permissions = new List<string>();
            var rolesAndPerms = await ExecAsync(async db => {
                return await db.KeyValuePairsAsync<string,string>(db.From<UserAuthRole>()
                    .Where(x => x.UserAuthId == int.Parse(userAuthId) && x.RefIdStr == null)
                    .Select(x => new { x.Role, x.Permission }), token).ConfigAwait(); 
            }).ConfigAwait();
            foreach (var kvp in rolesAndPerms)
            {
                if (kvp.Key != null)
                    roles.Add(kvp.Key);
                if (kvp.Value != null)
                    permissions.Add(kvp.Value);
            }
            return Tuple.Create(roles, permissions);
        }
        return await GetRolesAndPermissionsAsync(userAuthId, token);
    }

    public virtual async Task<Tuple<ICollection<string>, ICollection<string>>> GetRolesAndPermissionsAsync(string userAuthId, CancellationToken token=default)
    {
        ICollection<string> roles;
        ICollection<string> permissions;
            
        AssertUserAuthId(userAuthId);
        if (!UseDistinctRoleTables)
        {
            var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
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
                    .Select(x => new { x.Role, x.Permission }), token).ConfigAwait(); 
            }).ConfigAwait();

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
            var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
            return userAuth.Roles != null && userAuth.Roles.Contains(role);
        }
        else
        {
            return await ExecAsync(async db =>
            {
                return await db.CountAsync<UserAuthRole>(q =>
                    q.UserAuthId == int.Parse(userAuthId) && q.Role == role, token).ConfigAwait() > 0;
            }).ConfigAwait();
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
            var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
            return userAuth.Permissions != null && userAuth.Permissions.Contains(permission);
        }
        else
        {
            return await ExecAsync(async db =>
            {
                return await db.CountAsync<UserAuthRole>(q =>
                    q.UserAuthId == int.Parse(userAuthId) && q.Permission == permission, token).ConfigAwait() > 0;
            }).ConfigAwait();
        }
    }

    public async Task MergeRolesAsync(string userAuthId, string source, ICollection<string> roles, CancellationToken token = default)
    {
        if (UseDistinctRoleTables)
        {
            var userId = userAuthId.ToInt();
            await ExecAsync(async db =>
            {
                var now = DateTime.UtcNow;
                var userAuthRoles = await db.SelectAsync<UserAuthRole>(x => x.UserAuthId == userId && x.Role != null && x.RefIdStr == source, token).ConfigAwait();
                var roleList = userAuthRoles.Map(x => x.Role);
                var rolesToAdd = roles.Where(x => !roleList.Contains(x)).ToList();
                var rolesToRemove = roleList.Where(x => !roles.Contains(x)).ToList();
                    
                if (rolesToAdd.Count > 0)
                {
                    foreach (var role in rolesToAdd)
                    {
                        await db.InsertAsync(new UserAuthRole
                        {
                            UserAuthId = userId,
                            Role = role,
                            CreatedDate = now,
                            ModifiedDate = now,
                            RefIdStr = source,
                        }, token: token).ConfigAwait();
                    }
                }
                if (rolesToRemove.Count > 0)
                {
                    await db.DeleteAsync<UserAuthRole>(x => 
                        x.UserAuthId == userId && x.RefIdStr == source && rolesToRemove.Contains(x.Role), token:token).ConfigAwait();
                }
                    
            }).ConfigAwait();
        }
        await AssignRolesAsync(userAuthId, roles:roles, token:token);
    }

    public virtual async Task AssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
    {
        var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
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

            await SaveUserAuthAsync(userAuth, token).ConfigAwait();
        }
        else
        {
            await ExecAsync(async db =>
            {
                var now = DateTime.UtcNow;
                var userRoles = await db.SelectAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id, token).ConfigAwait();

                if (!roles.IsEmpty())
                {
                    var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToSet();
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
                            }, token: token).ConfigAwait();
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
                            await db.InsertAsync(new UserAuthRole
                            {
                                UserAuthId = userAuth.Id,
                                Permission = permission,
                                CreatedDate = now,
                                ModifiedDate = now,
                            }, token: token).ConfigAwait();
                        }
                    }
                }
            }).ConfigAwait();
        }
    }

    public virtual async Task UnAssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
    {
        var userAuth = await GetUserAuthAsync(userAuthId, token).ConfigAwait();
        if (!UseDistinctRoleTables)
        {
            roles.Each(x => userAuth.Roles.Remove(x));
            permissions.Each(x => userAuth.Permissions.Remove(x));

            if (roles != null || permissions != null)
            {
                await SaveUserAuthAsync(userAuth, token).ConfigAwait();
            }
        }
        else
        {
            await ExecAsync(async db =>
            {
                if (!roles.IsEmpty())
                {
                    await db.DeleteAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id && roles.Contains(q.Role), token: token).ConfigAwait();
                }
                if (!permissions.IsEmpty())
                {
                    await db.DeleteAsync<UserAuthRole>(q => q.UserAuthId == userAuth.Id && permissions.Contains(q.Permission), token: token).ConfigAwait();
                }
            }).ConfigAwait();
        }
    }

    public async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            return await db.ExistsAsync<ApiKey>(x => x.Id == apiKey, token).ConfigAwait();
        }).ConfigAwait();
    }

    public async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token=default)
    {
        return await ExecAsync(async db => 
            await db.SingleByIdAsync<ApiKey>(apiKey, token).ConfigAwait()).ConfigAwait();
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

            return await db.SelectAsync(q, token).ConfigAwait();
        }).ConfigAwait();
    }

    public async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token=default)
    {
        await ExecAsync(async db =>
        {
            await db.SaveAllAsync(apiKeys, token).ConfigAwait();
        }).ConfigAwait();
    }
}