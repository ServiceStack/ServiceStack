using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Authentication.Neo4j;
using AuthTokens = ServiceStack.Auth.AuthTokens;

namespace ServiceStack.Server.Tests.Auth
{
    [TestFixture]
    [Ignore("Requires Neo4j Dependency")]
    public class Neo4jAuthRepositoryTests
    {
        public class AppUser : UserAuth
        {
            public string AppName { get; set; }
        }
        
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
                    container.RegisterAutoWiredAs<Neo4jAuthRepository<AppUser, UserAuthDetails>, IUserAuthRepository>();
                    container.RegisterAutoWiredAs<Neo4jAuthRepository<AppUser, UserAuthDetails>, IAuthRepository>();
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
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;

            var authSession = new AuthUserSession
            {
                UserAuthId = userAuth.Id.ToString()
            };

            var tokens = new AuthTokens();

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            authSession.UserName.Should().Be(userAuth.UserName);
        }

        [Test]
        public void LoadUserAuth_By_Name()
        {
            // Arrange
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;

            var authSession = new AuthUserSession
            {
                UserAuthName = userAuth.UserName
            };

            var tokens = new AuthTokens();

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            authSession.UserName.Should().Be(userAuth.UserName);
        }

        [Test]
        public void LoadUserAuth_By_Token()
        {
            // Arrange
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;
            CreateUserAuthDetails(userAuth, "google");

            var authSession = new AuthUserSession();

            var tokens = new AuthTokens
            {
                UserId = userAuth.Id.ToString(),
                Provider = "google"
            };

            // Act
            Sut.LoadUserAuth(authSession, tokens);

            // Assert
            authSession.UserName.Should().Be(userAuth.UserName);
        }

        [Test]
        public void SaveUserAuth_Should_Throw_When_UserAuthId_Is_Not_An_Integer()
        {
            // Arrange
            var authUserSession = new AuthUserSession
            {
                UserAuthId = "Test"
            };

            // Act
            var exception = Assert.Throws<ArgumentException>(
                () => Sut.SaveUserAuth(authUserSession));
            
            // Assert
            exception.Message.Should().Be("Cannot convert to integer\r\nParameter name: userAuthId");
        }

        [Test]
        public void Should_SaveUserAuth()
        {
            // Arrange
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;

            var authUserSession = new AuthUserSession
            {
                UserAuthId = userAuth.Id.ToString()
            };

            // Act
            Sut.SaveUserAuth(authUserSession);
            
            // Assert
            var updatedUserAuth = Sut.GetUserAuth(userAuth.Id.ToString());
            updatedUserAuth.ModifiedDate.Should().BeAfter(userAuth.ModifiedDate);
        }

        [Test]
        public void SaveUserAuth_From_AuthSession()
        {
            // Arrange
            var authUserSession = NewAuthUserSession;

            // Act
            Sut.SaveUserAuth(authUserSession);
            
            // Assert
            authUserSession.UserAuthId.Should().NotBeNullOrEmpty();
            authUserSession.UserAuthId.ThrowIfNotConvertibleToInteger(nameof(AuthUserSession.UserAuthId));

            var userAuth = Sut.GetUserAuth(authUserSession.UserAuthId);
            userAuth.Email.Should().Be(authUserSession.Email);
        }

        [Test]
        public void Should_GetUserAuthDetails()
        {
            // Arrange
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;
            CreateUserAuthDetails(userAuth, "google");
            CreateUserAuthDetails(userAuth, "twitter");
            
            // Act
            var result = Sut.GetUserAuthDetails(userAuth.Id);
            
            // Assert
            result.Select(d => d.Provider).Should().Contain("google");
            result.Select(d => d.Provider).Should().Contain("twitter");
        }

        [Test]
        public void CreateOrMergeAuthSession_Create()
        {
            // Arrange
            var authSession = NewAuthUserSession;
            var tokens = NewAuthTokens;
            
            // Act
            var result = Sut.CreateOrMergeAuthSession(authSession, tokens);
            
            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.UserAuthId.Should().Be(result.Id);
        }

        [Test]
        public void CreateOrMergeAuthSession_Update()
        {
            // Arrange
            var userAuth = Sut.CreateUserAuth(NewUserAuth, Password);;
            CreateUserAuthDetails(userAuth, "google");

            var authSession = new AuthUserSession();

            var tokens = new AuthTokens
            {
                UserId = userAuth.Id.ToString(),
                Provider = "google"
            };
            
            // Act
            var result = Sut.CreateOrMergeAuthSession(authSession, tokens);
            
            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.Id.Should().Be(result.UserAuthId);
            result.ModifiedDate.Should().BeAfter(userAuth.ModifiedDate);
        }

        [Test]
        public void Should_GetUserAuth()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            CreateUserAuthDetails(createdUserAuth, "google");

            var authSession = new AuthUserSession();

            var tokens = new AuthTokens
            {
                UserId = createdUserAuth.Id.ToString(),
                Provider = "google"
            };

            // Act
            var result = Sut.GetUserAuth(authSession, tokens);
            
            // Assert
            result.Should().BeEquivalentTo(NewUserAuth, options => options
                .Excluding(m => m.Id)
                .Excluding(m => m.PasswordHash)
                .Excluding(m => m.DigestHa1Hash)
                .Excluding(m => m.CreatedDate)
                .Excluding(m => m.ModifiedDate));
        }

        [Test]
        public void GetUserAuthByUserName_By_UserName()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            // Act
            var result = Sut.GetUserAuthByUserName(createdUserAuth.UserName);
            
            // Assert
            result.Should().BeEquivalentTo(NewUserAuth, options => options
                .Excluding(m => m.Id)
                .Excluding(m => m.PasswordHash)
                .Excluding(m => m.DigestHa1Hash)
                .Excluding(m => m.CreatedDate)
                .Excluding(m => m.ModifiedDate));
        }

        [Test]
        public void GetUserAuthByUserName_By_Email()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            // Act
            var result = Sut.GetUserAuthByUserName(createdUserAuth.Email);
            
            // Assert
            result.Should().BeEquivalentTo(NewUserAuth, options => options
                .Excluding(m => m.Id)
                .Excluding(m => m.PasswordHash)
                .Excluding(m => m.DigestHa1Hash)
                .Excluding(m => m.CreatedDate)
                .Excluding(m => m.ModifiedDate));
        }

        [Test]
        public void SaveUserAuth_From_New()
        {
            // Arrange
            var newUserAuth = NewUserAuth;
            
            // Act
            Sut.SaveUserAuth(newUserAuth);
            
            // Assert
            var createdUserAuth = Sut.GetUserAuth(newUserAuth.Id);
            createdUserAuth.Id.Should().BeGreaterThan(0);
            createdUserAuth.CreatedDate.Should().NotBe(default);
            createdUserAuth.ModifiedDate.Should().NotBe(default);
        }

        [Test]
        public void SaveUserAuth_From_Updated()
        {
            // Arrange
            var newUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            var modifiedDate = newUserAuth.ModifiedDate;
            
            // Act
            Sut.SaveUserAuth(newUserAuth);
            
            // Assert
            newUserAuth.ModifiedDate.Should().BeAfter(modifiedDate);
        }

        [Test]
        public void TryAuthenticate_With_Invalid_UserAuth()
        {
            // Act
            var result = Sut.TryAuthenticate("invalid", null, out var userAuth);

            // Assert
            result.Should().BeFalse();
            userAuth.Should().BeNull();
        }

        [Test]
        public void TryAuthenticate_With_Valid_Credentials()
        {
            // Arrange
            var newUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            
            // Act
            var result = Sut.TryAuthenticate(newUserAuth.UserName, Password, out var userAuth);

            // Assert
            result.Should().BeTrue();
            userAuth.Id.Should().Be(newUserAuth.Id);
        }

        [Test]
        public void TryAuthenticate_With_Invalid_Password()
        {
            // Arrange
            var newUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            
            // Act
            var result = Sut.TryAuthenticate(newUserAuth.UserName, "invalid", out var userAuth);

            // Assert
            result.Should().BeFalse();
            userAuth.Should().BeNull();
        }

        [Test]
        public void TryAuthenticate_DigestAuth_With_Invalid_Username()
        {
            // Assert
            var digestHeaders = new Dictionary<string, string>
            {
                {"username", "invalid"}
            };

            // Act
            var result = Sut.TryAuthenticate(digestHeaders, default, default, default, out var userAuth);

            // Assert
            result.Should().BeFalse();
            userAuth.Should().BeNull();
        }

        [Test]
        public void Should_CreateUserAuth()
        {
            // Act
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            
            // Assert
            var result = Sut.GetUserAuthByUserName(createdUserAuth.UserName);
            result.Should().BeEquivalentTo(NewUserAuth, options => options
                .Excluding(m => m.Id)
                .Excluding(m => m.PasswordHash)
                .Excluding(m => m.DigestHa1Hash)
                .Excluding(m => m.CreatedDate)
                .Excluding(m => m.ModifiedDate));
        }

        [Test]
        public void UpdateUserAuth_Throws_ArgumentNullException_When_Username_Is_Null()
        {
            // Arrange
            var newUser = new UserAuth();
            
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Sut.UpdateUserAuth(default, newUser));
        }

        [Test]
        public void UpdateUserAuth_Throws_ArgumentNullException_When_User_With_Username_Already_Exists()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            var newUser = new UserAuth {UserName = createdUserAuth.UserName};
            
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Sut.UpdateUserAuth(default, newUser));
        }

        [Test]
        public void UpdateUserAuth_Throws_ArgumentNullException_When_User_With_Email_Already_Exists()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            var newUser = new UserAuth
            {
                UserName = "new",
                Email = createdUserAuth.Email
            };
            
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Sut.UpdateUserAuth(default, newUser));
        }

        [Test]
        public void Should_UpdateUserAuth()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            var modifiedUserAuth = Sut.GetUserAuth(createdUserAuth.Id.ToString());
            modifiedUserAuth.UserName = "newuser";
            modifiedUserAuth.Email = "newemail@fb.com";

            // Act
            var updatedUserAuth = Sut.UpdateUserAuth(createdUserAuth, modifiedUserAuth);
            
            // Assert
            updatedUserAuth.Should().BeEquivalentTo(createdUserAuth, options => options
                .Excluding(m => m.UserName)
                .Excluding(m => m.Email)
                .Excluding(m => m.ModifiedDate));
            
            updatedUserAuth.ModifiedDate.Should().BeAfter(createdUserAuth.ModifiedDate);
        }

        [Test]
        public void Should_UpdateUserAuth_With_Password()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            var modifiedUserAuth = Sut.GetUserAuth(createdUserAuth.Id.ToString());
            modifiedUserAuth.UserName = "newuser";
            modifiedUserAuth.Email = "newemail@fb.com";

            // Act
            var updatedUserAuth = Sut.UpdateUserAuth(createdUserAuth, modifiedUserAuth, "newpass");
            
            // Assert
            updatedUserAuth.Should().BeEquivalentTo(createdUserAuth, options => options
                .Excluding(m => m.UserName)
                .Excluding(m => m.Email)
                .Excluding(m => m.ModifiedDate)
                .Excluding(m => m.PasswordHash));
            
            updatedUserAuth.ModifiedDate.Should().BeAfter(createdUserAuth.ModifiedDate);
            
            var result = Sut.TryAuthenticate(updatedUserAuth.UserName, "newpass", out _);
            result.Should().BeTrue();
        }

        [Test]
        public void GetUserAuth_By_UserAuthId()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);

            // Act
            var result = Sut.GetUserAuth(createdUserAuth.Id.ToString());
            
            // Assert
            result.Should().BeEquivalentTo(NewUserAuth, options => options
                .Excluding(m => m.Id)
                .Excluding(m => m.PasswordHash)
                .Excluding(m => m.DigestHa1Hash)
                .Excluding(m => m.CreatedDate)
                .Excluding(m => m.ModifiedDate));
        }

        [Test]
        public void DeleteUserAuth()
        {
            // Arrange
            var createdUserAuth = Sut.CreateUserAuth(NewUserAuth, Password);
            
            // Act
            Sut.DeleteUserAuth(createdUserAuth.Id.ToString());

            // Assert
            var deletedUserAuth = Sut.GetUserAuth(createdUserAuth.Id.ToString());
            deletedUserAuth.Should().BeNull();
        }

        private IUserAuthDetails CreateUserAuthDetails(IUserAuth userAuth, string provider)
        {
            var authSession = NewAuthUserSession;
            authSession.UserAuthId = userAuth.Id.ToString();

            var tokens = NewAuthTokens;
            tokens.UserId = authSession.UserAuthId;
            tokens.Provider = provider;

            return Sut.CreateOrMergeAuthSession(authSession, tokens);
        }

        private static UserAuth NewUserAuth => new AppUser
        {
            AppName = nameof(AppUser.AppName),
            Address = nameof(UserAuth.Address),
            Address2 = nameof(UserAuth.Address2),
            BirthDate = new DateTime(2001, 4, 24),
            BirthDateRaw = nameof(UserAuth.BirthDateRaw),
            City = nameof(UserAuth.City),
            Company = nameof(UserAuth.Company),
            Country = nameof(UserAuth.Country),
            Culture = nameof(UserAuth.Culture),
            DigestHa1Hash = nameof(UserAuth.DigestHa1Hash),
            DisplayName = nameof(UserAuth.DisplayName),
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
            Meta = new Dictionary<string, string>
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"},
                {"Key3", "Value3"},
            },
        };

        private static AuthUserSession NewAuthUserSession => new AuthUserSession
        {
            Address = nameof(AuthUserSession.Address),
            Address2 = nameof(AuthUserSession.Address2),
            Audiences = new List<string> {"Audience1", "Audience2"},
            AuthProvider = nameof(AuthUserSession.AuthProvider),
            BirthDate = new DateTime(2001, 4, 24),
            BirthDateRaw = nameof(AuthUserSession.BirthDateRaw),
            City = nameof(AuthUserSession.City),
            Company = nameof(AuthUserSession.Company),
            Country = nameof(AuthUserSession.Country),
            Culture = nameof(AuthUserSession.Culture),
            DisplayName = nameof(AuthUserSession.DisplayName),
            Dns = nameof(AuthUserSession.Dns),
            Email = "email@example.com",
            EmailConfirmed = true,
            FirstName = nameof(AuthUserSession.FirstName),
            FullName = nameof(AuthUserSession.FullName),
            FromToken = true,
            FacebookUserId = nameof(AuthUserSession.FacebookUserId),
            Gender = nameof(AuthUserSession.Gender),
            Hash = nameof(AuthUserSession.Hash),
            Language = nameof(AuthUserSession.Language),
            LastName = nameof(AuthUserSession.LastName),
            MailAddress = nameof(AuthUserSession.MailAddress),
            Meta = new Dictionary<string, string>
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"},
                {"Key3", "Value3"},
            },
            Nickname = nameof(AuthUserSession.Nickname),
            Permissions = new List<string> {"First", "Second"},
            PhoneNumber = nameof(AuthUserSession.PhoneNumber),
            PostalCode = nameof(AuthUserSession.PostalCode),
            PrimaryEmail = nameof(AuthUserSession.PrimaryEmail),
            ProfileUrl = nameof(AuthUserSession.ProfileUrl),
            RequestTokenSecret = nameof(AuthTokens.RequestTokenSecret),
            Roles = new List<string> {"Role1", "Role2"},
            Rsa = nameof(AuthUserSession.Rsa),
            ReferrerUrl = nameof(AuthUserSession.ReferrerUrl),
            State = nameof(AuthUserSession.State),
            Scopes = new List<string> {"Scope1", "Scope2"},
            Sequence = nameof(AuthUserSession.Sequence),
            Sid = nameof(AuthUserSession.Sid),
            SecurityStamp = nameof(AuthUserSession.SecurityStamp),
            TimeZone = nameof(AuthUserSession.TimeZone),
            Tag = 1,
            Type = nameof(AuthUserSession.Type),
            TwitterScreenName = nameof(AuthUserSession.TwitterScreenName),
            TwitterUserId = nameof(AuthUserSession.TwitterUserId),
            TwoFactorEnabled = true,
            UserName = nameof(AuthUserSession.UserName),
            UserAuthName = nameof(AuthUserSession.UserAuthName),
            Webpage = nameof(AuthUserSession.Webpage),
        };

        private static AuthTokens NewAuthTokens => new AuthTokens
        {
            Address = nameof(AuthTokens.Address),
            Address2 = nameof(AuthTokens.Address2),
            AccessToken = nameof(AuthTokens.AccessToken),
            AccessTokenSecret = nameof(AuthTokens.AccessTokenSecret),
            BirthDate = new DateTime(2001, 4, 24),
            BirthDateRaw = nameof(UserAuth.BirthDateRaw),
            City = nameof(AuthTokens.City),
            Company = nameof(AuthTokens.Company),
            Country = nameof(AuthTokens.Country),
            Culture = nameof(AuthTokens.Culture),
            DisplayName = nameof(AuthTokens.DisplayName),
            Email = "email@example.com",
            FirstName = nameof(AuthTokens.FirstName),
            FullName = nameof(AuthTokens.FullName),
            Gender = nameof(AuthTokens.Gender),
            Items = new Dictionary<string, string>
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"},
                {"Key3", "Value3"},
            },
            Language = nameof(AuthTokens.Language),
            LastName = nameof(AuthTokens.LastName),
            MailAddress = nameof(AuthTokens.MailAddress),
            Nickname = nameof(AuthTokens.Nickname),
            PhoneNumber = nameof(AuthTokens.PhoneNumber),
            PostalCode = nameof(AuthTokens.PostalCode),
            RefreshToken = nameof(AuthTokens.RefreshToken),
            RequestToken = nameof(AuthTokens.RequestToken),
            RequestTokenSecret = nameof(AuthTokens.RequestTokenSecret),
            State = nameof(AuthTokens.State),
            TimeZone = nameof(AuthTokens.TimeZone),
            UserId = nameof(AuthTokens.UserId),
            UserName = nameof(AuthTokens.UserName)
        };
    }
}