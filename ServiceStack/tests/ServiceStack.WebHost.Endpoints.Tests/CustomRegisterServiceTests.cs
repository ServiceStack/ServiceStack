using System;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RegisterUser : Register
    {
        public string NetworkName { get; set; }
    }

    public class MyUser : UserAuth
    {
        public string NetworkName { get; set; }
    }

    public class RegisterUserService : RegisterUserAuthServiceBase
    {
        public async Task<object> PostAsync(RegisterUser request)
        {
            if (string.IsNullOrEmpty(request.NetworkName))
                throw new ArgumentNullException(nameof(request.NetworkName));
            
            var session = await GetSessionAsync();
            if (await UserExistsAsync(session))
                throw new NotSupportedException("You're already registered");

            var newUser = (MyUser)ToUser(request);
            newUser.NetworkName = request.NetworkName;

            await ValidateAndThrowAsync(request);
            var user = await AuthRepositoryAsync.CreateUserAuthAsync(newUser, request.Password);
            await RegisterNewUserAsync(session, user);
            
            var response = await CreateRegisterResponse(session, 
                request.UserName ?? request.Email, request.Password, request.AutoLogin);
            return response;
        }
    }

    public class CustomRegisterServiceTests
    {
        private ServiceStackHost appHost;

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(CustomRegisterServiceTests), typeof(RegisterUserService).Assembly) { }
            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                        AutoDisposeConnection = false,
                    });

                container.Register<IAuthRepository>(c => 
                    new OrmLiteAuthRepository<MyUser,UserAuthDetails>(c.Resolve<IDbConnectionFactory>()));
                container.Resolve<IAuthRepository>().InitSchema();
                
                Plugins.Add(new AuthFeature(new CredentialsAuthProvider(AppSettings)));
                // Plugins.Add(new ValidationFeature());
            }
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp() => appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();
        
        const string Password = "p@55wOrd";

        [Test]
        public void Can_register_Custom_User_and_Register_Service()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Send(new RegisterUser {
                Email = "user@gmail.com",
                DisplayName = "Test User",
                FirstName = "Test",
                LastName = "User",
                NetworkName = nameof(RegisterUser),
                Password = Password,
                ConfirmPassword = Password,
            });
            
            var authRepo = appHost.TryResolve<IAuthRepository>();
            var newUser = (MyUser)authRepo.GetUserAuth(response.UserId);
            
            Assert.That(newUser.Email, Is.EqualTo("user@gmail.com"));
            Assert.That(newUser.NetworkName, Is.EqualTo(nameof(RegisterUser)));

            var authResponse = client.Send(new Authenticate {
                provider = "credentials",
                UserName = newUser.Email,
                Password = Password,
                RememberMe = true,
            });
            Assert.That(authResponse.DisplayName, Is.EqualTo("Test User"));
        }

        [Test]
        public void Does_apply_custom_validation()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            try
            {
                var response = client.Send(new RegisterUser {
                    Email = "user@gmail.com",
                    DisplayName = "Test User",
                    FirstName = "Test",
                    LastName = "User",
                    Password = Password,
                    ConfirmPassword = Password,
                });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                var status = ex.GetResponseStatus();
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ArgumentNullException)));
                Assert.That(status.Message, Does.StartWith("Value cannot be null."));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo(nameof(MyUser.NetworkName)));
            }
        }
        
        [Test]
        public void Does_apply_existing_Register_validation()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            try
            {
                var response = client.Send(new RegisterUser {
                    NetworkName = nameof(RegisterUser),
                });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                var status = ex.GetResponseStatus();
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidationException)));
                Assert.That(status.Errors.First(x => x.FieldName == nameof(Register.Password)).ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors.First(x => x.FieldName == nameof(Register.UserName)).ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors.First(x => x.FieldName == nameof(Register.Email)).ErrorCode, Is.EqualTo("NotEmpty"));
            }
        }
        
    }
}