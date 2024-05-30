// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDb;

public partial class DynamoDbAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepositoryAsync, IManageRolesAsync, IClearableAsync, IManageApiKeysAsync, IQueryUserAuthAsync
    where TUserAuth : class, IUserAuth
    where TUserAuthDetails : class, IUserAuthDetails
{
    private async Task AssertNoExistingUserAsync(IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token=default)
    {
        if (newUser.UserName != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(newUser.UserName, token);
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException($"User {newUser.UserName} already exists");
        }
        if (newUser.Email != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(newUser.Email, token);
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException($"Email {newUser.Email} already exists");
        }
    }

    public async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default)
    {
        newUser.ValidateNewUser(password);

        await AssertNoExistingUserAsync(newUser, token: token);

        Sanitize(newUser);

        newUser.PopulatePasswordHashes(password);
        newUser.CreatedDate = DateTime.UtcNow;
        newUser.ModifiedDate = newUser.CreatedDate;

        await Db.PutItemAsync((TUserAuth)newUser, token: token);

        newUser = DeSanitize(Db.GetItem<TUserAuth>(newUser.Id));
        return newUser;
    }

    public async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token = default)
    {
        newUser.ValidateNewUser();

        await AssertNoExistingUserAsync(newUser, token: token);

        newUser.PasswordHash = existingUser.PasswordHash;
        newUser.Salt = existingUser.Salt;
        newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        await Db.PutItemAsync(Sanitize(newUser), token: token);

        newUser = DeSanitize(Db.GetItem<TUserAuth>(newUser.Id));
        return newUser;
    }

    public async Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token = default)
    {
        session.ThrowIfNull("session");

        var userAuth = await GetUserAuthAsync(session, tokens, token);
        await LoadUserAuthAsync(session, userAuth, token);
    }

    private async Task LoadUserAuthAsync(IAuthSession session, IUserAuth userAuth, CancellationToken token=default)
    {
        await session.PopulateSessionAsync(userAuth, this, token);
    }

    public async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token = default)
    {
        return DeSanitize(await Db.GetItemAsync<TUserAuth>(int.Parse(userAuthId), token));
    }

    public async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token = default)
    {
        if (authSession == null)
            throw new ArgumentNullException(nameof(authSession));

        var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
            ? await Db.GetItemAsync<TUserAuth>(int.Parse(authSession.UserAuthId), token)
            : authSession.ConvertTo<TUserAuth>();

        if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
            userAuth.Id = int.Parse(authSession.UserAuthId);

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default)
            userAuth.CreatedDate = userAuth.ModifiedDate;

        await Db.PutItemAsync(Sanitize(userAuth), token: token);
    }

    public async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token = default)
    {
        if (userAuth == null)
            throw new ArgumentNullException(nameof(userAuth));

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default)
            userAuth.CreatedDate = userAuth.ModifiedDate;

        await Db.PutItemAsync(Sanitize((TUserAuth)userAuth), token: token);
    }

    public async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token = default)
    {
        var id = int.Parse(userAuthId);
        return (await Db.QueryAsync(Db.FromQuery<TUserAuthDetails>(q => q.UserAuthId == id), token).ToListAsync(token))
            .Cast<IUserAuthDetails>().ToList();
    }

    public async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
    {
        TUserAuth userAuth = (TUserAuth)await GetUserAuthAsync(authSession, tokens, token)
                             ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

        TUserAuthDetails authDetails = null;
        var index = await GetUserAuthByProviderUserIdAsync(tokens.Provider, tokens.UserId, token);
        if (index != null)
            authDetails = await Db.GetItemAsync<TUserAuthDetails>(index.UserAuthId, index.Id, token);

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

        Sanitize(userAuth);

        await Db.PutItemAsync(userAuth, token: token);

        authDetails.UserAuthId = userAuth.Id;

        authDetails.ModifiedDate = userAuth.ModifiedDate;
        if (authDetails.CreatedDate == default)
            authDetails.CreatedDate = userAuth.ModifiedDate;

        await Db.PutItemAsync(authDetails, token: token);

        return authDetails;
    }

    public async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
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

        var authProviderIndex = await GetUserAuthByProviderUserIdAsync(tokens.Provider, tokens.UserId, token);
        if (authProviderIndex != null)
        {
            var userAuth = DeSanitize(await Db.GetItemAsync<TUserAuth>(authProviderIndex.UserAuthId, token));
            return userAuth;
        }
        return null;
    }

    private async Task<UserIdUserAuthDetailsIndex> GetUserAuthByProviderUserIdAsync(string provider, string userId, CancellationToken token=default)
    {
        var oAuthProvider = (await Db.FromQueryIndex<UserIdUserAuthDetailsIndex>(
                    q => q.UserId == userId && q.Provider == provider)
                .ExecAsync(token))
            .FirstOrDefault();

        return oAuthProvider;
    }

    public async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token = default)
    {
        if (userNameOrEmail == null)
            return null;

        if (LowerCaseUsernames)
            userNameOrEmail = userNameOrEmail.ToLower();

        var index = (await Db.FromQueryIndex<UsernameUserAuthIndex>(q => q.UserName == userNameOrEmail)
            .ExecAsync(token)).FirstOrDefault();
        if (index == null)
            return null;

        var userAuthId = index.Id;

        return DeSanitize(await Db.GetItemAsync<TUserAuth>(userAuthId, token));
    }

    public async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default)
    {
        var userAuth = await GetUserAuthByUserNameAsync(userName, token);
        if (userAuth == null)
            return null;

        if (userAuth.VerifyPassword(password, out var needsRehash))
        {
            await this.RecordSuccessfulLoginAsync(userAuth, needsRehash, password, token);
            return userAuth;
        }

        await this.RecordInvalidLoginAttemptAsync(userAuth, token);

        return null;
    }

    public async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, CancellationToken token = default)
    {
        var userAuth = await GetUserAuthByUserNameAsync(digestHeaders["username"], token);
        if (userAuth == null)
            return null;

        if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
        {
            await this.RecordSuccessfulLoginAsync(userAuth, token);
            return userAuth;
        }

        await this.RecordInvalidLoginAttemptAsync(userAuth, token);

        return null;
    }

    public async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token = default)
    {
        ValidateNewUser(newUser, password);

        await AssertNoExistingUserAsync(newUser, existingUser, token);

        //DynamoDb does not allow null hash keys on Global Indexes
        //Workaround by populating UserName with Email when null
        if (newUser.UserName == null)
            newUser.UserName = newUser.Email;

        if (this.LowerCaseUsernames)
            newUser.UserName = newUser.UserName.ToLower();

        var hash = existingUser.PasswordHash;
        var salt = existingUser.Salt;
        if (password != null)
            HostContext.Resolve<IHashProvider>().GetHashAndSaltString(password, out hash, out salt);

        var digestHash = existingUser.DigestHa1Hash;
        if (password != null || existingUser.UserName != newUser.UserName)
            digestHash = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);

        newUser.Id = existingUser.Id;
        newUser.PasswordHash = hash;
        newUser.Salt = salt;
        newUser.DigestHa1Hash = digestHash;
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        await Db.PutItemAsync(Sanitize((TUserAuth)newUser), token: token);

        return newUser;
    }

    public async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token = default)
    {
        var userId = int.Parse(userAuthId);

        await Db.DeleteItemAsync<TUserAuth>(userAuthId, token: token);

        var userAuthDetails = await Db.FromQuery<TUserAuthDetails>(x => x.UserAuthId == userId)
            .Select(x => x.Id)
            .ExecAsync(token);
        var userAuthDetailsKeys = userAuthDetails.Map(x => new DynamoId(x.UserAuthId, x.Id));
        await Db.DeleteItemsAsync<TUserAuthDetails>(userAuthDetailsKeys, token);

        var userAuthRoles = await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userId)
            .Select(x => x.Id)
            .ExecAsync(token);
        var userAuthRolesKeys = userAuthRoles.Map(x => new DynamoId(x.UserAuthId, x.Id));
        await Db.DeleteItemsAsync<UserAuthRole>(userAuthRolesKeys, token);
    }

    public async Task ClearAsync(CancellationToken token = default)
    {
        await Db.DeleteItemsAsync<TUserAuth>(Db.FromScan<TUserAuth>().ExecColumn(x => x.Id), token);

        var qDetails = Db.FromScan<TUserAuthDetails>().Select(x => new { x.UserAuthId, x.Id });
        await Db.DeleteItemsAsync<TUserAuthDetails>(qDetails.Exec().Map(x => new DynamoId(x.UserAuthId, x.Id)), token);

        var qRoles = Db.FromScan<UserAuthRole>().Select(x => new { x.UserAuthId, x.Id });
        await Db.DeleteItemsAsync<UserAuthRole>(qRoles.Exec().Map(x => new DynamoId(x.UserAuthId, x.Id)), token);
    }

    public async Task<ICollection<string>> GetRolesAsync(string userAuthId, CancellationToken token = default)
    {
        var authId = int.Parse(userAuthId);
        return (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Role != null)
                .ExecAsync(token))
            .Map(x => x.Role);
    }

    public async Task<ICollection<string>> GetPermissionsAsync(string userAuthId, CancellationToken token = default)
    {
        var authId = int.Parse(userAuthId);
        return (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Permission != null)
                .ExecAsync(token))
            .Map(x => x.Permission);
    }

    public async Task<Tuple<ICollection<string>, ICollection<string>>> GetRolesAndPermissionsAsync(string userAuthId, CancellationToken token = default)
    {
        var authId = int.Parse(userAuthId);
        var results = await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
            .ExecAsync(token);

        ICollection<string> roles = new List<string>();
        ICollection<string> permissions = new List<string>();
        foreach (var result in results)
        {
            if (result.Role != null)
                roles.Add(result.Role);
            if (result.Permission != null)
                permissions.Add(result.Permission);
        }

        return Tuple.Create(roles, permissions);
    }

    public async Task<bool> HasRoleAsync(string userAuthId, string role, CancellationToken token = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        if (userAuthId == null)
            return false;

        var authId = int.Parse(userAuthId);

        return (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Role == role)
                .ExecAsync(token))
            .Any();
    }

    public async Task<bool> HasPermissionAsync(string userAuthId, string permission, CancellationToken token = default)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        if (userAuthId == null)
            return false;

        var authId = int.Parse(userAuthId);

        return (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Permission == permission)
                .ExecAsync(token))
            .Any();
    }

    public async Task AssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token = default)
    {
        var userAuth = await GetUserAuthAsync(userAuthId, token);
        var now = DateTime.UtcNow;

        var userRoles = (await Db.FromQuery<UserAuthRole>(q => q.UserAuthId == userAuth.Id)
            .ExecAsync(token)).ToList();

        if (!roles.IsEmpty())
        {
            var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToSet();
            foreach (var role in roles)
            {
                if (!roleSet.Contains(role))
                {
                    await Db.PutRelatedItemAsync(userAuth.Id, new UserAuthRole
                    {
                        Role = role,
                        CreatedDate = now,
                        ModifiedDate = now,
                    }, token);
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
                    await Db.PutRelatedItemAsync(userAuth.Id, new UserAuthRole
                    {
                        Permission = permission,
                        CreatedDate = now,
                        ModifiedDate = now,
                    }, token);
                }
            }
        }
    }

    public async Task UnAssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token = default)
    {
        var userAuth = await GetUserAuthAsync(userAuthId, token);

        if (!roles.IsEmpty())
        {
            var authRoleIds = (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userAuth.Id)
                    .Filter(x => roles.Contains(x.Role))
                    .Select(x => new {x.UserAuthId, x.Id})
                    .ExecAsync(token))
                .Map(x => new DynamoId(x.UserAuthId, x.Id));

            await Db.DeleteItemsAsync<UserAuthRole>(authRoleIds, token);
        }

        if (!permissions.IsEmpty())
        {
            var authRoleIds = (await Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userAuth.Id)
                    .Filter(x => permissions.Contains(x.Permission))
                    .Select(x => new {x.UserAuthId, x.Id})
                    .ExecAsync(token))
                .Map(x => new DynamoId(x.UserAuthId, x.Id));

            await Db.DeleteItemsAsync<UserAuthRole>(authRoleIds, token);
        }
    }

    public async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token = default)
    {
        return (await Db.FromQueryIndex<ApiKeyIdIndex>(x => x.Id == apiKey).ExecAsync(1, token)).Count > 0;
    }

    public async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token = default)
    {
        return (await Db.FromQueryIndex<ApiKeyIdIndex>(x => x.Id == apiKey)
                .ExecIntoAsync<ApiKey>(token))
            .FirstOrDefault();
    }

    public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token = default)
    {
        return (await Db.FromQuery<ApiKey>(x => x.UserAuthId == userId)
                .Filter(x => x.CancelledDate == null
                             && (x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow))
                .ExecAsync(token))
            .OrderByDescending(x => x.CreatedDate)
            .ToList();
    }

    public async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token = default)
    {
        await Db.PutItemsAsync(apiKeys, token);
    }

    public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
    {
        return (await Db.FromScan<TUserAuth>().ExecAsync(token)).SortAndPage(orderBy, skip, take).Map(DeSanitize).ToList();
    }

    public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
    {
        return (await Db.FromScan<TUserAuth>().Filter(x =>
                x.UserName.Contains(query) || x.DisplayName.Contains(query) || x.Company.Contains(query))
            .ExecAsync(token)).SortAndPage(orderBy, skip, take).Map(DeSanitize).ToList();
    }
}