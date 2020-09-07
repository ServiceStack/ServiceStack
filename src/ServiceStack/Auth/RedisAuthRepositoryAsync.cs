#if NET472 || NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public partial class RedisAuthRepository<TUserAuth, TUserAuthDetails> 
        : IUserAuthRepositoryAsync, IClearableAsync, IManageApiKeysAsync, IQueryUserAuthAsync
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        private async Task AssertNoExistingUserAsync(IRedisClientFacadeAsync redis, IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token=default)
        {
            if (newUser.UserName != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(redis, newUser.UserName, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(redis, newUser.Email, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
            }
        }

        public virtual async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token=default)
        {
            newUser.ValidateNewUser(password);

            await using var redis = await factory.GetClientAsync(token);
            await AssertNoExistingUserAsync(redis, newUser, token: token);

            newUser.Id = await redis.AsAsync<IUserAuth>().GetNextSequenceAsync(token);
            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
            if (!newUser.UserName.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexUserNameToUserId, newUser.UserName, userId, token);
            }
            if (!newUser.Email.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexEmailToUserId, newUser.Email, userId, token);
            }

            await redis.StoreAsync(newUser, token);

            return newUser;
        }

        public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default)
        {
            newUser.ValidateNewUser(password);

            await using var redis = await factory.GetClientAsync(token);
            await AssertNoExistingUserAsync(redis, newUser, existingUser, token);

            if (existingUser.UserName != newUser.UserName && existingUser.UserName != null)
            {
                await redis.RemoveEntryFromHashAsync(IndexUserNameToUserId, existingUser.UserName, token);
            }
            if (existingUser.Email != newUser.Email && existingUser.Email != null)
            {
                await redis.RemoveEntryFromHashAsync(IndexEmailToUserId, existingUser.Email, token);
            }

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
            if (!newUser.UserName.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexUserNameToUserId, newUser.UserName, userId, token);
            }
            if (!newUser.Email.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexEmailToUserId, newUser.Email, userId, token);
            }

            await redis.StoreAsync(newUser, token);

            return newUser;
        }

        public virtual async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default)
        {
            newUser.ValidateNewUser();

            await using var redis = await factory.GetClientAsync(token);
            await AssertNoExistingUserAsync(redis, newUser, existingUser, token);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            await redis.StoreAsync(newUser, token);

            return newUser;
        }

        public virtual async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default)
        {
            if (userNameOrEmail == null)
                return null;

            await using var redis = await factory.GetClientAsync(token);
            return await GetUserAuthByUserNameAsync(redis, userNameOrEmail, token);
        }

        private async Task<IUserAuth> GetUserAuthByUserNameAsync(IRedisClientFacadeAsync redis, string userNameOrEmail, CancellationToken token=default)
        {
            if (userNameOrEmail == null)
                return null;

            var isEmail = userNameOrEmail.Contains("@");
            var userId = isEmail
                ? await redis.GetValueFromHashAsync(IndexEmailToUserId, userNameOrEmail, token)
                : await redis.GetValueFromHashAsync(IndexUserNameToUserId, userNameOrEmail, token);

            return userId == null ? null : await redis.AsAsync<IUserAuth>().GetByIdAsync(userId, token);
        }

        public virtual async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token=default)
        {
            //userId = null;
            var userAuth = await GetUserAuthByUserNameAsync(userName, token);
            if (userAuth == null)
                return null;

            if (userAuth.VerifyPassword(password, out var needsRehash))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, needsRehash, password, token: token);
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token);

            return null;
        }

        public async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, 
            CancellationToken token=default)
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

        public virtual async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            var existingUser = await GetUserAuthAsync(redis, userAuthId, token);
            if (existingUser == null)
                return;

            await redis.AsAsync<IUserAuth>().DeleteByIdAsync(userAuthId, token);

            var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
            var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token);
            await redis.AsAsync<TUserAuthDetails>().DeleteByIdsAsync(authProviderIds, token);

            if (existingUser.UserName != null)
            {
                await redis.RemoveEntryFromHashAsync(IndexUserNameToUserId, existingUser.UserName, token);
            }
            if (existingUser.Email != null)
            {
                await redis.RemoveEntryFromHashAsync(IndexEmailToUserId, existingUser.Email, token);
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
            await using var redis = await factory.GetClientAsync(token);
            return await GetUserAuthAsync(redis, userAuthId, token);
        }

        public virtual async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? await GetUserAuthAsync(redis, authSession.UserAuthId, token)
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await redis.StoreAsync(userAuth, token);
        }

        public async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token=default)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
            {
                userAuth.CreatedDate = userAuth.ModifiedDate;
            }

            await using var redis = await factory.GetClientAsync(token);
            await redis.StoreAsync(userAuth, token);

            var userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            if (!userAuth.UserName.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexUserNameToUserId, userAuth.UserName, userId, token);
            }
            if (!userAuth.Email.IsNullOrEmpty())
            {
                await redis.SetEntryInHashAsync(IndexEmailToUserId, userAuth.Email, userId, token);
            }
        }

        public virtual async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token=default)
        {
            if (userAuthId == null)
                throw new ArgumentNullException(nameof(userAuthId));

            await using var redis = await factory.GetClientAsync(token);
            var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
            var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token);
            return (await redis.AsAsync<TUserAuthDetails>().GetByIdsAsync(authProviderIds, token))
                .OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
        }

        public virtual async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            return await GetUserAuthAsync(redis, authSession, tokens, token);
        }

        private async Task<IUserAuth> GetUserAuthAsync(IRedisClientFacadeAsync redis, IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthAsync(redis, authSession.UserAuthId, token);
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

            var oAuthProviderId = await GetAuthProviderByUserIdAsync(redis, tokens.Provider, tokens.UserId, token);
            if (!oAuthProviderId.IsNullOrEmpty())
            {
                var oauthProvider = await redis.AsAsync<TUserAuthDetails>().GetByIdAsync(oAuthProviderId, token);
                if (oauthProvider != null)
                    return await redis.AsAsync<IUserAuth>().GetByIdAsync(oauthProvider.UserAuthId, token);
            }
            return null;
        }

        public virtual async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            TUserAuthDetails authDetails = null;

            var oAuthProviderId = await GetAuthProviderByUserIdAsync(redis, tokens.Provider, tokens.UserId, token);
            if (!oAuthProviderId.IsNullOrEmpty())
                authDetails = await redis.AsAsync<TUserAuthDetails>().GetByIdAsync(oAuthProviderId, token);

            var userAuth = await GetUserAuthAsync(redis, authSession, tokens, token);
            if (userAuth == null)
            {
                userAuth = typeof(TUserAuth).CreateInstance<TUserAuth>();
                userAuth.Id = await redis.AsAsync<IUserAuth>().GetNextSequenceAsync(token);
            }

            if (authDetails == null)
            {
                authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
                authDetails.Id = await redis.AsAsync<TUserAuthDetails>().GetNextSequenceAsync(token);
                authDetails.UserAuthId = userAuth.Id;
                authDetails.Provider = tokens.Provider;
                authDetails.UserId = tokens.UserId;
                var idx = IndexProviderToUserIdHash(tokens.Provider);
                await redis.SetEntryInHashAsync(idx, tokens.UserId, authDetails.Id.ToString(CultureInfo.InvariantCulture), token);
            }

            authDetails.PopulateMissing(tokens, overwriteReserved: true);
            userAuth.PopulateMissingExtended(authDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;

            authDetails.ModifiedDate = userAuth.ModifiedDate;

            await redis.StoreAsync(userAuth, token);
            await redis.StoreAsync(authDetails, token);
            await redis.AddItemToSetAsync(IndexUserAuthAndProviderIdsSet(userAuth.Id), authDetails.Id.ToString(CultureInfo.InvariantCulture), token);

            return authDetails;
        }

        private async Task<string> GetAuthProviderByUserIdAsync(IRedisClientFacadeAsync redis, string provider, string userId, CancellationToken token=default)
        {
            var idx = IndexProviderToUserIdHash(provider);
            var oAuthProviderId = await redis.GetValueFromHashAsync(idx, userId, token);
            return oAuthProviderId;
        }

        public virtual Task InitApiKeySchemaAsync(CancellationToken token = default) => TypeConstants.EmptyTask;

        public virtual async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            return redis.AsAsync<ApiKey>().GetByIdAsync(apiKey, token) != null;
        }

        public virtual async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            return await redis.AsAsync<ApiKey>().GetByIdAsync(apiKey, token);
        }

        public virtual async Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            var idx = IndexUserAuthAndApiKeyIdsSet(long.Parse(userId));
            var authProviderIds = await redis.GetAllItemsFromSetAsync(idx, token);
            var apiKeys = await redis.AsAsync<ApiKey>().GetByIdsAsync(authProviderIds, token);
            return apiKeys
                .Where(x => x.CancelledDate == null 
                            && (x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow))
                .OrderByDescending(x => x.CreatedDate).ToList();
        }

        public virtual async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            foreach (var apiKey in apiKeys)
            {
                var userAuthId = long.Parse(apiKey.UserAuthId);
                await redis.StoreAsync(apiKey, token);
                await redis.AddItemToSetAsync(IndexUserAuthAndApiKeyIdsSet(userAuthId), apiKey.Id, token);
            }
        }

        public virtual async Task ClearAsync(CancellationToken token=default)
        {
            await factory.ClearAsync(token);
        }

        public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            if (orderBy == null && (skip != null || take != null))
                return QueryUserAuths(await redis.AsAsync<IUserAuth>().GetAllAsync(skip, take, token));

            return QueryUserAuths(await redis.AsAsync<IUserAuth>().GetAllAsync(token: token), orderBy: orderBy, skip: skip, take: take);
        }

        public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            await using var redis = await factory.GetClientAsync(token);
            var results = await redis.AsAsync<IUserAuth>().GetAllAsync(token: token);
            return QueryUserAuths(results, query: query, orderBy: orderBy, skip: skip, take: take);
        }
    }
    
}
#endif