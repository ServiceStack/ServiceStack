using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using ServiceStack.Auth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Authentication.RavenDb
{
    public partial class RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IQueryUserAuth, ICustomUserAuth
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        readonly IDocumentStore documentStore;
        public static bool IsInitialized { get; private set; } = false;

        static TypeProperties UserAuthProps = TypeProperties<TUserAuth>.Instance;
        static TypeProperties UserAuthDetailsProps = TypeProperties<TUserAuthDetails>.Instance;
        static string UserAuthCollectionName { get; set; }

        static RavenDbUserAuthRepository()
        {
            var typeName = typeof(TUserAuth).Name;
            UserAuthCollectionName = typeName.ToLower().EndsWith("s") ? typeName : typeName + "s";
        }

        public string UserAuthIdentifier { get; set; } = nameof(RavenUserAuth.Key);
        public string UserAuthDetailsIdentifier { get; set; } = nameof(RavenUserAuthDetails.Key);
        public static void CreateOrUpdateUserAuthIndex(IDocumentStore store)
        {
            new UserAuth_By_UserNameOrEmail().Execute(store);
            new UserAuth_By_UserAuthDetails().Execute(store);
            IsInitialized = true;
        }

        private PropertyAccessor userAuthKeyProp;
        private PropertyAccessor UserAuthKeyProp => userAuthKeyProp
            ??= UserAuthProps.GetAccessor(UserAuthIdentifier)
            ?? throw new NotSupportedException($"{typeof(TUserAuth).Name} does not contain '{UserAuthIdentifier}' property, add property or specify alternate Raven Identifier in UserAuthIdentifier");


        private PropertyAccessor userAuthDetailsKeyProp;
        private PropertyAccessor UserAuthDetailsKeyProp => userAuthDetailsKeyProp
            ??= UserAuthDetailsProps.GetAccessor(UserAuthDetailsIdentifier)
            ?? throw new NotSupportedException(typeof(TUserAuthDetails).Name +
                $" does not contain '{UserAuthDetailsIdentifier}' property, add property or specify alternate Raven Identifier in UserAuthDetailsIdentifier");

        public RavenDbUserAuthRepository(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;

            EnsureThatUniqueIndexesAreCreated(documentStore);

            RegisterPopulator();
        }

        static void EnsureThatUniqueIndexesAreCreated(IDocumentStore documentStore)
        {
            if (!IsInitialized)
                CreateOrUpdateUserAuthIndex(documentStore);
        }

        void RegisterPopulator()
        {
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            AutoMapping.RegisterPopulator((IAuthSession session, IUserAuth userAuth) =>
            {
                existingPopulator?.Invoke(session, userAuth);
                UpdateSessionKey(session, userAuth);
            });
        }

        public class UserAuth_By_UserNameOrEmail : AbstractIndexCreationTask<TUserAuth, UserAuth_By_UserNameOrEmail.Result>
        {
            public class Result
            {
                public string UserName { get; set; }
                public string Email { get; set; }
                public string[] Search { get; set; }
            }

            public UserAuth_By_UserNameOrEmail()
            {
                Map = users => from user in users
                               select new Result
                               {
                                   UserName = user.UserName,
                                   Email = user.Email,
                                   Search = new[] { user.UserName, user.Email }
                               };

                Index(x => x.Search, FieldIndexing.Exact);
            }
        }

        public class UserAuth_By_UserAuthDetails : AbstractIndexCreationTask<TUserAuthDetails, UserAuth_By_UserAuthDetails.Result>
        {
            public class Result
            {
                public string Provider { get; set; }
                public string UserId { get; set; }
                public string UserAuthId { get; set; }
                public DateTime ModifiedDate { get; set; }
            }

            public UserAuth_By_UserAuthDetails()
            {
                Map = userDetails => from userDetail in userDetails
                                     select new Result
                                     {
                                         Provider = userDetail.Provider,
                                         UserId = userDetail.UserId,
                                         ModifiedDate = userDetail.ModifiedDate,
                                         UserAuthId = userDetail.RefIdStr,
                                     };
            }
        }

        #region IUserAuthRepository
        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(newUser);

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            using var session = documentStore.OpenSession();
            session.Store(newUser);
            UpdateIntKey(newUser);
            session.SaveChanges();
            return newUser;
        }

        void UpdateIntKey(IUserAuth newUser)
        {
            var key = (string)UserAuthKeyProp.PublicGetter(newUser);
            newUser.Id = RavenIdConverter.ToInt(key);
        }

        string GetKey(IUserAuth newUser)
        {
            return (string)UserAuthKeyProp.PublicGetter(newUser);
        }

        public void DeleteUserAuth(string ravenUserAuthId)
        {
            using var session = documentStore.OpenSession();
            var userAuth = session.Load<TUserAuth>(ravenUserAuthId);

            var userAuthDetails = session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.UserAuthId == ravenUserAuthId);
            session.Delete(userAuth);
            userAuthDetails.Each(session.Delete);
            session.SaveChanges();
        }

        public IUserAuth GetUserAuth(string ravenUserAuthId)
        {
            using var session = documentStore.OpenSession();
            return session.Load<TUserAuth>(ravenUserAuthId);
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            newUser.ValidateNewUser();

            AssertNoExistingUser(newUser, existingUser);

            UpdateKey(existingUser, newUser);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using var session = documentStore.OpenSession();
            session.Store(newUser);
            session.SaveChanges();

            return newUser;
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(newUser, existingUser);

            UpdateKey(existingUser, newUser);

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using var session = documentStore.OpenSession();
            session.Store(newUser);
            session.SaveChanges();

            return newUser;
        }
        #endregion

        #region IAuthRepository
        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            using var session = documentStore.OpenSession();
            var authDetails = session
                .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                .OfType<TUserAuthDetails>()
                .FirstOrDefault();

            if (authDetails == null)
            {
                authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
                authDetails.Provider = tokens.Provider;
                authDetails.UserId = tokens.UserId;
            }

            authDetails.PopulateMissing(tokens);

            userAuth.PopulateMissingExtended(authDetails);
            userAuth.ModifiedDate = DateTime.UtcNow;
            userAuth.CreatedDate = (userAuth.CreatedDate == default) ? userAuth.ModifiedDate : userAuth.CreatedDate;

            session.Store(userAuth);
            UpdateIntKey(userAuth);
            session.SaveChanges();

            var key = GetKey(userAuth);

            authDetails.UserAuthId = userAuth.Id; // Partial FK int Id
            authDetails.RefIdStr = key; // FK

            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            session.Store(authDetails);
            session.SaveChanges();

            return authDetails;
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
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

            using var session = documentStore.OpenSession();
            var oAuthProvider = session
                .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                .OfType<TUserAuthDetails>()
                .FirstOrDefault();

            if (oAuthProvider != null)
                return session.Load<TUserAuth>(RavenIdConverter.ToString(UserAuthCollectionName, oAuthProvider.UserAuthId));

            return null;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            using var session = documentStore.OpenSession();
            var userAuth = session.Query<UserAuth_By_UserNameOrEmail.Result, UserAuth_By_UserNameOrEmail>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(x => x.Search.Contains(userNameOrEmail))
                .OfType<TUserAuth>()
                .FirstOrDefault();

            return userAuth;
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string ravenUserAuthId)
        {
            using var session = documentStore.OpenSession();
            return session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.UserAuthId == ravenUserAuthId)
                .OrderBy(x => x.ModifiedDate)
                .OfType<TUserAuthDetails>()
                .ToList()
                .ConvertAll(x => x as IUserAuthDetails);
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, (TUserAuth)userAuth);
        }

        void LoadUserAuth(IAuthSession session, TUserAuth userAuth)
        {
            UpdateSessionKey(session, userAuth);
            session.PopulateSession(userAuth, this);
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using var session = documentStore.OpenSession();
            var userAuth = LoadOrCreateFromSession(authSession, session);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            session.Store(userAuth);
            session.SaveChanges();
        }

        static TUserAuth LoadOrCreateFromSession(IAuthSession authSession, Raven.Client.Documents.Session.IDocumentSession session)
        {
            TUserAuth userAuth = null;
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var ravenKey = RavenIdConverter.ToString(UserAuthCollectionName, int.Parse(authSession.UserAuthId));
                userAuth = session.Load<TUserAuth>(ravenKey);
            }
            else
                userAuth = authSession.ConvertTo<TUserAuth>();
            return userAuth;
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            using var session = documentStore.OpenSession();
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            session.Store(userAuth);
            session.SaveChanges();
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

        void UpdateSessionKey(IAuthSession session, IUserAuth userAuth)
        {
            var keyProp = UserAuthProps.GetAccessor(UserAuthIdentifier);
            if (keyProp != null)
            {
                session.UserAuthId = (string)keyProp.PublicGetter(userAuth);
            }
        }

        void UpdateKey(IUserAuth existingUser, IUserAuth newUser)
        {
            var keyProp = UserAuthKeyProp;
            keyProp.PublicSetter(newUser, keyProp.PublicGetter(existingUser));
        }

        void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.UserAlreadyExistsFmt.LocalizeFmt(newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ErrorMessages.EmailAlreadyExistsFmt.LocalizeFmt(newUser.Email.SafeInput()));
            }
        }
        #endregion

        #region IQueryUserAuth
        public List<IUserAuth> GetUserAuths(string orderBy = null, int? skip = null, int? take = null)
        {
            using var session = documentStore.OpenSession();
            var q = session.Query<TUserAuth>();
            return SortAndPage(q, orderBy, skip, take).OfType<IUserAuth>().ToList();
        }

        public List<IUserAuth> SearchUserAuths(string query, string orderBy = null, int? skip = null, int? take = null)
        {
            if (string.IsNullOrEmpty(query))
                return GetUserAuths(orderBy, skip, take);

            using var session = documentStore.OpenSession();
            // RavenDB cant query string Contains/IndexOf
            var q = session.Query<TUserAuth>()
                .Where(x => x.UserName.StartsWith(query) || x.UserName.EndsWith(query) ||
                            x.Email.StartsWith(query) || x.Email.EndsWith(query))
                .Customize(x => x.WaitForNonStaleResults());

            return SortAndPage(q, orderBy, skip, take).OfType<IUserAuth>().ToList();
        }

        static IQueryable<TUserAuth> SortAndPage(IRavenQueryable<TUserAuth> q, string orderBy, int? skip, int? take)
        {
            var qEnum = q.AsQueryable();
            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy = AuthRepositoryUtils.ParseOrderBy(orderBy, out var desc);
                qEnum = desc
                    ? q.OrderByDescending(orderBy)
                    : q.OrderBy(orderBy);
            }

            if (skip != null)
                qEnum = qEnum.Skip(skip.Value);
            if (take != null)
                qEnum = qEnum.Take(take.Value);
            return qEnum;
        }
        #endregion

        #region ICustomUserAuth
        IUserAuth ICustomUserAuth.CreateUserAuth()
        {
            return Activator.CreateInstance<TUserAuth>();
        }

        IUserAuthDetails ICustomUserAuth.CreateUserAuthDetails()
        {
            return Activator.CreateInstance<TUserAuthDetails>();
        }
        #endregion
    }
}