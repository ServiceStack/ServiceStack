using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Raven.Client;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.RavenDb
{
    public class RavenDbUserAuthRepository : IUserAuthRepository
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly IDocumentStore _documentStore;
        private static bool _isInitialized = false;

        public static void CreateOrUpdateUserAuthIndex(IDocumentStore store)
        {
            // put this index into the ravendb database
            new ServiceStack_UserAuth_ByUserNameOrEmail().Execute(store);
            new ServiceStack_UserAuth_ByOAuthProvider().Execute(store);
            _isInitialized = true;
        }

        public RavenDbUserAuthRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;

            // if the user didn't call this method in their AppHostBase
            // Let's call if for them. No worries if this is called a few
            // times, we just don't want it running all the time
            if (!_isInitialized)
                CreateOrUpdateUserAuthIndex(documentStore);
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            password.ThrowIfNullOrEmpty("password");

            ValidateNewUserWithoutPassword(newUser);
        }

        private void ValidateNewUserWithoutPassword(IUserAuth newUser)
        {
            newUser.ThrowIfNull("newUser");

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

            AssertNoExistingUser(newUser);

            var saltedHash = new SaltedHash();
            string salt;
            string hash;
            saltedHash.GetHashAndSaltString(password, out hash, out salt);
            var digestHelper = new DigestAuthFunctions();
            newUser.DigestHa1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            using (var session = _documentStore.OpenSession())
            {
                session.Store(newUser);
                session.SaveChanges();
            }

            return newUser;
        }

        private void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
            }
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password = null)
        {
            ValidateNewUserWithoutPassword(newUser);

            AssertNoExistingUser(newUser, existingUser);

            var hash = existingUser.PasswordHash;
            var salt = existingUser.Salt;
            if (password != null)
            {
                var saltedHash = new SaltedHash();
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

            using (var session = _documentStore.OpenSession())
            {
                session.Store(newUser);
                session.SaveChanges();
            }

            return newUser;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            using (var session = _documentStore.OpenSession())
            {
                var userAuth = session.Query<ServiceStack_UserAuth_ByUserNameOrEmail.Result, ServiceStack_UserAuth_ByUserNameOrEmail>()
                       .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                       .Search(x => x.Search, userNameOrEmail)
                       .OfType<UserAuth>()
                       .FirstOrDefault();

                return userAuth;
            }
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) return false;

            var saltedHash = new SaltedHash();
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
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) return false;

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, PrivateKey, NonceTimeOut, userAuth.DigestHa1Hash, sequence))
            {
                return true;
            }
            userAuth = null;
            return false;
        }

        public void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, (UserAuth)userAuth);
        }

        private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
        {
            if (userAuth == null) return;

            var idSesije = session.Id;  //first record session Id (original session Id)
            session.PopulateWith(userAuth); //here, original sessionId is overwritten with facebook user Id
            session.Id = idSesije;  //we return Id of original session here

            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserAuthDetails(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens)x);

        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using (var session = _documentStore.OpenSession())
            {
                int intAuthId;
                return int.TryParse(userAuthId, out intAuthId)
                    ? session.Load<UserAuth>(intAuthId)
                    : session.Load<UserAuth>(userAuthId);
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using (var session = _documentStore.OpenSession())
            {
                int idInt = int.Parse(authSession.UserAuthId);

                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                                   ? session.Load<UserAuth>(idInt)
                                   : authSession.ConvertTo<UserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                    userAuth.Id = idInt;

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                session.Store(userAuth);
                session.SaveChanges();
            }
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            using (var session = _documentStore.OpenSession())
            {
                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                session.Store(userAuth);
                session.SaveChanges();
            }
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            using (var session = _documentStore.OpenSession())
            {
                var id = int.Parse(userAuthId);
                return session.Query<ServiceStack_UserAuth_ByOAuthProvider.Result, ServiceStack_UserAuth_ByOAuthProvider>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                    .Where(q => q.UserAuthId == id)
                    .OrderBy(x => x.ModifiedDate)
                    .OfType<UserAuthDetails>()
                    .ToList()
                    .Cast<IUserAuthDetails>().ToList();
            }
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

            using (var session = _documentStore.OpenSession())
            {
                var oAuthProvider = session
                    .Query<ServiceStack_UserAuth_ByOAuthProvider.Result, ServiceStack_UserAuth_ByOAuthProvider>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                    .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                    .OfType<UserAuthDetails>()
                    .FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = session.Load<UserAuth>(oAuthProvider.UserAuthId);
                    return userAuth;
                }
                return null;
            }
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new UserAuth();

            using (var session = _documentStore.OpenSession())
            {
                var oAuthProvider = session
                    .Query<ServiceStack_UserAuth_ByOAuthProvider.Result, ServiceStack_UserAuth_ByOAuthProvider>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                    .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                    .OfType<UserAuthDetails>()
                    .FirstOrDefault();

                if (oAuthProvider == null)
                {
                    oAuthProvider = new UserAuthDetails
                    {
                        Provider = tokens.Provider,
                        UserId = tokens.UserId,
                    };
                }

                oAuthProvider.PopulateMissing(tokens);
                userAuth.PopulateMissingExtended(oAuthProvider);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                session.Store(userAuth);
                session.SaveChanges();

                oAuthProvider.UserAuthId = userAuth.Id;

                if (oAuthProvider.CreatedDate == default(DateTime))
                    oAuthProvider.CreatedDate = userAuth.ModifiedDate;
                oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

                session.Store(oAuthProvider);
                session.SaveChanges();

                return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
