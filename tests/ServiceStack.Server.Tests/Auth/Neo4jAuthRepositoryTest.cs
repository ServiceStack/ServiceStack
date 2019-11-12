using System;
using System.Collections.Generic;
using Neo4j.Driver.V1;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Authentication.Neo4j;
using AuthTokens = ServiceStack.Auth.AuthTokens;

namespace ServiceStack.Server.Tests.Auth
{
    [TestFixture]
    //[Ignore("Requires Neo4j Dependency")]
    public class Neo4jAuthRepositoryTest
    {
        private const string Password = "letmein";

        private ServiceStackHost AppHost { get; set; }
        private IUserAuthRepository Sut { get; set; }

        protected virtual ServiceStackHost CreateAppHost()
        {
            return new AppHost
            {
                Use = container =>
                {
                    container.Register(c => GraphDatabase.Driver("bolt://localhost:7687"));
                    container.RegisterAutoWiredAs<Neo4jAuthRepository, IUserAuthRepository>();
                    container.RegisterAutoWiredAs<Neo4jAuthRepository, IAuthRepository>();
                }
            };
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            AppHost = CreateAppHost().Init();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AppHost.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            Sut = AppHost.Container.Resolve<IUserAuthRepository>();
            ((IClearable)Sut).Clear();
        }

        [Test]
        public void LoadUserAuth_By_Id()
        {
            // Arrange
            var userAuth = CreateUserAuth();

            var authSession = new AuthUserSession
            {
                UserAuthId = userAuth.Id.ToString()
            };

            var tokens = new AuthTokens();

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            Assert.AreEqual(userAuth.UserName, authSession.UserName);
        }

        [Test]
        public void LoadUserAuth_By_Name()
        {
            // Arrange
            var userAuth = CreateUserAuth();

            var authSession = new AuthUserSession
            {
                UserAuthName = userAuth.UserName
            };

            var tokens = new AuthTokens();

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            Assert.AreEqual(userAuth.UserName, authSession.UserName);
        }

        [Test]
        public void LoadUserAuth_By_Token()
        {
            // Arrange
            var userAuth = CreateUserAuth();
            var details = CreateUserAuthDetails(userAuth);

            var authSession = new AuthUserSession
            {
                UserAuthName = userAuth.UserName
            };

            var tokens = new AuthTokens();

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            Assert.AreEqual(userAuth.UserName, authSession.UserName);
        }

        [Test]
        public void SaveUserAuth(IAuthSession authSession)
        {
            // Act
            Sut.SaveUserAuth(authSession);
        }

        [Test]
        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            // Act
            var result = Sut.GetUserAuthDetails(userAuthId);
            return result;
        }

        [Test]
        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            // Act
            var result = Sut.CreateOrMergeAuthSession(authSession, tokens);
            return result;
        }

        [Test]
        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            var result = Sut.GetUserAuth(authSession, tokens);
            return result;
        }

        [Test]
        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            var result = Sut.GetUserAuthByUserName(userNameOrEmail);
            return result;
        }

        [Test]
        public void SaveUserAuth(IUserAuth userAuth)
        {
            Sut.SaveUserAuth(userAuth);
        }

        [Test]
        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            var result = Sut.TryAuthenticate(userName, password, out userAuth);
            return result;
        }

        [Test]
        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence,
            out IUserAuth userAuth)
        {
            var result = Sut.TryAuthenticate(digestHeaders, privateKey, nonceTimeOut, sequence, out userAuth);
            return result;
        }

        [Test]
        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            var result = Sut.CreateUserAuth(newUser, password);
            return result;
        }

        [Test]
        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser)
        {
            var result = Sut.UpdateUserAuth(existingUser, newUser);
            return result;
        }

        [Test]
        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            var result = Sut.UpdateUserAuth(existingUser, newUser, password);
            return result;
        }

        [Test]
        public IUserAuth GetUserAuth(string userAuthId)
        {
            var result = Sut.GetUserAuth(userAuthId);
            return result;
        }

        [Test]
        public void DeleteUserAuth(string userAuthId)
        {
            Sut.DeleteUserAuth(userAuthId);
        }

        private IUserAuth CreateUserAuth(Action<UserAuth> userAuthFn = null)
        {
            var userAuth = GetUserAuth();
            userAuthFn?.Invoke(userAuth);

            return Sut.CreateUserAuth(userAuth, Password);
        }

        private static UserAuth GetUserAuth()
        {
            return new UserAuth
            {
                Address = nameof(UserAuth.Address),
                Address2 = nameof(UserAuth.Address2),
                BirthDate = new DateTime(2001, 4, 24),
                BirthDateRaw = nameof(UserAuth.BirthDateRaw),
                City = nameof(UserAuth.City),
                Company = nameof(UserAuth.Company),
                Country = nameof(UserAuth.Country),
                Culture = nameof(UserAuth.Culture),
                DigestHa1Hash = nameof(UserAuth.DigestHa1Hash),
                Email = "email@example.com",
                FirstName = nameof(UserAuth.FirstName),
                FullName = nameof(UserAuth.FullName),
                Gender = nameof(UserAuth.Gender),
                Language = nameof(UserAuth.Language),
                LastName = nameof(UserAuth.LastName),
                MailAddress = nameof(UserAuth.MailAddress),
                Nickname = nameof(UserAuth.Nickname),
                PhoneNumber = nameof(UserAuth.PhoneNumber),
                PostalCode = nameof(UserAuth.PostalCode),
                PrimaryEmail = nameof(UserAuth.PrimaryEmail),
                Permissions = new List<string> {"First", "Second"},
                RecoveryToken = nameof(UserAuth.RecoveryToken),
                RefId = 123,
                Roles = new List<string> {"Role1", "Role2"},
                RefIdStr = nameof(UserAuth.RefIdStr),
                State = nameof(UserAuth.State),
                TimeZone = nameof(UserAuth.TimeZone),
                UserName = nameof(UserAuth.UserName),
            };
        }

        private IUserAuthDetails CreateUserAuthDetails(IUserAuth userAuth)
        {
            var authSession = GetAuthUserSession();
            authSession.UserAuthId = userAuth.Id.ToString();

            var tokens = GetAuthTokens();
            tokens.UserId = authSession.UserAuthId;
            tokens.Provider = "google";

            return Sut.CreateOrMergeAuthSession(authSession, tokens);
        }

        private static AuthUserSession GetAuthUserSession()
        {
            return new AuthUserSession();
        }

        private static AuthTokens GetAuthTokens()
        {
            return new AuthTokens();
        }
    }

}