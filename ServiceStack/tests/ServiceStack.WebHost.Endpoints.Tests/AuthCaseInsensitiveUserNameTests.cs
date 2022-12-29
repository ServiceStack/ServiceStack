using System;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AuthSaveUserNameAsLowerCaseTests
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AuthCaseInsensitiveUserNameTests), typeof(AuthCaseInsensitiveUserNameTests).Assembly) {}

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
                
                container.Resolve<IAuthRepository>().InitSchema();
                
                Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                    new []
                    {
                        new CredentialsAuthProvider(AppSettings), 
                    })
                {
                    IncludeRegistrationService = true,
                    SaveUserNamesInLowerCase = true,
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public AuthSaveUserNameAsLowerCaseTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_register_and_authenticate_with_exact_UserName()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Exact",
                Email = "exact@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });
            
            var response = client.Post(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "Exact",
                Password = "test"
            });
            
            Assert.That(response.UserName, Is.EqualTo("Exact"));
        }
        
        [Test]
        public void Can_register_and_login_using_different_cases()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Mythz",
                Email = "mythz@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });
            
            var response = client.Post(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "MythZ",
                Password = "test"
            });
            
            Assert.That(response.UserName, Is.EqualTo("MythZ"));
        }
    }
    
    public class AuthCaseInsensitiveUserNameTests
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AuthCaseInsensitiveUserNameTests), typeof(AuthCaseInsensitiveUserNameTests).Assembly) {}

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
                
                container.Resolve<IAuthRepository>().InitSchema();
                
                Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                    new []
                    {
                        new CredentialsAuthProvider(AppSettings), 
                    })
                {
                    IncludeRegistrationService = true,
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public AuthCaseInsensitiveUserNameTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_register_and_authenticate_with_exact_UserName()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Exact",
                Email = "exact@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });
            
            var response = client.Post(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "Exact",
                Password = "test"
            });
            
            Assert.That(response.UserName, Is.EqualTo("Exact"));
        }
        
        [Test]
        public void Can_register_and_login_using_different_cases_using_case_insensitive_fallback()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Mythz",
                Email = "mythz@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });
            
            var response = client.Post(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "MythZ",
                Password = "test"
            });
            
            Assert.That(response.UserName, Is.EqualTo("MythZ"));
        }
    }
        
    public class AuthCaseSensitiveUserNameTests
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AuthCaseInsensitiveUserNameTests), typeof(AuthCaseInsensitiveUserNameTests).Assembly) {}

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())
                    {
                        ForceCaseInsensitiveUserNameSearch = false,
                    });
                
                container.Resolve<IAuthRepository>().InitSchema();
                
                Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                    new []
                    {
                        new CredentialsAuthProvider(AppSettings), 
                    })
                {
                    IncludeRegistrationService = true,                    
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public AuthCaseSensitiveUserNameTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_register_and_authenticate_with_exact_UserName()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Exact",
                Email = "exact@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });
            
            var response = client.Post(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "Exact",
                Password = "test"
            });
            
            Assert.That(response.UserName, Is.EqualTo("Exact"));
        }
        
        [Test]
        public void Can_disable_non_index_fallback()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var registerResponse = client.Post(new Register
            {
                UserName = "Mythz",
                Email = "mythz@gmail.com",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "DisplayName",
                Password = "test"
            });

            try
            {
                var response = client.Post(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "MythZ",
                    Password = "test"
                });
                
                Assert.Fail("Should fail");
            }
            catch (WebServiceException) {}
        }
    }

}