using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Auth;

public partial class RedisAuthRepository<TUserAuth, TUserAuthDetails> 
    : IUserAuthRepositoryAsync, IClearableAsync, IManageApiKeysAsync, IQueryUserAuthAsync
    where TUserAuth : class, IUserAuth
    where TUserAuthDetails : class, IUserAuthDetails
{
    private async Task AssertNoExistingUserAsync(IRedisClientFacadeAsync redis, IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token=default)
    {
        if (newUser.UserName != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(redis, newUser.UserName, token).ConfigAwait();
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
        }
        if (newUser.Email != null)
        {
            var existingUser = await GetUserAuthByUserNameAsync(redis, newUser.Email, token).ConfigAwait();
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
        }
    }

    public virtual async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token=default)
    {
        newUser.ValidateNewUser(password);

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        await AssertNoExistingUserAsync(redis, newUser, token: token).ConfigAwait();

        newUser.Id = await redis.AsAsync<IUserAuth>().GetNextSequenceAsync(token).ConfigAwait();
        newUser.PopulatePasswordHashes(password);
        newUser.CreatedDate = DateTime.UtcNow;
        newUser.ModifiedDate = newUser.CreatedDate;

        var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!newUser.UserName.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexUserNameToUserId, newUser.UserName, userId, token).ConfigAwait();
        }
        if (!newUser.Email.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexEmailToUserId, newUser.Email, userId, token).ConfigAwait();
        }

        await redis.StoreAsync(newUser, token).ConfigAwait();

        return newUser;
    }

    public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default)
    {
        newUser.ValidateNewUser(password);

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        await AssertNoExistingUserAsync(redis, newUser, existingUser, token).ConfigAwait();

        if (existingUser.UserName != newUser.UserName && existingUser.UserName != null)
        {
            await redis.RemoveEntryFromHashAsync(IndexUserNameToUserId, existingUser.UserName, token).ConfigAwait();
        }
        if (existingUser.Email != newUser.Email && existingUser.Email != null)
        {
            await redis.RemoveEntryFromHashAsync(IndexEmailToUserId, existingUser.Email, token).ConfigAwait();
        }

        newUser.Id = existingUser.Id;
        newUser.PopulatePasswordHashes(password, existingUser);
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!newUser.UserName.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexUserNameToUserId, newUser.UserName, userId, token).ConfigAwait();
        }
        if (!newUser.Email.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexEmailToUserId, newUser.Email, userId, token).ConfigAwait();
        }

        await redis.StoreAsync(newUser, token).ConfigAwait();

        return newUser;
    }

    public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default)
    {
        newUser.ValidateNewUser();

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        await AssertNoExistingUserAsync(redis, newUser, existingUser, token).ConfigAwait();

        newUser.Id = existingUser.Id;
        newUser.PasswordHash = existingUser.PasswordHash;
        newUser.Salt = existingUser.Salt;
        newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        await redis.StoreAsync(newUser, token).ConfigAwait();

        return newUser;
    }

    public virtual async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default)
    {
        if (userNameOrEmail == null)
            return null;

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        return await GetUserAuthByUserNameAsync(redis, userNameOrEmail, token).ConfigAwait();
    }

    private async Task<IUserAuth> GetUserAuthByUserNameAsync(IRedisClientFacadeAsync redis, string userNameOrEmail, CancellationToken token=default)
    {
        if (userNameOrEmail == null)
            return null;

        var isEmail = userNameOrEmail.Contains("@");
        var userId = isEmail
            ? await redis.GetValueFromHashAsync(IndexEmailToUserId, userNameOrEmail, token).ConfigAwait()
            : await redis.GetValueFromHashAsync(IndexUserNameToUserId, userNameOrEmail, token).ConfigAwait();

        return userId == null ? null : await redis.AsAsync<IUserAuth>().GetByIdAsync(userId, token).ConfigAwait();
    }

    public virtual async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token=default)
    {
        //userId = null;
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

    public async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, 
        CancellationToken token=default)
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

    public virtual async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        var existingUser = await GetUserAuthAsync(redis, userAuthId, token).ConfigAwait();
        if (existingUser == null)
            return;

        await redis.AsAsync<IUserAuth>().DeleteByIdAsync(userAuthId, token).ConfigAwait();

        var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
        var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token).ConfigAwait();
        await redis.AsAsync<TUserAuthDetails>().DeleteByIdsAsync(authProviderIds, token).ConfigAwait();

        if (existingUser.UserName != null)
        {
            await redis.RemoveEntryFromHashAsync(IndexUserNameToUserId, existingUser.UserName, token).ConfigAwait();
        }
        if (existingUser.Email != null)
        {
            await redis.RemoveEntryFromHashAsync(IndexEmailToUserId, existingUser.Email, token).ConfigAwait();
        }
    }

    private Task<IUserAuth> GetUserAuthAsync(IRedisClientFacadeAsync redis, string userAuthId, CancellationToken token=default)
    {
        if (userAuthId == null || !long.TryParse(userAuthId, out var longId))
            return null;

        return redis.AsAsync<IUserAuth>().GetByIdAsync(longId, token);
    }

    public virtual async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        return await GetUserAuthAsync(redis, userAuthId, token).ConfigAwait();
    }

    public virtual async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
            ? await GetUserAuthAsync(redis, authSession.UserAuthId, token).ConfigAwait()
            : authSession.ConvertTo<TUserAuth>();

        if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
            userAuth.Id = int.Parse(authSession.UserAuthId);

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default)
            userAuth.CreatedDate = userAuth.ModifiedDate;

        await redis.StoreAsync(userAuth, token).ConfigAwait();
    }

    public async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token=default)
    {
        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default)
        {
            userAuth.CreatedDate = userAuth.ModifiedDate;
        }

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        await redis.StoreAsync(userAuth, token).ConfigAwait();

        var userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
        if (!userAuth.UserName.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexUserNameToUserId, userAuth.UserName, userId, token).ConfigAwait();
        }
        if (!userAuth.Email.IsNullOrEmpty())
        {
            await redis.SetEntryInHashAsync(IndexEmailToUserId, userAuth.Email, userId, token).ConfigAwait();
        }
    }

    public virtual async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token=default)
    {
        if (userAuthId == null)
            throw new ArgumentNullException(nameof(userAuthId));

        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
        var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token).ConfigAwait();
        return (await redis.AsAsync<TUserAuthDetails>().GetByIdsAsync(authProviderIds, token).ConfigAwait())
            .OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
    }

    public virtual async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        return await GetUserAuthAsync(redis, authSession, tokens, token).ConfigAwait();
    }

    private async Task<IUserAuth> GetUserAuthAsync(IRedisClientFacadeAsync redis, IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
    {
        if (!authSession.UserAuthId.IsNullOrEmpty())
        {
            var userAuth = await GetUserAuthAsync(redis, authSession.UserAuthId, token).ConfigAwait();
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

        var oAuthProviderId = await GetAuthProviderByUserIdAsync(redis, tokens.Provider, tokens.UserId, token).ConfigAwait();
        if (!oAuthProviderId.IsNullOrEmpty())
        {
            var oauthProvider = await redis.AsAsync<TUserAuthDetails>().GetByIdAsync(oAuthProviderId, token).ConfigAwait();
            if (oauthProvider != null)
                return await redis.AsAsync<IUserAuth>().GetByIdAsync(oauthProvider.UserAuthId, token);
        }
        return null;
    }

    public virtual async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        TUserAuthDetails authDetails = null;

        var oAuthProviderId = await GetAuthProviderByUserIdAsync(redis, tokens.Provider, tokens.UserId, token).ConfigAwait();
        if (!oAuthProviderId.IsNullOrEmpty())
            authDetails = await redis.AsAsync<TUserAuthDetails>().GetByIdAsync(oAuthProviderId, token).ConfigAwait();

        var userAuth = await GetUserAuthAsync(redis, authSession, tokens, token).ConfigAwait();
        if (userAuth == null)
        {
            userAuth = typeof(TUserAuth).CreateInstance<TUserAuth>();
            userAuth.Id = await redis.AsAsync<IUserAuth>().GetNextSequenceAsync(token).ConfigAwait();
        }

        if (authDetails == null)
        {
            authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
            authDetails.Id = await redis.AsAsync<TUserAuthDetails>().GetNextSequenceAsync(token).ConfigAwait();
            authDetails.UserAuthId = userAuth.Id;
            authDetails.Provider = tokens.Provider;
            authDetails.UserId = tokens.UserId;
            var idx = IndexProviderToUserIdHash(tokens.Provider);
            await redis.SetEntryInHashAsync(idx, tokens.UserId, authDetails.Id.ToString(CultureInfo.InvariantCulture), token).ConfigAwait();
        }

        authDetails.PopulateMissing(tokens, overwriteReserved: true);
        userAuth.PopulateMissingExtended(authDetails);

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (authDetails.CreatedDate == default)
            authDetails.CreatedDate = userAuth.ModifiedDate;

        authDetails.ModifiedDate = userAuth.ModifiedDate;

        await redis.StoreAsync(userAuth, token).ConfigAwait();
        await redis.StoreAsync(authDetails, token).ConfigAwait();
        await redis.AddItemToSetAsync(IndexUserAuthAndProviderIdsSet(userAuth.Id), authDetails.Id.ToString(CultureInfo.InvariantCulture), token).ConfigAwait();

        return authDetails;
    }

    private async Task<string> GetAuthProviderByUserIdAsync(IRedisClientFacadeAsync redis, string provider, string userId, CancellationToken token=default)
    {
        var idx = IndexProviderToUserIdHash(provider);
        var oAuthProviderId = await redis.GetValueFromHashAsync(idx, userId, token).ConfigAwait();
        return oAuthProviderId;
    }

    public virtual Task InitApiKeySchemaAsync(CancellationToken token = default) => TypeConstants.EmptyTask;

    public virtual async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        return redis.AsAsync<ApiKey>().GetByIdAsync(apiKey, token) != null;
    }

    public virtual async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        return await redis.AsAsync<ApiKey>().GetByIdAsync(apiKey, token).ConfigAwait();
    }

    public virtual async Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        var idx = IndexUserAuthAndApiKeyIdsSet(long.Parse(userId));
        var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token).ConfigAwait();
        var apiKeys = await redis.AsAsync<ApiKey>().GetByIdsAsync(authProviderIds, token).ConfigAwait();
        return apiKeys
            .Where(x => x.CancelledDate == null 
                        && (x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow))
            .OrderByDescending(x => x.CreatedDate).ToList();
    }

    public virtual async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        foreach (var apiKey in apiKeys)
        {
            var userAuthId = long.Parse(apiKey.UserAuthId);
            await redis.StoreAsync(apiKey, token).ConfigAwait();
            await redis.AddItemToSetAsync(IndexUserAuthAndApiKeyIdsSet(userAuthId), apiKey.Id, token).ConfigAwait();
        }
    }

    public virtual async Task ClearAsync(CancellationToken token=default)
    {
        await factory.ClearAsync(token).ConfigAwait();
    }

    public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        if (orderBy == null && (skip != null || take != null))
            return QueryUserAuths(await redis.AsAsync<IUserAuth>().GetAllAsync(skip, take, token).ConfigAwait());

        return QueryUserAuths(await redis.AsAsync<IUserAuth>().GetAllAsync(token: token).ConfigAwait(), orderBy: orderBy, skip: skip, take: take);
    }

    public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
    {
        await using var redis = await factory.GetClientAsync(token).ConfigAwait();
        var results = await redis.AsAsync<IUserAuth>().GetAllAsync(token: token).ConfigAwait();
        return QueryUserAuths(results, query: query, orderBy: orderBy, skip: skip, take: take);
    }
}