using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Auth;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ServiceStack.Authentication.MongoDb
{
    public class MongoDbAuthRepository : IUserAuthRepository, IClearable
    {
        // http://www.mongodb.org/display/DOCS/How+to+Make+an+Auto+Incrementing+Field
        class Counters
        {
            public int Id { get; set; }
            public int UserAuthCounter { get; set; }
            public int UserOAuthProviderCounter { get; set; }
        }

        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly MongoDatabase mongoDatabase;

        // UserAuth collection name
        private static string UserAuth_Col
        {
            get
            {
                return typeof(UserAuth).Name;
            }
        }
        // UserOAuthProvider collection name
        private static string UserOAuthProvider_Col
        {
            get
            {
                return typeof(UserAuthDetails).Name;
            }
        }
        // Counters collection name
        private static string Counters_Col
        {
            get
            {
                return typeof(Counters).Name;
            }
        }

        public MongoDbAuthRepository(MongoDatabase mongoDatabase, bool createMissingCollections)
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
            return (mongoDatabase.CollectionExists(UserAuth_Col))
                    && (mongoDatabase.CollectionExists(UserOAuthProvider_Col))
                    && (mongoDatabase.CollectionExists(Counters_Col));

        }

        public void CreateMissingCollections()
        {
            if (!mongoDatabase.CollectionExists(UserAuth_Col))
                mongoDatabase.CreateCollection(UserAuth_Col);

            if (!mongoDatabase.CollectionExists(UserOAuthProvider_Col))
                mongoDatabase.CreateCollection(UserOAuthProvider_Col);

            if (!mongoDatabase.CollectionExists(Counters_Col))
            {
                mongoDatabase.CreateCollection(Counters_Col);

                var CountersCollection = mongoDatabase.GetCollection<Counters>(Counters_Col);
                Counters counters = new Counters();
                CountersCollection.Save(counters);
            }
        }

        public void DropAndReCreateCollections()
        {
            if (mongoDatabase.CollectionExists(UserAuth_Col))
                mongoDatabase.DropCollection(UserAuth_Col);

            if (mongoDatabase.CollectionExists(UserOAuthProvider_Col))
                mongoDatabase.DropCollection(UserOAuthProvider_Col);

            if (mongoDatabase.CollectionExists(Counters_Col))
                mongoDatabase.DropCollection(Counters_Col);

            CreateMissingCollections();
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException("UserName or Email is required");

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
            }
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(mongoDatabase, newUser);

            var saltedHash = HostContext.Resolve<IHashProvider>();
            string salt;
            string hash;
            saltedHash.GetHashAndSaltString(password, out hash, out salt);
            var digestHelper = new DigestAuthFunctions();
            newUser.DigestHa1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            SaveUser(newUser);
            return newUser;
        }

        private void SaveUser(IUserAuth userAuth)
        {
            if (userAuth.Id == default(int))
                userAuth.Id = IncUserAuthCounter();
            var usersCollection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);
            usersCollection.Save(userAuth);
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
            var CountersCollection = mongoDatabase.GetCollection<Counters>(Counters_Col);
            var args = new FindAndModifyArgs() { Query = Query.Null, SortBy = SortBy.Null, Update = Update.Inc(counterName, 1), Upsert = true };
            FindAndModifyResult counterIncResult = CountersCollection.FindAndModify(args);
            Counters updatedCounters = counterIncResult.GetModifiedDocumentAs<Counters>();
            return updatedCounters;
        }

        private static void AssertNoExistingUser(MongoDatabase mongoDatabase, IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(mongoDatabase, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(mongoDatabase, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
            }
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(mongoDatabase, newUser, existingUser);

            var hash = existingUser.PasswordHash;
            var salt = existingUser.Salt;
            if (password != null)
            {
                var saltedHash = HostContext.Resolve<IHashProvider>();
                saltedHash.GetHashAndSaltString(password, out hash, out salt);
            }
            // If either one changes the digest hash has to be recalculated
            var digestHash = existingUser.DigestHa1Hash;
            if (password != null || existingUser.UserName != newUser.UserName)
            {
                var digestHelper = new DigestAuthFunctions();
                digestHash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            }
            newUser.Id = existingUser.Id;
            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.DigestHa1Hash = digestHash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            SaveUser(newUser);

            return newUser;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            return GetUserAuthByUserName(mongoDatabase, userNameOrEmail);
        }

        private static UserAuth GetUserAuthByUserName(MongoDatabase mongoDatabase, string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            var isEmail = userNameOrEmail.Contains("@");
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);

            IMongoQuery query = isEmail
                ? Query.EQ("Email", userNameOrEmail)
                : Query.EQ("UserName", userNameOrEmail);

            UserAuth userAuth = collection.FindOne(query);
            return userAuth;
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) return false;

            var saltedHash = HostContext.Resolve<IHashProvider>();
            if (saltedHash.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) return false;

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, PrivateKey, NonceTimeOut, userAuth.DigestHa1Hash, sequence))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            userAuth = null;
            return false;
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, IUserAuth userAuth)
        {
            session.PopulateSession(userAuth,
                GetUserAuthDetails(session.UserAuthId).ConvertAll(x => (IAuthTokens)x));
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);
            UserAuth userAuth = collection.FindOneById(int.Parse(userAuthId));
            return userAuth;
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth)GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            var collection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);
            SaveUser(userAuth);
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser(userAuth);
        }

        public void DeleteUserAuth(string userAuthId)
        {
            var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);
            userAuthCollection.Remove(Query.EQ("_id", int.Parse(userAuthId)));
            
            var query = Query.EQ("UserAuthId", int.Parse(userAuthId));
            var userAuthDetails = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProvider_Col);
            userAuthDetails.Remove(query);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            IMongoQuery query = Query.EQ("UserAuthId", int.Parse(userAuthId));

            var collection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProvider_Col);
            MongoCursor<UserAuthDetails> queryResult = collection.Find(query);
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

            var query = Query.And(
                            Query.EQ("Provider", tokens.Provider),
                            Query.EQ("UserId", tokens.UserId)
                        );

            var providerCollection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProvider_Col);
            var oAuthProvider = providerCollection.FindOne(query);


            if (oAuthProvider != null)
            {
                var userAuthCollection = mongoDatabase.GetCollection<UserAuth>(UserAuth_Col);
                var userAuth = userAuthCollection.FindOneById(oAuthProvider.UserAuthId);
                return userAuth;
            }
            return null;
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();

            var query = Query.And(
                            Query.EQ("Provider", tokens.Provider),
                            Query.EQ("UserId", tokens.UserId)
                        );
            var providerCollection = mongoDatabase.GetCollection<UserAuthDetails>(UserOAuthProvider_Col);
            var authDetails = providerCollection.FindOne(query);

            if (authDetails == null)
            {
                authDetails = new UserAuthDetails
                {
                    Provider = tokens.Provider,
                    UserId = tokens.UserId,
                };
            }

            authDetails.PopulateMissing(tokens);
            userAuth.PopulateMissingExtended(authDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser((UserAuth)userAuth);

            if (authDetails.Id == default(int))
                authDetails.Id = IncUserOAuthProviderCounter();

            authDetails.UserAuthId = userAuth.Id;

            if (authDetails.CreatedDate == default(DateTime))
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            providerCollection.Save(authDetails);

            return authDetails;
        }

        public void Clear()
        {
            DropAndReCreateCollections();
        }
    }
}
