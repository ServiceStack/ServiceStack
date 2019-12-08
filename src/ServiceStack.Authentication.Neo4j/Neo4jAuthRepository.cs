using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.Neo4j
{
    // ReSharper disable once InconsistentNaming
    public class Neo4jAuthRepository : Neo4jAuthRepository<UserAuth, UserAuthDetails>
    {
        public Neo4jAuthRepository(IDriver driver) : base(driver) { }
    }
    
    // ReSharper disable once InconsistentNaming
    public class Neo4jAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IClearable, IRequiresSchema, IManageApiKeys
        where TUserAuth : class, IUserAuth, new()
        where TUserAuthDetails : class, IUserAuthDetails, new()
    {
        private static class Label
        {
            public const string AuthIdSeq = nameof(AuthIdSeq);    
            public static string UserAuth => typeof(UserAuth).Name;
            public static string UserAuthDetails => typeof(UserAuthDetails).Name;
            public static string ApiKey => typeof(ApiKey).Name;
        }

        private static class Rel
        {
            public const string HasUserAuthDetails = "HAS_USER_AUTH_DETAILS";
            public const string HasApiKey = "HAS_API_KEY";
        }

        private static class Query
        {
            public static string IdScopeConstraint => $@"
                CREATE CONSTRAINT ON (seq:{Label.AuthIdSeq}) ASSERT seq.Scope IS UNIQUE";

            public static string UserAuthConstraint => $@"
                CREATE CONSTRAINT ON (userAuth:{Label.UserAuth}) ASSERT userAuth.Id IS UNIQUE";

            public static string UserAuthDetailsConstraint => $@"
                CREATE CONSTRAINT ON (details:{Label.UserAuthDetails}) ASSERT details.Id IS UNIQUE";

            public static string ApiKeyConstraint => $@"
                CREATE CONSTRAINT ON (apiKey:{Label.ApiKey}) ASSERT apiKey.Id IS UNIQUE";

            public static string NextSequence => $@"
                MERGE (seq:{Label.AuthIdSeq} {{Scope: $scope}})
                SET seq.Value = COALESCE(seq.Value, 0) + 1
                RETURN seq.Value";

            public static string DeleteAllSequence => $@"
                MATCH (seq:{Label.AuthIdSeq})
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

            InitMappers();
        }

        public void InitSchema()
        {
            driver.WriteTxQuery(tx =>
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
            driver.WriteTxQuery(tx =>
            {
                if (userAuth.Id == default)
                    userAuth.Id = NextSequence(tx, Label.UserAuth);

                var parameters = new
                {
                    user = userAuth.ConvertTo<Dictionary<string, object>>()
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

            var result = driver.ReadQuery(isEmail ? Query.UserAuthByEmail : Query.UserAuthByName, parameters);

            return result.Map<TUserAuth>().SingleOrDefault();
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
            TryConvertToInteger(userAuthId, "userAuthId", out var idVal);

            var parameters = new
            {
                id = idVal
            };

            var result = driver.ReadQuery(Query.UserAuthById, parameters);

            return result.Map<TUserAuth>().SingleOrDefault();
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (TUserAuth)GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
            {
                TryConvertToInteger(authSession.UserAuthId, "authSession.UserAuthId", out var idVal);

                userAuth.Id = idVal;
            }

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser(userAuth);

            if (authSession.UserAuthId.IsNullOrEmpty())
            {
                authSession.UserAuthId = userAuth.Id.ToString();
            }
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
            TryConvertToInteger(userAuthId, "userAuthId", out var idVal);

            var parameters = new
            {
                id = idVal
            };

            driver.WriteQuery(Query.DeleteUserAuth, parameters);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            TryConvertToInteger(userAuthId, "userAuthId", out var idVal);

            var parameters = new
            {
                id = idVal
            };

            var results = driver.ReadQuery(Query.UserAuthDetailsById, parameters);

            var items = results.Map<TUserAuthDetails>();

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

            var result = driver.ReadQuery(Query.UserAuthByProviderAndUserId, parameters);

            return result.Map<TUserAuth>().SingleOrDefault();
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var parameters = new
            {
                userId = tokens.UserId,
                provider = tokens.Provider
            };

            var result = driver.ReadQuery(Query.UserAuthDetailsByProviderAndUserId, parameters);

            var userAuthDetails = result.Map<TUserAuthDetails>().SingleOrDefault() ?? new TUserAuthDetails
            {
                Provider = tokens.Provider,
                UserId = tokens.UserId,
            };

            userAuthDetails.PopulateMissing(tokens);
            
            var userAuth = GetUserAuth(authSession, tokens) ?? new TUserAuth();
            userAuth.PopulateMissingExtended(userAuthDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            SaveUser((TUserAuth)userAuth);

            userAuthDetails.UserAuthId = userAuth.Id;
            
            if (userAuthDetails.CreatedDate == default)
                userAuthDetails.CreatedDate = userAuth.ModifiedDate;
            userAuthDetails.ModifiedDate = userAuth.ModifiedDate;

            driver.WriteTxQuery(tx =>
            {
                if (userAuthDetails.Id == default)
                    userAuthDetails.Id = NextSequence(tx, Label.UserAuthDetails);

                var detailsParameters = new
                {
                    details = userAuthDetails.ConvertTo<Dictionary<string, object>>(),
                    id = userAuth.Id
                };

                tx.Run(Query.CreateOrUpdateUserAuthDetails, detailsParameters);
            });

            return userAuthDetails;
        }

        public void Clear()
        {
            driver.WriteTxQuery(tx =>
            {
                tx.Run(Query.DeleteAllUserAuth);
                tx.Run(Query.DeleteAllSequence);
            });
        }

        public void InitApiKeySchema()
        {
            driver.WriteQuery(Query.ApiKeyConstraint);
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

            var result = driver.ReadQuery(Query.ApiKeyById, parameters);

            return result.Map<ApiKey>().SingleOrDefault();
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            TryConvertToInteger(userId, "userId", out var idVal);

            var parameters = new
            {
                id = idVal,
                expiry = DateTime.UtcNow
            };

            var results = driver.ReadQuery(Query.ActiveApiKeysByUserAuthId, parameters);

            return results.Map<ApiKey>().ToList();
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            var parameters = new
            {
                keys = apiKeys.Select(p => p.ToObjectDictionary())
            };

            driver.WriteQuery(Query.UpdateApiKeys, parameters);
        }
        
        private static void InitMappers()
        {
            AutoMapping.RegisterConverter<ZonedDateTime, DateTime>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
            AutoMapping.RegisterConverter<ZonedDateTime, DateTime?>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
            
            AutoMapping.RegisterConverter<TUserAuth, Dictionary<string, object>>(userAuth =>
            {
                var dictionary = userAuth.ToObjectDictionary();
                dictionary[nameof(UserAuth.Meta)] = userAuth.Meta.ToJsv();
                dictionary[nameof(UserAuth.Roles)] = userAuth.Roles.ToJsv();
                dictionary[nameof(UserAuth.Permissions)] = userAuth.Permissions.ToJsv();
                return dictionary;
            });

            AutoMapping.RegisterConverter<TUserAuthDetails, Dictionary<string, object>>(userAuthDetails =>
            {
                var dictionary = userAuthDetails.ToObjectDictionary();
                dictionary[nameof(UserAuthDetails.Items)] = userAuthDetails.Items.ToJsv();
                dictionary[nameof(UserAuthDetails.Meta)] = userAuthDetails.Meta.ToJsv();
                return dictionary;
            });
        }
        
        private static void TryConvertToInteger(string strValue, string varName, out int result)
        {
            if (!int.TryParse(strValue, out result))
                throw new ArgumentException(@"Cannot convert to integer", varName ?? "string");
        }
    }

    internal static class DriverExtensions
    {
        public static IStatementResult ReadQuery(this IDriver driver, string statement, object parameters = null)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(tx => tx.Run(statement, parameters));
            }
        }
        
        public static void WriteQuery(this IDriver driver, string statement, object parameters = null)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(tx => tx.Run(statement, parameters));
            }
        }
        
        public static void WriteTxQuery(this IDriver driver, Action<ITransaction> action)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(action);
            }
        }
    }
    
    internal static class RecordExtensions
    {
        public static IEnumerable<TReturn> Map<TReturn>(
            this IEnumerable<IRecord> records)
        {
            return records.Select(record => record.Map<TReturn>());
        }

        public static TReturn Map<TReturn>(this IRecord record)
        {
            return ((IEntity) record[0]).Properties.FromObjectDictionary<TReturn>();
        }
    }
}
