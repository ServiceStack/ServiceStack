using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Auth;

using Raven.Client;
using ServiceStack.DataAnnotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Linq;

namespace ServiceStack.Authentication.RavenDb
{
    [Index(Name = nameof(Key))]
    public class RavenUserAuth : UserAuth
    {
        string key;

        public string Key
        {
            get => key;

            set
            {
                key = value;
                Id = RavenIdConverter.ToInt(key);
            }
        }
    }
    
    [Index(Name = nameof(Key))]
    public class RavenUserAuthDetails : UserAuthDetails
    {
        string key;

        public string Key
        {
            get => key;

            set
            {
                key = value;
                Id = RavenIdConverter.ToInt(key);
            }
        }
    }
    
    public class RavenDbUserAuthRepository : RavenDbUserAuthRepository<RavenUserAuth, RavenUserAuthDetails>, IUserAuthRepository
    {
        public RavenDbUserAuthRepository(IDocumentStore documentStore) : base(documentStore) { }

        public static Func<MemberInfo, bool> FindIdentityProperty { get; set; } = DefaultFindIdentityProperty;

        public static bool DefaultFindIdentityProperty(MemberInfo p) => 
            p.Name == (p.DeclaringType.FirstAttribute<IndexAttribute>()?.Name ?? "Id");
    }

    public partial class RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IQueryUserAuth
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        private readonly IDocumentStore documentStore;
        private static bool isInitialized = false;

        public static void CreateOrUpdateUserAuthIndex(IDocumentStore store)
        {
            // put this index into the ravendb database
            new UserAuth_By_UserNameOrEmail().Execute(store);
            new UserAuth_By_UserAuthDetails().Execute(store);
            isInitialized = true;
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

        public RavenDbUserAuthRepository(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;

            // if the user didn't call this method in their AppHostBase
            // Let's call if for them. No worries if this is called a few
            // times, we just don't want it running all the time
            if (!isInitialized)
                CreateOrUpdateUserAuthIndex(documentStore);

            RegisterPopulator();
        }

        public virtual void RegisterPopulator()
        {
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            AutoMapping.RegisterPopulator((IAuthSession session, IUserAuth userAuth) => {
                existingPopulator?.Invoke(session, userAuth);
                UpdateSessionKey(session, userAuth);
            });
        }

        public virtual void UpdateSessionKey(IAuthSession session, IUserAuth userAuth)
        {
            var ra = userAuth.ConvertTo<RavenUserAuth>();
            if (ra.Id != default)
                ra.Key = RavenIdConverter.ToString(RavenIdConverter.RavenUserAuthsIdPrefix, ra.Id);
            session.UserAuthId = ra.Key;
        }

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

        private void UpdateKey(IUserAuth existingUser, IUserAuth newUser)
        {
            ((RavenUserAuth)newUser).Key = ((RavenUserAuth)existingUser).Key;
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

        private void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
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

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            using var session = documentStore.OpenSession();
            var userAuth = session.Query<UserAuth_By_UserNameOrEmail.Result, UserAuth_By_UserNameOrEmail>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(x => x.Search.Contains(userNameOrEmail))
                .OfType<TUserAuth>()
                .FirstOrDefault();
                
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
            LoadUserAuth(session, (TUserAuth)userAuth);
        }

        private void LoadUserAuth(IAuthSession session, TUserAuth userAuth)
        {
            UpdateSessionKey(session, userAuth);
            session.PopulateSession(userAuth, this);
        }

        public void DeleteUserAuth(string userAuthId)
        {
            using var session = documentStore.OpenSession();
            var userAuth = session.Load<TUserAuth>(userAuthId);

            var userAuthDetails = session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.UserAuthId == userAuthId);
            session.Delete(userAuth);
            userAuthDetails.Each(session.Delete);
            session.SaveChanges();
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using var session = documentStore.OpenSession();
            return int.TryParse(userAuthId, out var intAuthId)
                ? session.Load<TUserAuth>(intAuthId)
                : session.Load<TUserAuth>(userAuthId);
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using var session = documentStore.OpenSession();
            int idInt = int.Parse(authSession.UserAuthId);

            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? session.Load<TUserAuth>(idInt)
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = idInt;

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            session.Store(userAuth);
            session.SaveChanges();
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

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            using var session = documentStore.OpenSession();
            return  session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.UserAuthId == userAuthId)
                .OrderBy(x => x.ModifiedDate)
                .OfType<TUserAuthDetails>()
                .ToList()
                .ConvertAll(x => x as IUserAuthDetails);
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

            using var session = documentStore.OpenSession();
            var oAuthProvider = session
                .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                .OfType<TUserAuthDetails>()
                .FirstOrDefault();

            if (oAuthProvider != null)
            {
                var userAuth = session.Load<TUserAuth>(oAuthProvider.UserAuthId);
                return userAuth;
            }
            return null;
        }

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
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            session.Store(userAuth);
            session.SaveChanges();

            var key = ((RavenUserAuth)userAuth).Key;

            if (userAuth.Id == default)
                userAuth.Id = RavenIdConverter.ToInt(key);

            authDetails.UserAuthId = userAuth.Id; // Partial FK int Id
            authDetails.RefIdStr = key; // FK

            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            session.Store(authDetails);
            session.SaveChanges();

            return authDetails;
        }

        public static IQueryable<TUserAuth> SortAndPage(IRavenQueryable<TUserAuth> q, string orderBy, int? skip, int? take)
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
    }
    
    internal static class RavenDbExtensions
    {
        public static T Load<T>(this IDocumentSession session, int id)
        {
            return session.Load<T>(id.ToString());
        }
    }
}