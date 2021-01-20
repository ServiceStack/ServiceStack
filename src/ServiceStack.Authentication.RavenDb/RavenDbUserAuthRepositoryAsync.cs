using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using ServiceStack.Text;

namespace ServiceStack.Authentication.RavenDb
{
    public partial class RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepositoryAsync, IQueryUserAuthAsync
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public static async Task CreateOrUpdateUserAuthIndexAsync(IDocumentStore store, CancellationToken token=default)
        {
            // put this index into the ravendb database
            await new UserAuth_By_UserNameOrEmail().ExecuteAsync(store, token: token).ConfigAwait();
            await new UserAuth_By_UserAuthDetails().ExecuteAsync(store, token: token).ConfigAwait();
            isInitialized = true;
        }

        public async Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default)
        {
            newUser.ValidateNewUser(password);

            await AssertNoExistingUserAsync(newUser, token: token).ConfigAwait();

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            using var session = documentStore.OpenAsyncSession();
            await session.StoreAsync(newUser, token);
            await session.SaveChangesAsync(token);

            return newUser;
        }

        public async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token = default)
        {
            newUser.ValidateNewUser();

            await AssertNoExistingUserAsync(newUser, existingUser, token).ConfigAwait();

            UpdateKey(existingUser, newUser);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using var session = documentStore.OpenAsyncSession();
            await session.StoreAsync(newUser, token);
            await session.SaveChangesAsync(token);

            return newUser;
        }


        private async Task AssertNoExistingUserAsync(IUserAuth newUser, IUserAuth exceptForExistingUser = null, CancellationToken token = default)
        {
            if (newUser.UserName != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(newUser.UserName, token).ConfigAwait();
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.UserAlreadyExistsTemplate1, newUser.UserName.SafeInput()));
            }
            if (newUser.Email != null)
            {
                var existingUser = await GetUserAuthByUserNameAsync(newUser.Email, token).ConfigAwait();
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(string.Format(ErrorMessages.EmailAlreadyExistsTemplate1, newUser.Email.SafeInput()));
            }
        }

        public async Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token = default)
        {
            newUser.ValidateNewUser(password);

            await AssertNoExistingUserAsync(newUser, existingUser, token).ConfigAwait();

            UpdateKey(existingUser, newUser);

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using var session = documentStore.OpenAsyncSession();
            await session.StoreAsync(newUser, token);
            await session.SaveChangesAsync(token);

            return newUser;
        }

        public async Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token = default)
        {
            if (userNameOrEmail == null)
                return null;

            using var session = documentStore.OpenAsyncSession();
            var userAuth = await session.Query<UserAuth_By_UserNameOrEmail.Result, UserAuth_By_UserNameOrEmail>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(x => x.Search.Contains(userNameOrEmail))
                .OfType<TUserAuth>()
                .FirstOrDefaultAsync(token).ConfigAwait();
                
            return userAuth;
        }

        public async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default)
        {
            var userAuth = await GetUserAuthByUserNameAsync(userName, token).ConfigAwait();
            if (userAuth == null)
                return null;

            if (userAuth.VerifyPassword(password, out var needsRehash))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, needsRehash, password, token).ConfigAwait();
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token).ConfigAwait();
            return null;
        }

        public async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, CancellationToken token = default)
        {
            var userAuth = await GetUserAuthByUserNameAsync(digestHeaders["username"], token).ConfigAwait();
            if (userAuth == null)
                return null;

            if (userAuth.VerifyDigestAuth(digestHeaders, privateKey, nonceTimeOut, sequence))
            {
                await this.RecordSuccessfulLoginAsync(userAuth, token).ConfigAwait();
                return userAuth;
            }

            await this.RecordInvalidLoginAttemptAsync(userAuth, token).ConfigAwait();
            return null;
        }

        public async Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var userAuth = await GetUserAuthAsync(session, tokens, token).ConfigAwait();
            await LoadUserAuthAsync(session, (TUserAuth)userAuth, token).ConfigAwait();
        }

        private async Task LoadUserAuthAsync(IAuthSession session, TUserAuth userAuth, CancellationToken token = default)
        {
            UpdateSessionKey(session, userAuth);
            await session.PopulateSessionAsync(userAuth, this, token).ConfigAwait();
        }

        public async Task DeleteUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            var userAuth = await session.LoadAsync<TUserAuth>(userAuthId, token);

            var userAuthDetails = await session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.UserAuthId == userAuthId).ToListAsync(token);
            userAuthDetails.Each(session.Delete);
            session.Delete(userAuth);
            await session.SaveChangesAsync(token);
        }

        public async Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            return await session.LoadAsync<TUserAuth>(userAuthId, token);
        }

        public async Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            int idInt = int.Parse(authSession.UserAuthId);

            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? await session.LoadAsync<TUserAuth>(authSession.UserAuthId, token)
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = idInt;

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await session.StoreAsync(userAuth, token);
            await session.SaveChangesAsync(token);
        }

        public async Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default)
                userAuth.CreatedDate = userAuth.ModifiedDate;

            await session.StoreAsync(userAuth, token);
            await session.SaveChangesAsync(token);
        }

        public async Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            return (await session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                    .Customize(x => x.WaitForNonStaleResults())
                    .Where(q => q.UserAuthId == userAuthId)
                    .OrderBy(x => x.ModifiedDate)
                    .OfType<TUserAuthDetails>()
                    .ToListAsync(token).ConfigAwait())
                .ConvertAll(x => x as IUserAuthDetails);
        }

        public async Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthAsync(authSession.UserAuthId, token).ConfigAwait();
                if (userAuth != null) return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = await GetUserAuthByUserNameAsync(authSession.UserAuthName, token).ConfigAwait();
                if (userAuth != null) return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            using var session = documentStore.OpenAsyncSession();
            var oAuthProvider = await session
                .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                .OfType<TUserAuthDetails>()
                .FirstOrDefaultAsync(token).ConfigAwait();

            if (oAuthProvider != null)
            {
                var userAuth = await session.LoadAsync<TUserAuth>(oAuthProvider.UserAuthId.ToString(), token);
                return userAuth;
            }
            return null;
        }

        public async Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            var userAuth = await GetUserAuthAsync(authSession, tokens, token)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            using var session = documentStore.OpenAsyncSession();
            var authDetails = await session
                .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(q => q.Provider == tokens.Provider && q.UserId == tokens.UserId)
                .OfType<TUserAuthDetails>()
                .FirstOrDefaultAsync(token).ConfigAwait();

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

            await session.StoreAsync(userAuth, token);
            await session.SaveChangesAsync(token);

            var key = (string)UserAuthKeyProp.PublicGetter(userAuth); 
            if (userAuth.Id == default)
            {
                userAuth.Id = RavenDbUserAuthRepository.ParseIntId(key);
            }

            authDetails.UserAuthId = userAuth.Id; // Partial FK int Id
            authDetails.RefIdStr = key; // FK

            if (authDetails.CreatedDate == default)
                authDetails.CreatedDate = userAuth.ModifiedDate;
            authDetails.ModifiedDate = userAuth.ModifiedDate;

            await session.StoreAsync(authDetails, token);
            await session.SaveChangesAsync(token);

            return authDetails;
        }

        public async Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
        {
            using var session = documentStore.OpenAsyncSession();
            var q = session.Query<TUserAuth>();
            return (await SortAndPage(q, orderBy, skip, take).ToListAsync(token)).OfType<IUserAuth>().ToList();
        }

        public async Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(query))
                return await GetUserAuthsAsync(orderBy, skip, take, token).ConfigAwait();

            using var session = documentStore.OpenAsyncSession();
            // RavenDB cant query string Contains/IndexOf
            var q = session.Query<TUserAuth>()
                .Where(x => x.UserName.StartsWith(query) || x.UserName.EndsWith(query) || 
                            x.Email.StartsWith(query) || x.Email.EndsWith(query))
                .Customize(x => x.WaitForNonStaleResults());

            return (await SortAndPage(q, orderBy, skip, take).ToListAsync(token)).OfType<IUserAuth>().ToList();
        }
        
    }
}
