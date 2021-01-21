using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using ServiceStack.Auth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Authentication.RavenDb
{
    public partial class RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IQueryUserAuth
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        readonly IDocumentStore documentStore;
        public static bool UserAuthIndexCreated { get; private set; } = false;

        public static void CreateOrUpdateUserAuthIndex(IDocumentStore store)
        {
            new UserAuth_By_UserNameOrEmail().Execute(store);
            new UserAuth_By_UserAuthDetails().Execute(store);
            UserAuthIndexCreated = true;
        }

        public RavenDbUserAuthRepository(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;

            EnsureThatUniqueIndexesAreCreated(documentStore);

            RegisterPopulator();
        }

            static void EnsureThatUniqueIndexesAreCreated(IDocumentStore documentStore)
            {
                if (!UserAuthIndexCreated)
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

        #region IUserAuthRepository
        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            var nu = ((UserAuth)newUser).ToRavenUserAuth();
            nu.ValidateNewUser(password);

            AssertNoExistingUser(nu);

            nu.PopulatePasswordHashes(password);
            nu.CreatedDate = DateTime.UtcNow;
            nu.ModifiedDate = nu.CreatedDate;

            using var session = documentStore.OpenSession();
            session.Store(nu);
            session.SaveChanges();

            return nu;
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
            session.SaveChanges();

            var key = ((RavenUserAuth)userAuth).Key;

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
                return session.Load<TUserAuth>(RavenIdConverter.ToString(RavenIdConverter.RavenUserAuthsIdPrefix, oAuthProvider.UserAuthId));

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
                    var ravenKey = RavenIdConverter.ToString(RavenIdConverter.RavenUserAuthsIdPrefix, int.Parse(authSession.UserAuthId));
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
            var ra = userAuth.ConvertTo<RavenUserAuth>();
            if (ra.Id != default)
                ra.Key = RavenIdConverter.ToString(RavenIdConverter.RavenUserAuthsIdPrefix, ra.Id);
            session.UserAuthId = ra.Key;
        }

        void UpdateKey(IUserAuth existingUser, IUserAuth newUser)
        {
            ((RavenUserAuth)newUser).Key = ((RavenUserAuth)existingUser).Key;
        }

        void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
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
    }
}