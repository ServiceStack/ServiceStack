using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using ServiceStack.Auth;
using MongoDB.Driver;

namespace ServiceStack.Authentication.MongoDb
{
    public partial class MongoDbAuthRepository : IUserAuthRepositoryAsync, IClearableAsync, IManageApiKeysAsync, IQueryUserAuthAsync
    {
        public async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default)
        {
            newUser.ValidateNewUser(password);

            await AssertNoExistingUserAsync(mongoDatabase, newUser, token: token);

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            await SaveUserAsync(newUser, token);
            return newUser;
        }

        private async Task SaveUserAsync(IUserAuth userAuth, CancellationToken token=default)
        {
            if (userAuth.Id == default)
                userAuth.Id = await IncUserAuthCounterAsync(token);
            var usersCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            await usersCollection.ReplaceOneAsync<UserAuth>(u => u.Id == userAuth.Id, (UserAuth)userAuth, 
                new ReplaceOptions { IsUpsert = true }, cancellationToken: token);
        }

        private async Task<int> IncUserAuthCounterAsync(CancellationToken token=default)
        {
            return (await IncCounterAsync("UserAuthCounter", token)).UserAuthCounter;
        }

        private async Task<int> IncUserOAuthProviderCounterAsync(CancellationToken token=default)
        {
            return (await IncCounterAsync("UserOAuthProviderCounter", token)).UserOAuthProviderCounter;
        }

        private async Task<Counters> IncCounterAsync(string counterName, CancellationToken token=default)
        {
            var countersCollection = mongoDatabase.GetCollection<Counters>(CountersCol);
            var update = Builders<Counters>.Update.Inc(counterName, 1);
            var updatedCounters = await countersCollection.FindOneAndUpdateAsync(new BsonDocument(), update, 
                new FindOneAndUpdateOptions<Counters> { IsUpsert = true, ReturnDocument = ReturnDocument.After}, token);
            return updatedCounters;
        }

        private static async Task AssertNoExistingUserAsync(IMongoDatabase mongoDatabase, IUserAuth newUser, IUserAuth exceptForExistingUser = null,
            CancellationToken token=default)
        {
            if (newUser.UserName != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(mongoDatabase, newUser.UserName, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(mongoDatabase, newUser.Email, token);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
            }
        }

        public async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default)
        {
            newUser.ValidateNewUser(password);

            await AssertNoExistingUserAsync(mongoDatabase, newUser, existingUser, token);

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            await SaveUserAsync(newUser, token);

            return newUser;
        }

        public async Task<IUserAuth>  UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default)
        {
            newUser.ValidateNewUser();

            await AssertNoExistingUserAsync(mongoDatabase, newUser, token: token);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            await SaveUserAsync(newUser, token);

            return newUser;
        }

        public async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default)
        {
            return await GetUserAuthByUserNameAsync(mongoDatabase, userNameOrEmail, token);
        }

        private static async Task<UserAuth> GetUserAuthByUserNameAsync(IMongoDatabase mongoDatabase, string userNameOrEmail, CancellationToken token=default)
        {
            if (userNameOrEmail == null)
                return null;

            var isEmail = userNameOrEmail.Contains("@");
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);

            var builder = Builders<UserAuth>.Filter;
            var query = isEmail
                ? builder.Eq(auth => auth.Email, userNameOrEmail)
                : builder.Eq(auth => auth.UserName, userNameOrEmail);

            var userAuth = (await collection.FindAsync(query, cancellationToken: token)).FirstOrDefault();
            return userAuth;
        }

        private static async Task<List<IUserAuth>> SortAndPageAsync(IFindFluent<UserAuth, UserAuth> q, string orderBy, int? skip, int? take, CancellationToken token=default)
        {
            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy = AuthRepositoryUtils.ParseOrderBy(orderBy, out var desc);
                q = q.Sort(desc ? Builders<UserAuth>.Sort.Descending(orderBy) : Builders<UserAuth>.Sort.Ascending(orderBy));
            }

            if (skip != null)
                q = q.Skip(skip.Value);
            if (take != null)
                q = q.Limit(take.Value);

            return (await q.ToListAsync(token)).ConvertAll(x => x as IUserAuth);
        }

        public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            return await SortAndPageAsync(collection.Find(Builders<UserAuth>.Filter.Empty), orderBy, skip, take, token);
        }

        public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            var filter = Builders<UserAuth>.Filter;
            var q = filter.Where(x => x.UserName.Contains(query) ||
                  x.Email.Contains(query) ||
                  x.DisplayName.Contains(query) ||
                  x.Company.Contains(query));
            
            return await SortAndPageAsync(collection.Find(q), orderBy, skip, take, token);
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
            //userId = null;
            var userAuth = await GetUserAuthByUserNameAsync(digestHeaders["username"], token);
            if (userAuth == null)
                return null;

            if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, token: token);
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token: token);
            return null;
        }

        public async Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var userAuth = await GetUserAuthAsync(session, tokens, token);
            await LoadUserAuthAsync(session, userAuth, token);
        }

        private async Task LoadUserAuthAsync(IAuthSession session, IUserAuth userAuth, CancellationToken token = default)
        {
            await session.PopulateSessionAsync(userAuth, this, token);
        }

        public async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            var intUserId = int.Parse(userAuthId);
            UserAuth userAuth = (await collection.FindAsync(u => u.Id == intUserId, cancellationToken: token)).FirstOrDefault();
            return userAuth;
        }

        public async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token = default)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth) await GetUserAuthAsync(authSession.UserAuthId, token)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            await SaveUserAsync(userAuth, token);
        }

        public async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token = default)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await SaveUserAsync(userAuth, token);
        }

        public async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            await userAuthCollection.DeleteOneAsync(u => u.Id == int.Parse(userAuthId), token);

            var userAuthDetails = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            await userAuthDetails.DeleteOneAsync(u => u.UserAuthId == int.Parse(userAuthId), token);
        }

        public async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token = default)
        {
            var collection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var intUserId = int.Parse(userAuthId);
            var queryResult = await collection.FindAsync(ud => ud.UserAuthId == intUserId, cancellationToken: token);
            return (await queryResult.ToListAsync(token)).Cast<IUserAuthDetails>().ToList();
        }

        public async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthAsync(authSession.UserAuthId, token);
                if (userAuth != null) return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthByUserNameAsync(authSession.UserAuthName, token);
                if (userAuth != null) return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            var providerCollection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var oAuthProvider =
                (await providerCollection.FindAsync(ud => ud.Provider == tokens.Provider && ud.UserId == tokens.UserId,
                    cancellationToken: token)).FirstOrDefault();

            if (oAuthProvider != null)
            {
                var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
                var userAuth = (await userAuthCollection.FindAsync(u => u.Id == oAuthProvider.UserAuthId,
                        cancellationToken: token)).FirstOrDefault();
                return userAuth;
            }
            return null;
        }

        public async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            var userAuth = await GetUserAuthAsync(authSession, tokens, token) ?? new UserAuth();

            var providerCollection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var authDetails = providerCollection.Find(ud => ud.Provider == tokens.Provider && ud.UserId == tokens.UserId).FirstOrDefault() ??
                new UserAuthDetails
                {
                    Provider = tokens.Provider,
                    UserId = tokens.UserId,
                };

            authDetails.PopulateMissing(tokens);
            userAuth.PopulateMissingExtended(authDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await SaveUserAsync((UserAuth)userAuth, token);

            if (authDetails.Id == default)
                authDetails.Id = await IncUserOAuthProviderCounterAsync(token);

            authDetails.UserAuthId = userAuth.Id;

            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            await providerCollection.ReplaceOneAsync(ud => ud.Id == authDetails.Id, authDetails, 
                new ReplaceOptions {IsUpsert = true}, 
                token);

            return authDetails;
        }

        public async Task CreateMissingCollectionsAsync(CancellationToken token=default)
        {
            var collections = await (await mongoDatabase.ListCollectionsAsync(cancellationToken: token)).ToListAsync(token);
            if (!collections.Exists(document => document["name"] == UserAuthCol))
                await mongoDatabase.CreateCollectionAsync(UserAuthCol, cancellationToken: token);

            if (!collections.Exists(document => document["name"] == UserOAuthProviderCol))
                await mongoDatabase.CreateCollectionAsync(UserOAuthProviderCol, cancellationToken: token);

            if (!collections.Exists(document => document["name"] == CountersCol))
            {
                await mongoDatabase.CreateCollectionAsync(CountersCol, cancellationToken: token);
                var countersCollection = mongoDatabase.GetCollection<Counters>(CountersCol);
                Counters counters = new Counters();
                await countersCollection.InsertOneAsync(counters, cancellationToken: token);
            }
        }

        public async Task DropAndReCreateCollectionsAsync(CancellationToken token=default)
        {
            await mongoDatabase.DropCollectionAsync(UserAuthCol, token);
            await mongoDatabase.DropCollectionAsync(UserOAuthProviderCol, token);
            await mongoDatabase.DropCollectionAsync(CountersCol, token);

            await CreateMissingCollectionsAsync(token);
        }

        public async Task ClearAsync(CancellationToken token = default)
        {
            await DropAndReCreateCollectionsAsync(token);
        }

        public async Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            return await collection.CountDocumentsAsync(key => key.Id == apiKey, cancellationToken: token) > 0;
        }

        public async Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token = default)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            return (await collection.FindAsync(key => key.Id == apiKey, cancellationToken: token)).FirstOrDefault();
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token = default)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>("ApiKey");
            var queryResult = await collection.FindAsync(key => 
                key.UserAuthId == userId
                && key.CancelledDate == null
                && (key.ExpiryDate == null || key.ExpiryDate >= DateTime.UtcNow), cancellationToken: token);
            return await queryResult.ToListAsync(token);
        }

        public async Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token = default)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>("ApiKey");
            var bulkApiKeys = new List<WriteModel<ApiKey>>();
            foreach (var apiKey in apiKeys)
            {
                var apiKeyFilter = Builders<ApiKey>.Filter.Eq(key => key.Id, apiKey.Id);
                bulkApiKeys.Add(new ReplaceOneModel<ApiKey>(apiKeyFilter, apiKey) {IsUpsert = true});
            }

            if (bulkApiKeys.Any())
                await collection.BulkWriteAsync(bulkApiKeys, cancellationToken: token);
        }
    }
}
