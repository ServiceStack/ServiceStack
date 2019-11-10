using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Neo4j.Driver.V1;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.Neo4j
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Neo4jAuthRepository : IUserAuthRepository, IClearable, IRequiresSchema, IManageApiKeys
    {
        internal static class Label
        {
            public const string IdScope = "AuthId";    
            public static string UserAuth => typeof(UserAuth).Name;
            public static string UserAuthDetails => typeof(UserAuthDetails).Name;
            public static string ApiKey => typeof(ApiKey).Name;
        }

        internal static class Rel
        {
            public const string HasUserAuthDetails = "HAS_USER_AUTH_DETAILS";
            public const string HasApiKey = "HAS_API_KEY";
        }

        internal static class Query
        {
            public static string IdScopeConstraint => $@"
                CREATE CONSTRAINT ON (u:{Label.IdScope}) ASSERT u.Scope IS UNIQUE";

            public static string UserAuthConstraint => $@"
                CREATE CONSTRAINT ON (userAuth:{Label.UserAuth}) ASSERT userAuth.Id IS UNIQUE";

            public static string UserAuthDetailsConstraint => $@"
                CREATE CONSTRAINT ON (details:{Label.UserAuthDetails}) ASSERT details.Id IS UNIQUE";

            public static string ApiKeyConstraint => $@"
                CREATE CONSTRAINT ON (apiKey:{Label.ApiKey}) ASSERT apiKey.Id IS UNIQUE";

            public static string NextSequence => $@"
                MERGE (seq:{Label.IdScope} {{Scope: $scope}})
                SET seq.Value = COALESCE(seq.Value, 0) + 1
                RETURN seq.Value";

            public static string DeleteAllSequence => $@"
                MATCH (seq:{Label.IdScope})
                DELETE seq";

            public static string CreateOrUpdateUserAuth => $@"
                MERGE (user:{Label.UserAuth} {{Id: $user.Id}})
                SET user = $user";

            public static string UserAuthById => $@"
                MATCH (user:{Label.UserAuth} {{Id: $id}})
                RETURN user";

            public static string UserAuthByName => $@"
                MATCH (user:{Label.UserAuth} {{UserName: $name}})
                RETURN user";

            public static string UserAuthByEmail => $@"
                MATCH (user:{Label.UserAuth} {{Email: $name}})
                RETURN user";

            public static string UserAuthDetailsById => $@"
                MATCH (:{Label.UserAuth} {{Id: $id}})-[:{Rel.HasUserAuthDetails}]->(details:{Label.UserAuthDetails})
                RETURN details";

            public static string DeleteUserAuth => $@"
                MATCH (user:{Label.UserAuth} {{Id: $id}})
                OPTIONAL MATCH (user)-[rDetails:{Rel.HasUserAuthDetails}]->(details:{Label.UserAuthDetails})
                OPTIONAL MATCH (user)-[rApiKey:{Rel.HasApiKey}]->(apiKey:{Label.ApiKey})
                DELETE user, details, apiKey, rDetails, rApiKey";

            public static string DeleteAllUserAuth => $@"
                MATCH (user:{Label.UserAuth})
                OPTIONAL MATCH (user)-[rDetails:{Rel.HasUserAuthDetails}]->(details:{Label.UserAuthDetails})
                OPTIONAL MATCH (user)-[rApiKey:{Rel.HasApiKey}]->(apiKey:{Label.ApiKey})
                DELETE user, details, apiKey, rDetails, rApiKey";

            public static string UserAuthDetailsByProviderAndUserId => $@"
                MATCH (details:{Label.UserAuthDetails})
                WHERE details.Provider = $provider AND details.UserId = $userId
                RETURN details";

            public static string UserAuthByProviderAndUserId => $@"
                MATCH (details:{Label.UserAuthDetails})
                WHERE details.Provider = $provider AND details.UserId = $userId
                WITH details
                MATCH (userAuth:{Label.UserAuth})-[:{Rel.HasUserAuthDetails}]->(details:{Label.UserAuthDetails})
                RETURN DISTINCT userAuth";

            public static string CreateOrUpdateUserAuthDetails => $@"
                MERGE (details:{Label.UserAuthDetails} {{Id: $details.Id}})
                SET details = $details
                WITH details
                MATCH (user:{Label.UserAuth} {{Id: $id}})
                MERGE (user)-[:{Rel.HasUserAuthDetails}]->(details)";

            public static string ApiKeyById => $@"
                MATCH (apiKey:{Label.ApiKey} {{Id: $id}})
                RETURN apiKey";

            public static string ActiveApiKeysByUserAuthId => $@"
                MATCH (userAuth:{Label.UserAuth} {{Id: $id}})-[:{Rel.HasApiKey}]->(apiKey:{Label.ApiKey})
                WHERE apiKey.CancelledDate Is null AND (apiKey.ExpiryDate IS null OR apiKey.ExpiryDate >= $expiry)
                RETURN apiKey";

            public static string UpdateApiKeys => $@"
                UNWIND $keys AS key
                MERGE (apiKey:{Label.ApiKey} {{Id: key.Id}})
                SET apiKey = key
                WITH apiKey, key
                MATCH (userAuth:{Label.UserAuth} {{Id: toInteger(key.UserAuthId)}})
                MERGE (userAuth)-[:{Rel.HasApiKey}]->(apiKey)";
        }

        private readonly IDriver driver;
        
        public Neo4jAuthRepository(IDriver driver)
        {
            this.driver = driver;

            AutoMapping.RegisterConverter<ZonedDateTime, DateTime>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
            AutoMapping.RegisterConverter<ZonedDateTime, DateTime?>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
        }

        public void InitSchema()
        {
            WriteTxQuery(tx =>
            {
                tx.Run(Query.IdScopeConstraint);
                tx.Run(Query.UserAuthConstraint);
                tx.Run(Query.UserAuthDetailsConstraint);
            });
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(newUser);

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            SaveUser(newUser);
            return newUser;
        }

        private void SaveUser(IUserAuth userAuth)
        {
            WriteTxQuery(tx =>
            {
                if (userAuth.Id == default)
                    userAuth.Id = NextSequence(tx, Label.UserAuth);

                var parameters = new
                {
                    user = userAuth.ToObjectDictionary()
                };

                tx.Run(Query.CreateOrUpdateUserAuth, parameters);
            });
        }

        private int NextSequence(ITransaction tx, string scope)
        {
            var parameters = new { scope };

            var result = tx.Run(Query.NextSequence, parameters);

            var record = result.Single();
            return record[0].As<int>();
        }

        private void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            IUserAuth existingUser;
            if (newUser.UserName != null)
            {
                existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }

            if (newUser.Email == null) return;
            
            existingUser = GetUserAuthByUserName(newUser.Email);
            if (existingUser != null
                && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(newUser, existingUser);

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

            AssertNoExistingUser(newUser);

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
            if (userNameOrEmail == null)
                return null;

            var isEmail = userNameOrEmail.Contains("@");

            var parameters = new
            {
                name = userNameOrEmail
            };

            var result = ReadQuery(isEmail ? Query.UserAuthByEmail : Query.UserAuthByName, parameters);

            return result.Map<UserAuth>().SingleOrDefault();
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
            userAuthId.ThrowIfNotConvertibleToInteger("userAuthId");

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            var result = ReadQuery(Query.UserAuthById, parameters);

            return result.Map<UserAuth>().SingleOrDefault();
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth)GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
            {
                authSession.UserAuthId.ThrowIfNotConvertibleToInteger("userAuthId");
                
                userAuth.Id = int.Parse(authSession.UserAuthId);
            }

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
            userAuthId.ThrowIfNotConvertibleToInteger("userAuthId");

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            WriteQuery(Query.DeleteUserAuth, parameters);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            userAuthId.ThrowIfNotConvertibleToInteger("userAuthId");

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            var results = ReadQuery(Query.UserAuthDetailsById, parameters);

            var items = results.Map<UserAuthDetails>();

            return items.Cast<IUserAuthDetails>().ToList();
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            IUserAuth userAuth;
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null) return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null) return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            var parameters = new
            {
                userId = tokens.UserId,
                provider = tokens.Provider
            };

            var result = ReadQuery(Query.UserAuthByProviderAndUserId, parameters);

            return result.Map<UserAuth>().SingleOrDefault();
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var parameters = new
            {
                userId = tokens.UserId,
                provider = tokens.Provider
            };

            var result = ReadQuery(Query.UserAuthDetailsByProviderAndUserId, parameters);

            var userAuthDetails = result.Map<UserAuthDetails>().SingleOrDefault();

            if (userAuthDetails == null)
            {
                userAuthDetails = new UserAuthDetails
                {
                    Provider = tokens.Provider,
                    UserId = tokens.UserId,
                };
            }

            userAuthDetails.PopulateMissing(tokens);
            
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();
            userAuth.PopulateMissingExtended(userAuthDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser((UserAuth)userAuth);

            userAuthDetails.UserAuthId = userAuth.Id;
            
            if (userAuthDetails.CreatedDate == default)
                userAuthDetails.CreatedDate = userAuth.ModifiedDate;
            userAuthDetails.ModifiedDate = userAuth.ModifiedDate;

            WriteTxQuery(tx =>
            {
                if (userAuthDetails.Id == default)
                    userAuthDetails.Id = NextSequence(tx, Label.UserAuthDetails);

                var detailsParameters = new
                {
                    details = userAuthDetails.ToObjectDictionary(),
                    id = userAuth.Id
                };

                tx.Run(Query.CreateOrUpdateUserAuthDetails, detailsParameters);
            });

            return userAuthDetails;
        }

        public void Clear()
        {
            WriteTxQuery(tx =>
            {
                tx.Run(Query.DeleteAllUserAuth);
                tx.Run(Query.DeleteAllSequence);
            });
        }

        public void InitApiKeySchema()
        {
            WriteQuery(Query.ApiKeyConstraint);
        }

        public bool ApiKeyExists(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            return GetApiKey(apiKey) != null;
        }

        public ApiKey GetApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var parameters = new
            {
                id = apiKey
            };

            var result = ReadQuery(Query.ApiKeyById, parameters);

            return result.Map<ApiKey>().SingleOrDefault();
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            userId.ThrowIfNotConvertibleToInteger("userId");

            var parameters = new
            {
                id = int.Parse(userId),
                expiry = DateTime.UtcNow
            };

            var results = ReadQuery(Query.ActiveApiKeysByUserAuthId, parameters);

            return results.Map<ApiKey>().ToList();
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            var parameters = new
            {
                keys = apiKeys.Select(p => p.ToObjectDictionary())
            };

            WriteQuery(Query.UpdateApiKeys, parameters);
        }

        private IStatementResult ReadQuery(string statement, object parameters = null)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(tx => tx.Run(statement, parameters));
            }
        }
        
        private void WriteQuery(string statement, object parameters = null)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(tx => tx.Run(statement, parameters));
            }
        }
        
        private void WriteTxQuery(Action<ITransaction> action)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(action);
            }
        }
    }
}
