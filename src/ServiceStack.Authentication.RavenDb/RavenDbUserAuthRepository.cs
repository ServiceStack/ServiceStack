﻿using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Indexes;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.RavenDb
{
    public class RavenDbUserAuthRepository : RavenDbUserAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public RavenDbUserAuthRepository(IDocumentStore documentStore) : base(documentStore) { }
    }

    public class RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository
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

                Index(x => x.Search, FieldIndexing.NotAnalyzed);
            }
        }

        public class UserAuth_By_UserAuthDetails : AbstractIndexCreationTask<TUserAuthDetails, UserAuth_By_UserAuthDetails.Result>
        {
            public class Result
            {
                public string Provider { get; set; }
                public string UserId { get; set; }
                public int UserAuthId { get; set; }
                public DateTime ModifiedDate { get; set; }
            }

            public UserAuth_By_UserAuthDetails()
            {
                Map = oauthProviders => from oauthProvider in oauthProviders
                    select new Result
                    {
                        Provider = oauthProvider.Provider,
                        UserId = oauthProvider.UserId,
                        ModifiedDate = oauthProvider.ModifiedDate,
                        UserAuthId = oauthProvider.UserAuthId
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
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            newUser.ValidateNewUser(password);

            AssertNoExistingUser(newUser);

            newUser.PopulatePasswordHashes(password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            using (var session = documentStore.OpenSession())
            {
                session.Store(newUser);
                session.SaveChanges();
            }

            return newUser;
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            newUser.ValidateNewUser();

            AssertNoExistingUser(newUser, existingUser);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = existingUser.PasswordHash;
            newUser.Salt = existingUser.Salt;
            newUser.DigestHa1Hash = existingUser.DigestHa1Hash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using (var session = documentStore.OpenSession())
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

            newUser.Id = existingUser.Id;
            newUser.PopulatePasswordHashes(password, existingUser);
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            using (var session = documentStore.OpenSession())
            {
                session.Store(newUser);
                session.SaveChanges();
            }

            return newUser;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            using (var session = documentStore.OpenSession())
            {
                var userAuth = session.Query<UserAuth_By_UserNameOrEmail.Result, UserAuth_By_UserNameOrEmail>()
                       .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                       .Where(x => x.Search.Contains(userNameOrEmail))
                       .OfType<TUserAuth>()
                       .FirstOrDefault();

                return userAuth;
            }
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
            session.PopulateSession(userAuth,
                GetUserAuthDetails(session.UserAuthId).ConvertAll(x => (IAuthTokens)x));
        }

        public void DeleteUserAuth(string userAuthId)
        {
            using (var session = documentStore.OpenSession())
            {
                if (int.TryParse(userAuthId, out var userId))
                {
                    var userAuth = session.Load<TUserAuth>(userId);
                    session.Delete(userAuth);

                    var userAuthDetails = session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                        .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                        .Where(q => q.UserAuthId == userId);

                    userAuthDetails.Each(session.Delete);
                }
                else
                {
                    var userAuth = session.Load<TUserAuth>(userAuthId);
                    session.Delete(userAuth);
                }
            }
        }

        public IUserAuth GetUserAuth(string userAuthId)
        {
            using (var session = documentStore.OpenSession())
            {
                int intAuthId;
                return int.TryParse(userAuthId, out intAuthId)
                    ? session.Load<TUserAuth>(intAuthId)
                    : session.Load<TUserAuth>(userAuthId);
            }
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            using (var session = documentStore.OpenSession())
            {
                int idInt = int.Parse(authSession.UserAuthId);

                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? session.Load<TUserAuth>(idInt)
                    : authSession.ConvertTo<TUserAuth>();

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
            using (var session = documentStore.OpenSession())
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
            using (var session = documentStore.OpenSession())
            {
                var id = int.Parse(userAuthId);
                return session.Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                    .Where(q => q.UserAuthId == id)
                    .OrderBy(x => x.ModifiedDate)
                    .OfType<TUserAuthDetails>()
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

            using (var session = documentStore.OpenSession())
            {
                var oAuthProvider = session
                    .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
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
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            using (var session = documentStore.OpenSession())
            {
                var authDetails = session
                    .Query<UserAuth_By_UserAuthDetails.Result, UserAuth_By_UserAuthDetails>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
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
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                session.Store(userAuth);
                session.SaveChanges();

                authDetails.UserAuthId = userAuth.Id;

                if (authDetails.CreatedDate == default(DateTime))
                    authDetails.CreatedDate = userAuth.ModifiedDate;
                authDetails.ModifiedDate = userAuth.ModifiedDate;

                session.Store(authDetails);
                session.SaveChanges();

                return authDetails;
            }
        }
    }
}
