using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public abstract class PasswordHasherTestsBase
    {
        protected ServiceStackHost appHost;

        protected class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(PasswordHasherTestsBase), typeof(PasswordHasherTestsBase).Assembly) {}

            public bool UsePasswordHasher { get; set; }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    DebugMode = true
                });

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(AppSettings),
                    })
                {
                    IncludeRegistrationService = true
                });

                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

                container.Resolve<IAuthRepository>().InitSchema();

                var authRepo = container.Resolve<IAuthRepository>();

                Config.UseSaltedHash = UsePasswordHasher;

                authRepo.CreateUserAuth(new UserAuth
                {
                    UserName = "oldUser",
                    Email = "oldUser@email.com",
                    DisplayName = "Old User",
                    FirstName = "Old",
                    LastName = "User",
                }, "oldpass");

                Config.UseSaltedHash = !UsePasswordHasher;

                authRepo.CreateUserAuth(new UserAuth
                {
                    UserName = "newUser",
                    Email = "newUser@email.com",
                    DisplayName = "New User",
                    FirstName = "New",
                    LastName = "User",
                }, "newpass");
            }
        }

        protected readonly IUserAuth origNewUser;
        protected readonly IUserAuth origOldUser;

        protected PasswordHasherTestsBase()
        {
            appHost = CreateAppHost();

            origNewUser = appHost.GetAuthRepository().GetUserAuthByUserName("newUser");
            origOldUser = appHost.GetAuthRepository().GetUserAuthByUserName("oldUser");
        }

        protected abstract ServiceStackHost CreateAppHost();

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        protected virtual JsonServiceClient CreateClient() => new JsonServiceClient(Config.ListeningOn);

        protected void AssertUsedNewPasswordHasher(IUserAuth user)
        {
            Assert.That(user.PasswordHash != null && user.Salt == null);

            byte[] decodedHashedPassword = Convert.FromBase64String(user.PasswordHash);

            Assert.That(decodedHashedPassword[0], Is.EqualTo(0x01));
            Assert.That(appHost.TryResolve<IPasswordHasher>().Version, Is.EqualTo(0x01));
        }

        protected void AssertUsedOldSaltedHash(IUserAuth user) => Assert.That(user.PasswordHash != null && user.Salt != null);
    }

    class PasswordHasherUpgradeTests : PasswordHasherTestsBase
    {
        protected override ServiceStackHost CreateAppHost() => new AppHost
            {         
                UsePasswordHasher = true,
            }
            .Init()
            .Start(Config.ListeningOn);

        [Test]
        public void Does_use_old_SaltedHash_for_oldUser()
        {
            AssertUsedOldSaltedHash(origOldUser);
        }

        [Test]
        public void Does_use_new_PasswordHasher_for_newUser()
        {
            AssertUsedNewPasswordHasher(origNewUser);
        }

        [Test]
        public void Can_authenticate_with_oldUser_which_upgrade_to_PasswordHash()
        {
            var client = CreateClient();

            var response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "oldUser",
                Password = "oldpass",
            });

            Assert.That(response.DisplayName, Is.EqualTo("Old User"));

            var oldUserAfterAuth = appHost.GetAuthRepository().GetUserAuthByUserName("oldUser");
            AssertUsedNewPasswordHasher(oldUserAfterAuth);

            //Can re-auth after password hash upgrade
            response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "oldUser",
                Password = "oldpass",
            });

            Assert.That(response.DisplayName, Is.EqualTo("Old User"));
            AssertUsedNewPasswordHasher(oldUserAfterAuth);
        }

        [Test]
        public void Can_Autenticate_with_newUser_which_retains_new_PasswordHash()
        {
            var client = CreateClient();

            var response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "newUser",
                Password = "newpass",
            });

            Assert.That(response.DisplayName, Is.EqualTo("New User"));

            var newUserAfterAuth = appHost.GetAuthRepository().GetUserAuthByUserName("newUser");
            AssertUsedNewPasswordHasher(newUserAfterAuth);
        }

        [Test]
        public void New_registered_user_uses_new_PasswordHash()
        {
            var client = CreateClient();

            var response = client.Post(new Register
            {
                UserName = "newUser2",
                Email = "newUser2@email.com",
                DisplayName = "New User2",
                FirstName = "New2",
                LastName = "User",
                Password = "newpass2"
            });

            var newUser2 = appHost.GetAuthRepository().GetUserAuthByUserName("newUser2");
            AssertUsedNewPasswordHasher(newUser2);

            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "newUser2",
                Password = "newpass2",
            });
        }
    }

    public class PasswordHasherDowngradeTests : PasswordHasherTestsBase
    {
        protected override ServiceStackHost CreateAppHost() => new AppHost
            {
                UsePasswordHasher = false,
            }
            .Init()
            .Start(Config.ListeningOn);

        [Test]
        public void Does_use_new_PasswordHasher_for_oldUser()
        {
            AssertUsedNewPasswordHasher(origOldUser);
        }

        [Test]
        public void Does_use_old_SaltedHash_for_newUser()
        {
            AssertUsedOldSaltedHash(origNewUser);
        }

        [Test]
        public void Can_authenticate_with_oldUser_which_downgrades_to_SaltedHash()
        {
            var client = CreateClient();

            var response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "oldUser",
                Password = "oldpass",
            });

            Assert.That(response.DisplayName, Is.EqualTo("Old User"));

            var oldUserAfterAuth = appHost.GetAuthRepository().GetUserAuthByUserName("oldUser");
            AssertUsedOldSaltedHash(oldUserAfterAuth);

            //Can re-auth after password hash downgrade
            response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "oldUser",
                Password = "oldpass",
            });

            Assert.That(response.DisplayName, Is.EqualTo("Old User"));
            AssertUsedOldSaltedHash(oldUserAfterAuth);
        }

        [Test]
        public void New_registered_user_uses_old_SaltedHash()
        {
            var client = CreateClient();

            var response = client.Post(new Register
            {
                UserName = "newUser2",
                Email = "newUser2@email.com",
                DisplayName = "New User2",
                FirstName = "New2",
                LastName = "User",
                Password = "newpass2"
            });

            var newUser2 = appHost.GetAuthRepository().GetUserAuthByUserName("newUser2");
            AssertUsedOldSaltedHash(newUser2);

            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "newUser2",
                Password = "newpass2",
            });
        }
    }
}