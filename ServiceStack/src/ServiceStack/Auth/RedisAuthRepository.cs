using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ServiceStack.Redis;

namespace ServiceStack.Auth;

public class RedisAuthRepository : RedisAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
{
    public RedisAuthRepository(IRedisClientsManager factory) : base(factory) { }

    public RedisAuthRepository(IRedisClientManagerFacade factory) : base(factory) { }
}

public partial class RedisAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IClearable, IManageApiKeys, ICustomUserAuth, IQueryUserAuth
    where TUserAuth : class, IUserAuth
    where TUserAuthDetails : class, IUserAuthDetails
{
    private readonly IRedisClientManagerFacade factory;

    public RedisAuthRepository(IRedisClientsManager factory)
        : this(new RedisClientManagerFacade(factory)) { }

    public RedisAuthRepository(IRedisClientManagerFacade factory) { this.factory = factory; }

    public string NamespacePrefix { get; set; }

    private string UsePrefix => NamespacePrefix ?? "";

    private string IndexUserAuthAndProviderIdsSet(long userAuthId) { return UsePrefix + "urn:UserAuth>UserOAuthProvider:" + userAuthId; }

    private string IndexProviderToUserIdHash(string provider) { return UsePrefix + "hash:ProviderUserId>OAuthProviderId:" + provider; }

    private string IndexUserNameToUserId => UsePrefix + "hash:UserAuth:UserName>UserId";

    private string IndexEmailToUserId => UsePrefix + "hash:UserAuth:Email>UserId";

    private string IndexUserAuthAndApiKeyIdsSet(long userAuthId) { return UsePrefix + "urn:UserAuth>ApiKey:" + userAuthId; }

    private void AssertNoExistingUser(IRedisClientFacade redis, IUserAuth newUser, IUserAuth exceptForExistingUser = null)
    {
        if (newUser.UserName != null)
        {
            var existingUser = GetUserAuthByUserName(redis, newUser.UserName);
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
        }
        if (newUser.Email != null)
        {
            var existingUser = GetUserAuthByUserName(redis, newUser.Email);
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
        }
    }

    public virtual IUserAuth CreateUserAuth(IUserAuth newUser, string password)
    {
        newUser.ValidateNewUser(password);

        using var redis = factory.GetClient();
        AssertNoExistingUser(redis, newUser);

        newUser.Id = redis.As<IUserAuth>().GetNextSequence();
        newUser.PopulatePasswordHashes(password);
        newUser.CreatedDate = DateTime.UtcNow;
        newUser.ModifiedDate = newUser.CreatedDate;

        var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!newUser.UserName.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexUserNameToUserId, newUser.UserName, userId);
        }
        if (!newUser.Email.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexEmailToUserId, newUser.Email, userId);
        }

        redis.Store(newUser);

        return newUser;
    }

    public virtual IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
    {
        newUser.ValidateNewUser(password);

        using var redis = factory.GetClient();
        AssertNoExistingUser(redis, newUser, existingUser);

        if (existingUser.UserName != newUser.UserName && existingUser.UserName != null)
        {
            redis.RemoveEntryFromHash(IndexUserNameToUserId, existingUser.UserName);
        }
        if (existingUser.Email != newUser.Email && existingUser.Email != null)
        {
            redis.RemoveEntryFromHash(IndexEmailToUserId, existingUser.Email);
        }

        newUser.Id = existingUser.Id;
        newUser.PopulatePasswordHashes(password, existingUser);
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        var userId = newUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!newUser.UserName.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexUserNameToUserId, newUser.UserName, userId);
        }
        if (!newUser.Email.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexEmailToUserId, newUser.Email, userId);
        }

        redis.Store(newUser);

        return newUser;
    }

    public virtual IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
    {
        newUser.ValidateNewUser();

        using var redis = factory.GetClient();
        AssertNoExistingUser(redis, newUser, existingUser);

        newUser.Id = existingUser.Id;
        newUser.PasswordHash = existingUser.PasswordHash;
        newUser.Salt = existingUser.Salt;
        newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
        newUser.CreatedDate = existingUser.CreatedDate;
        newUser.ModifiedDate = DateTime.UtcNow;

        redis.Store(newUser);

        return newUser;
    }

    public virtual IUserAuth GetUserAuthByUserName(string userNameOrEmail)
    {
        if (userNameOrEmail == null)
            return null;

        using var redis = factory.GetClient();
        return GetUserAuthByUserName(redis, userNameOrEmail);
    }

    private IUserAuth GetUserAuthByUserName(IRedisClientFacade redis, string userNameOrEmail)
    {
        if (userNameOrEmail == null)
            return null;

        var isEmail = userNameOrEmail.Contains("@");
        var userId = isEmail
            ? redis.GetValueFromHash(IndexEmailToUserId, userNameOrEmail)
            : redis.GetValueFromHash(IndexUserNameToUserId, userNameOrEmail);

        return userId == null ? null : redis.As<IUserAuth>().GetById(userId);
    }

    public virtual bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
    {
        //userId = null;
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

    public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
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

    public virtual void DeleteUserAuth(string userAuthId)
    {
        using var redis = factory.GetClient();
        var existingUser = GetUserAuth(redis, userAuthId);
        if (existingUser == null)
            return;

        redis.As<IUserAuth>().DeleteById(userAuthId);

        var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
        var authProviderIds = redis.GetAllItemsFromSet(idx);
        redis.As<TUserAuthDetails>().DeleteByIds(authProviderIds);

        if (existingUser.UserName != null)
        {
            redis.RemoveEntryFromHash(IndexUserNameToUserId, existingUser.UserName);
        }
        if (existingUser.Email != null)
        {
            redis.RemoveEntryFromHash(IndexEmailToUserId, existingUser.Email);
        }
    }

    private IUserAuth GetUserAuth(IRedisClientFacade redis, string userAuthId)
    {
        if (userAuthId == null || !long.TryParse(userAuthId, out var longId))
            return null;

        return redis.As<IUserAuth>().GetById(longId);
    }

    public virtual IUserAuth GetUserAuth(string userAuthId)
    {
        using var redis = factory.GetClient();
        return GetUserAuth(redis, userAuthId);
    }

    public virtual void SaveUserAuth(IAuthSession authSession)
    {
        using var redis = factory.GetClient();
        var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
            ? GetUserAuth(redis, authSession.UserAuthId)
            : authSession.ConvertTo<TUserAuth>();

        if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
            userAuth.Id = int.Parse(authSession.UserAuthId);

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default(DateTime))
            userAuth.CreatedDate = userAuth.ModifiedDate;

        redis.Store(userAuth);
    }

    public void SaveUserAuth(IUserAuth userAuth)
    {
        userAuth.ModifiedDate = DateTime.UtcNow;
        if (userAuth.CreatedDate == default(DateTime))
        {
            userAuth.CreatedDate = userAuth.ModifiedDate;
        }

        using var redis = factory.GetClient();
        redis.Store(userAuth);

        var userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
        if (!userAuth.UserName.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexUserNameToUserId, userAuth.UserName, userId);
        }
        if (!userAuth.Email.IsNullOrEmpty())
        {
            redis.SetEntryInHash(IndexEmailToUserId, userAuth.Email, userId);
        }
    }

    public virtual List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
    {
        if (userAuthId == null)
            throw new ArgumentNullException(nameof(userAuthId));

        using var redis = factory.GetClient();
        var idx = IndexUserAuthAndProviderIdsSet(long.Parse(userAuthId));
        var authProviderIds = redis.GetAllItemsFromSet(idx);
        return redis.As<TUserAuthDetails>().GetByIds(authProviderIds).OrderBy(x => x.ModifiedDate).Cast<IUserAuthDetails>().ToList();
    }

    public virtual IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
    {
        using var redis = factory.GetClient();
        return GetUserAuth(redis, authSession, tokens);
    }

    private IUserAuth GetUserAuth(IRedisClientFacade redis, IAuthSession authSession, IAuthTokens tokens)
    {
        if (!authSession.UserAuthId.IsNullOrEmpty())
        {
            var userAuth = GetUserAuth(redis, authSession.UserAuthId);
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

        var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
        if (!oAuthProviderId.IsNullOrEmpty())
        {
            var oauthProvider = redis.As<TUserAuthDetails>().GetById(oAuthProviderId);
            if (oauthProvider != null)
                return redis.As<IUserAuth>().GetById(oauthProvider.UserAuthId);
        }
        return null;
    }

    public virtual IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
    {
        using var redis = factory.GetClient();
        TUserAuthDetails authDetails = null;

        var oAuthProviderId = GetAuthProviderByUserId(redis, tokens.Provider, tokens.UserId);
        if (!oAuthProviderId.IsNullOrEmpty())
            authDetails = redis.As<TUserAuthDetails>().GetById(oAuthProviderId);

        var userAuth = GetUserAuth(redis, authSession, tokens);
        if (userAuth == null)
        {
            userAuth = typeof(TUserAuth).CreateInstance<TUserAuth>();
            userAuth.Id = redis.As<IUserAuth>().GetNextSequence();
        }

        if (authDetails == null)
        {
            authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
            authDetails.Id = redis.As<TUserAuthDetails>().GetNextSequence();
            authDetails.UserAuthId = userAuth.Id;
            authDetails.Provider = tokens.Provider;
            authDetails.UserId = tokens.UserId;
            var idx = IndexProviderToUserIdHash(tokens.Provider);
            redis.SetEntryInHash(idx, tokens.UserId, authDetails.Id.ToString(CultureInfo.InvariantCulture));
        }

        authDetails.PopulateMissing(tokens, overwriteReserved: true);
        userAuth.PopulateMissingExtended(authDetails);

        userAuth.ModifiedDate = DateTime.UtcNow;
        if (authDetails.CreatedDate == default(DateTime))
            authDetails.CreatedDate = userAuth.ModifiedDate;

        authDetails.ModifiedDate = userAuth.ModifiedDate;

        redis.Store(userAuth);
        redis.Store(authDetails);
        redis.AddItemToSet(IndexUserAuthAndProviderIdsSet(userAuth.Id), authDetails.Id.ToString(CultureInfo.InvariantCulture));

        return authDetails;
    }

    private string GetAuthProviderByUserId(IRedisClientFacade redis, string provider, string userId)
    {
        var idx = IndexProviderToUserIdHash(provider);
        var oAuthProviderId = redis.GetValueFromHash(idx, userId);
        return oAuthProviderId;
    }

    public virtual void InitApiKeySchema()
    {
    }

    public virtual bool ApiKeyExists(string apiKey)
    {
        using var redis = factory.GetClient();
        return redis.As<ApiKey>().GetById(apiKey) != null;
    }

    public virtual ApiKey GetApiKey(string apiKey)
    {
        using var redis = factory.GetClient();
        return redis.As<ApiKey>().GetById(apiKey);
    }

    public virtual List<ApiKey> GetUserApiKeys(string userId)
    {
        using var redis = factory.GetClient();
        var idx = IndexUserAuthAndApiKeyIdsSet(long.Parse(userId));
        var authProviderIds = redis.GetAllItemsFromSet(idx);
        var apiKeys = redis.As<ApiKey>().GetByIds(authProviderIds);
        return apiKeys
            .Where(x => x.CancelledDate == null 
                        && (x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow))
            .OrderByDescending(x => x.CreatedDate).ToList();
    }

    public virtual void StoreAll(IEnumerable<ApiKey> apiKeys)
    {
        using var redis = factory.GetClient();
        foreach (var apiKey in apiKeys)
        {
            var userAuthId = long.Parse(apiKey.UserAuthId);
            redis.Store(apiKey);
            redis.AddItemToSet(IndexUserAuthAndApiKeyIdsSet(userAuthId), apiKey.Id);
        }
    }

    public virtual void Clear() { factory.Clear(); }

    public IUserAuth CreateUserAuth()
    {
        return (IUserAuth)typeof(TUserAuth).CreateInstance();
    }

    public IUserAuthDetails CreateUserAuthDetails()
    {
        return (IUserAuthDetails)typeof(TUserAuthDetails).CreateInstance();
    }

    public List<IUserAuth> GetUserAuths(string orderBy = null, int? skip = null, int? take = null)
    {
        using var redis = factory.GetClient();
        if (orderBy == null && (skip != null || take != null))
            return QueryUserAuths(redis.As<IUserAuth>().GetAll(skip, take));

        return QueryUserAuths(redis.As<IUserAuth>().GetAll(), orderBy: orderBy, skip: skip, take: take);
    }

    public List<IUserAuth> SearchUserAuths(string query, string orderBy = null, int? skip = null, int? take = null)
    {
        using var redis = factory.GetClient();
        var results = redis.As<IUserAuth>().GetAll();
        return QueryUserAuths(results, query: query, orderBy: orderBy, skip: skip, take: take);
    }

    public virtual List<IUserAuth> QueryUserAuths(List<IUserAuth> results, string query = null,
        string orderBy = null, int? skip = null, int? take = null)
    {
        var to = !string.IsNullOrEmpty(query)
            ? results.Where(x => 
                x.UserName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.PrimaryEmail?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.Email?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.DisplayName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.Company?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            : results.AsEnumerable();

        return to.SortAndPage(orderBy, skip, take).ToList();
    }
}