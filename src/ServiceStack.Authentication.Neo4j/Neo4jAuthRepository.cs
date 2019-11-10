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
        private readonly IDriver driver;

        private static string UserAuthLabel => typeof(UserAuth).Name;
        private static string UserAuthDetailsLabel => typeof(UserAuthDetails).Name;
        private static string ApiKeyLabel => typeof(ApiKey).Name;

        private const string IdScopeLabel = "AuthId";
        private const string HasUserAuthDetailsRel = "HAS_USER_AUTH_DETAILS";
        private const string HasApiKeyRel = "HAS_API_KEY";

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
                tx.Run($"CREATE CONSTRAINT ON (u:{IdScopeLabel}) ASSERT u.Scope IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (userAuth:{UserAuthLabel}) ASSERT userAuth.Id IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (details:{UserAuthDetailsLabel}) ASSERT details.Id IS UNIQUE");
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
            var query = $@"
                MERGE (user:{UserAuthLabel} {{Id: $user.Id}})
                SET user = $user";

            WriteTxQuery(tx =>
            {
                if (userAuth.Id == default)
                    userAuth.Id = NextId(tx, UserAuthLabel);

                var parameters = new
                {
                    user = userAuth.ToObjectDictionary()
                };

                tx.Run(query, parameters);
            });
        }

        private int NextId(ITransaction tx, string scope)
        {
            var query = $@"
                MERGE (n:{IdScopeLabel} {{Scope: $scope}})
                SET n.Value = COALESCE(n.Value, 0) + 1
                RETURN n.Value";

            var parameters = new { scope };

            var result = tx.Run(query, parameters);

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

            var findByUsernameQuery = $@"
                MATCH (user:{UserAuthLabel} {{UserName: $name}})
                RETURN user";

            var findByEmailQuery = $@"
                MATCH (user:{UserAuthLabel} {{Email: $name}})
                RETURN user";

            var parameters = new
            {
                name = userNameOrEmail
            };

            var result = ReadQuery(isEmail ? findByEmailQuery : findByUsernameQuery, parameters)
                .SingleOrDefault();

            var userAuth = ((INode) result?[0])?.Map<UserAuth>();

            return userAuth;
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
            var query = $@"
                MATCH (user:{UserAuthLabel} {{Id: $id}})
                RETURN user";

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            var result = ReadQuery(query, parameters)
                .SingleOrDefault();

            var userAuth = ((INode)result?[0])?.Map<UserAuth>();

            return userAuth;
        }


        public void SaveUserAuth(IAuthSession authSession)
        {
            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? (UserAuth)GetUserAuth(authSession.UserAuthId)
                : authSession.ConvertTo<UserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

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
            var query = $@"
                MATCH (user:{UserAuthLabel} {{Id: $id}})
                OPTIONAL MATCH (user)-[r:{HasUserAuthDetailsRel}]->(details:{UserAuthDetailsLabel})
                DELETE user, details, r";

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            WriteQuery(query, parameters);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            var query = $@"
                MATCH (:{UserAuthLabel} {{Id: $id}})-[:{HasUserAuthDetailsRel}]->(details:{UserAuthDetailsLabel})
                RETURN details";

            var parameters = new
            {
                id = int.Parse(userAuthId)
            };

            var results = ReadQuery(query, parameters);

            var items = results.Select(
                result => ((INode) result[0]).Map<UserAuthDetails>());

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

            var query = $@"
                MATCH (details:{UserAuthDetailsLabel})
                WHERE details.Provider = $provider AND details.UserId = $userId
                WITH details
                MATCH (userAuth:{UserAuthLabel})-[:{HasUserAuthDetailsRel}]->(details:{UserAuthDetailsLabel})
                RETURN DISTINCT userAuth";

            var parameters = new
            {
                userId = tokens.UserId,
                provider = tokens.Provider
            };

            var result = ReadQuery(query, parameters)
                .SingleOrDefault();

            userAuth = ((INode)result?[0])?.Map<UserAuth>();

            return userAuth;
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var query = $@"
                MATCH (details:{UserAuthDetailsLabel})
                WHERE details.Provider = $provider AND details.UserId = $userId
                RETURN details";

            var parameters = new
            {
                userId = tokens.UserId,
                provider = tokens.Provider
            };

            var result = ReadQuery(query, parameters)
                .SingleOrDefault();

            var userAuthDetails = ((INode)result?[0])?.Map<UserAuthDetails>();

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

            var detailsQuery = $@"
                MERGE (details:{UserAuthDetailsLabel} {{Id: $details.Id}})
                SET details = $details
                WITH details
                MATCH (user:{UserAuthLabel} {{Id: $id}})
                MERGE (user)-[:{HasUserAuthDetailsRel}]->(details)";
            
            WriteTxQuery(tx =>
            {
                if (userAuthDetails.Id == default)
                    userAuthDetails.Id = NextId(tx, UserAuthDetailsLabel);

                var detailsParameters = new
                {
                    details = userAuthDetails.ToObjectDictionary(),
                    id = userAuth.Id
                };

                tx.Run(detailsQuery, detailsParameters);
            });

            return userAuthDetails;
        }

        public void Clear()
        {
            var userAuthQuery = $@"
                MATCH (userAuth:{UserAuthLabel})
                OPTIONAL MATCH (userAuth)-[r1:{HasUserAuthDetailsRel}]->(details)
                OPTIONAL MATCH (userAuth)-[r2:{HasApiKeyRel}]->(apiKey)
                DELETE userAuth, details, apiKey, r1, r2";

            var idScopeQuery = $@"
                MATCH (u:{IdScopeLabel})
                DELETE u";

            WriteTxQuery(tx =>
            {
                tx.Run(userAuthQuery);
                tx.Run(idScopeQuery);
            });
        }

        public void InitApiKeySchema()
        {
            WriteQuery($"CREATE CONSTRAINT ON (apiKey:{ApiKeyLabel}) ASSERT apiKey.Id IS UNIQUE");
        }

        public bool ApiKeyExists(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            var query = $@"
                MATCH (apiKey:{ApiKeyLabel} {{Id: $id}})
                RETURN user IS NOT NULL";

            var parameters = new
            {
                id = apiKey
            };

            var result = ReadQuery(query, parameters)
                .SingleOrDefault();

            return result?[0].As<bool>() ?? false;
        }

        public ApiKey GetApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var query = $@"
                MATCH (apiKey:{ApiKeyLabel} {{Id: $id}})
                RETURN apiKey";

            var parameters = new
            {
                id = apiKey
            };

            var result = ReadQuery(query, parameters)
                .SingleOrDefault();

            return ((INode)result?[0])?.Map<ApiKey>();
        }

        public List<ApiKey> GetUserApiKeys(string userId)
        {
            var query = $@"
                MATCH (userAuth:{UserAuthLabel} {{Id: $id}})-[:{HasApiKeyRel}]->(apiKey:{ApiKeyLabel})
                WHERE apiKey.CancelledDate Is null AND (apiKey.ExpiryDate IS null OR apiKey.ExpiryDate >= $expiry)
                RETURN apiKey";

            var parameters = new
            {
                id = int.Parse(userId),
                expiry = DateTime.UtcNow
            };

            var results = ReadQuery(query, parameters);

            var items = results.Select(
                result => ((INode)result[0]).Map<ApiKey>());

            var itemList = items.ToList();

            return itemList;
        }

        public void StoreAll(IEnumerable<ApiKey> apiKeys)
        {
            var query = $@"
                UNWIND $keys AS key
                MERGE (apiKey:{ApiKeyLabel} {{Id: key.Id}})
                SET apiKey = key
                WITH apiKey, key
                MATCH (userAuth:{UserAuthLabel} {{Id: toInteger(key.UserAuthId)}})
                MERGE (userAuth)-[:{HasApiKeyRel}]->(apiKey)";

            var parameters = new
            {
                keys = apiKeys.Select(p => p.ToObjectDictionary())
            };

            WriteQuery(query, parameters);
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
