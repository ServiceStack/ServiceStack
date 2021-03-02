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

        public MongoDbAuthRepository(IMongoDatabase mongoDatabase, bool createMissingCollections)
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
            return collections.Any(document => collectionNames.Contains(document["name"].AsString));
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
                new UpdateOptions() {IsUpsert = true});
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
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(mongoDatabase, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(mongoDatabase, newUser.Email);
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

            AssertNoExistingUser(mongoDatabase, newUser);

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
            //userId = null;
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
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            var intUserId = int.Parse(userAuthId);
            UserAuth userAuth = collection.Find(u => u.Id == intUserId).FirstOrDefault();
            return userAuth;
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth) GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
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
            var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuthCol);
            userAuthCollection.DeleteOne(u => u.Id == int.Parse(userAuthId));

            var userAuthDetails = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            userAuthDetails.DeleteOne(u => u.UserAuthId == int.Parse(userAuthId));
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            var collection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProviderCol);
            var intUserId = int.Parse(userAuthId);
            var queryResult = collection.Find(ud => ud.UserAuthId == intUserId);
            return queryResult.ToList().Cast<IUserAuthDetails>().ToList();
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
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

            providerCollection.ReplaceOne(ud => ud.Id == authDetails.Id, authDetails, new UpdateOptions() {IsUpsert = true});

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
            return collection.Count(key => key.Id == apiKey) > 0;
        }

        public ApiKey GetApiKey(string apiKey)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>(ApiKeysCol);
            return collection.Find(key => key.Id == apiKey).FirstOrDefault();
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>("ApiKey");
            var queryResult = collection.Find(key => 
            key.UserAuthId == userId
            && key.CancelledDate == null
            && (key.ExpiryDate == null || key.ExpiryDate >= DateTime.UtcNow));
            return queryResult.ToList();
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            var collection = mongoDatabase.GetCollection<ApiKey>("ApiKey");
            var bulkApiKeys = new List<WriteModel<ApiKey>>();
            foreach (var apiKey in apiKeys)
            {
                var apiKeyFilter = Builders<ApiKey>.Filter.Eq(key => key.Id, apiKey.Id);
                bulkApiKeys.Add(new ReplaceOneModel<ApiKey>(apiKeyFilter, apiKey) {IsUpsert = true});
            }

            if (bulkApiKeys.Any())
                collection.BulkWrite(bulkApiKeys);
        }

        #endregion
    }
}
