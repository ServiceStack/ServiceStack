using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using ServiceStack.Auth;
using MongoDB.Driver;

namespace ServiceStack.Authentication.MongoDb
{
    public partial class MongoDbAuthRepository : IUserAuthRepository, IClearable, IManageApiKeys, IQueryUserAuth
    {
        // http://www.mongodb.org/display/DOCS/How+to+Make+an+Auto+Incrementing+Field
        class Counters
        {
            public int Id { get; set; }
            public int UserAuthCounter { get; set; }
            public int UserOAuthProviderCounter { get; set; }
        }

        private readonly IMongoDatabase mongoDatabase;

        // UserAuth collection name
        private static string UserAuthCol => nameof(UserAuth);
        // UserOAuthProvider collection name
        private static string UserOAuthProviderCol => nameof(UserAuthDetails);
        // Counters collection name
        private static string CountersCol => nameof(Counters);
        // ApiKey collection name
        private static string ApiKeysCol => nameof(ApiKey);

        public MongoDbAuthRepository(IMongoDatabase mongoDatabase, bool createMissingCollections = false)
        {
            this.mongoDatabase = mongoDatabase;

            if (createMissingCollections)
            {
                CreateMissingCollections();
            }

            if (!CollectionsExists())
            {
                throw new InvalidOperationException("One of the collections needed by MongoDBAuthRepository is missing." +
                                                    "You can call MongoDBAuthRepository constructor with the parameter CreateMissingCollections set to 'true'  " +
                                                    "to create the needed collections.");
            }
        }
        public bool CollectionsExists()
        {
            var collectionNames = new List<string>()
            {
                UserAuthCol,
                UserOAuthProviderCol,
                CountersCol
            };

            var collections = mongoDatabase.ListCollections().ToList();
            return collectionNames.TrueForAll(name => collections.Exists(document => document["name"] == name));
        }

        public void CreateMissingCollections()
        {
            var collections = mongoDatabase.ListCollections().ToList();
            if (!collections.Exists(document => document["name"] == UserAuthCol))
                mongoDatabase.CreateCollection(UserAuthCol);

            if (!collections.Exists(document => document["name"] == UserOAuthProviderCol))
                mongoDatabase.CreateCollection(UserOAuthProviderCol);

            if (!collections.Exists(document => document["name"] == CountersCol))
            {
                mongoDatabase.CreateCollection(CountersCol);
                var countersCollection = mongoDatabase.GetCollection<Counters>(CountersCol);
                Counters counters = new Counters();
                countersCollection.InsertOne(counters);
            }
        }

        public void DropAndReCreateCollections()
        {
            mongoDatabase.DropCollection(UserAuthCol);
            mongoDatabase.DropCollection(UserOAuthProviderCol);
            mongoDatabase.DropCollection(CountersCol);

            CreateMissingCollections();
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(mongoDatabase, newUser);

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            SaveUser(newUser);
            return newUser;
        }

        private void SaveUser(IUserAuth userAuth)
        {
            if (userAuth.Id == default)
                userAuth.Id = IncUserAuthCounter();
            var usersCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            usersCollection.ReplaceOne(u => u.Id == userAuth.Id, (UserAuth)userAuth, 
                new ReplaceOptions { IsUpsert = true });
        }

        private int IncUserAuthCounter()
        {
            return IncCounter("UserAuthCounter").UserAuthCounter;
        }

        private int IncUserOAuthProviderCounter()
        {
            return IncCounter("UserOAuthProviderCounter").UserOAuthProviderCounter;
        }

        private Counters IncCounter(string counterName)
        {
            var countersCollection = mongoDatabase.GetCollection<Counters>(CountersCol);
            var update = Builders<Counters>.Update.Inc(counterName, 1);
            var updatedCounters = countersCollection.FindOneAndUpdate(new BsonDocument(), update, 
                new FindOneAndUpdateOptions<Counters> { IsUpsert = true, ReturnDocument = ReturnDocument.After});
            return updatedCounters;
        }

        private static void AssertNoExistingUser(IMongoDatabase mongoDatabase, IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            if (newUser.UserName != null)
            {
                var existingUser = collection.Find(u => u.UserName == newUser.UserName).FirstOrDefault();
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = collection.Find(u => u.Email == newUser.Email).FirstOrDefault();
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
            }
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(mongoDatabase, newUser, existingUser);

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            SaveUser(newUser);

            return newUser;
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            newUser.ValidateNewUser();

            AssertNoExistingUser(mongoDatabase, newUser, existingUser);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            SaveUser(newUser);

            return newUser;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            return GetUserAuthByUserName(mongoDatabase, userNameOrEmail);
        }

        private static UserAuth GetUserAuthByUserName(IMongoDatabase mongoDatabase, string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            var isEmail = userNameOrEmail.Contains("@");
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);

            var builder = Builders<UserAuth>.Filter;
            var query = isEmail
                ? builder.Eq(auth => auth.Email, userNameOrEmail)
                : builder.Eq(auth => auth.UserName, userNameOrEmail);

            var userAuth = collection.Find(query).FirstOrDefault();
            return userAuth;
        }

        private static List<IUserAuth> SortAndPage(IFindFluent<UserAuth, UserAuth> q, string orderBy, int? skip, int? take)
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

            return q.ToList().ConvertAll(x => x as IUserAuth);
        }

        public List<IUserAuth> GetUserAuths(string orderBy = null, int? skip = null, int? take = null)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            return SortAndPage(collection.Find(Builders<UserAuth>.Filter.Empty), orderBy, skip, take);
        }

        public List<IUserAuth> SearchUserAuths(string query, string orderBy = null, int? skip = null, int? take = null)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            var filter = Builders<UserAuth>.Filter;
            var q = filter.Where(x => x.UserName.Contains(query) ||
                  x.Email.Contains(query) ||
                  x.DisplayName.Contains(query) ||
                  x.Company.Contains(query));
            
            return SortAndPage(collection.Find(q), orderBy, skip, take);
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
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

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            userAuth = null;
            if (digestHeaders == null || !digestHeaders.TryGetValue("username", out var username))
                return false;

            userAuth = GetUserAuthByUserName(username);
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

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
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

        public IUserAuth GetUserAuth(string userAuthId)
        {
            if (!int.TryParse(userAuthId, out var intUserId))
                return null;

            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            UserAuth userAuth = collection.Find(u => u.Id == intUserId).FirstOrDefault();
            return userAuth;
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            if (authSession == null)
                throw new ArgumentNullException(nameof(authSession));

            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth) GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default && int.TryParse(authSession.UserAuthId, out var parsedId))
                userAuth.Id = parsedId;

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser(userAuth);
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser(userAuth);
        }

        public void DeleteUserAuth(string userAuthId)
        {
            if (!int.TryParse(userAuthId, out var intUserId))
                return;

            var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            userAuthCollection.DeleteOne(u => u.Id == intUserId);

            var userAuthDetails = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            userAuthDetails.DeleteMany(u => u.UserAuthId == intUserId);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            if (!int.TryParse(userAuthId, out var intUserId))
                return new List<IUserAuthDetails>();

            var collection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var queryResult = collection.Find(ud => ud.UserAuthId == intUserId);
            return queryResult.ToList().Cast<IUserAuthDetails>().ToList();
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (authSession != null)
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
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            var providerCollection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var oAuthProvider = providerCollection.Find(ud => ud.Provider == tokens.Provider && ud.UserId == tokens.UserId).FirstOrDefault();

            if (oAuthProvider != null)
            {
                var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
                var userAuth = userAuthCollection.Find(u => u.Id == oAuthProvider.UserAuthId).FirstOrDefault();
                return userAuth;
            }
            return null;
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();

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

            SaveUser((UserAuth)userAuth);

            if (authDetails.Id == default)
                authDetails.Id = IncUserOAuthProviderCounter();

            authDetails.UserAuthId = userAuth.Id;

            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            providerCollection.ReplaceOne(ud => ud.Id == authDetails.Id, authDetails, new ReplaceOptions { IsUpsert = true });

            return authDetails;
        }

        public void Clear()
        {
            DropAndReCreateCollections();
        }

        #region IManageApiKeys

        public void InitApiKeySchema()
        {
            var collections = mongoDatabase.ListCollections().ToList();
            if (!collections.Exists(document => document["name"] == ApiKeysCol))
                mongoDatabase.CreateCollection(ApiKeysCol);
        }

        public bool ApiKeyExists(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            return collection.CountDocuments(key => key.Id == apiKey) > 0;
        }

        public ApiKey GetApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            return collection.Find(key => key.Id == apiKey).FirstOrDefault();
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<ApiKey>();
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            var queryResult = collection.Find(key => 
                key.UserAuthId == userId
                && key.CancelledDate == null
                && (key.ExpiryDate == null || key.ExpiryDate >= DateTime.UtcNow));
            return queryResult.ToList();
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            if (apiKeys == null)
                return;
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            var bulkApiKeys = new List<WriteModel<ApiKey>>();
            foreach (var apiKey in apiKeys)
            {
                if (apiKey == null || apiKey.Id == null)
                    continue;
                var apiKeyFilter = Builders<ApiKey>.Filter.Eq(key => key.Id, apiKey.Id);
                bulkApiKeys.Add(new ReplaceOneModel<ApiKey>(apiKeyFilter, apiKey) {IsUpsert = true});
            }

            if (bulkApiKeys.Count > 0)
                collection.BulkWrite(bulkApiKeys);
        }

        #endregion
    }
}
