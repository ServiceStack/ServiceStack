using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class CustomUserAuth : IUserAuth
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Company { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string BirthDateRaw { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string Culture { get; set; }
    public string FullName { get; set; }
    public string Gender { get; set; }
    public string Language { get; set; }
    public string MailAddress { get; set; }
    public string Nickname { get; set; }
    public string PostalCode { get; set; }
    public string TimeZone { get; set; }
    public Dictionary<string, string> Meta { get; set; }
    public string PrimaryEmail { get; set; }
    public string Salt { get; set; }
    public string PasswordHash { get; set; }
    public string DigestHa1Hash { get; set; }
    public List<string> Roles { get; set; }
    public List<string> Permissions { get; set; }
    public int? RefId { get; set; }
    public string RefIdStr { get; set; }
    public int InvalidLoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public DateTime? LockedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class CustomUserAuthDetails : IUserAuthDetails
{
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Company { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string BirthDateRaw { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string Culture { get; set; }
    public string FullName { get; set; }
    public string Gender { get; set; }
    public string Language { get; set; }
    public string MailAddress { get; set; }
    public string Nickname { get; set; }
    public string PostalCode { get; set; }
    public string TimeZone { get; set; }
    public string Provider { get; set; }
    public string UserId { get; set; }
    public string AccessToken { get; set; }
    public string AccessTokenSecret { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string RequestToken { get; set; }
    public string RequestTokenSecret { get; set; }
    public Dictionary<string, string> Items { get; set; }
    public Dictionary<string, string> Meta { get; set; }
    public int Id { get; set; }
    public int UserAuthId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int? RefId { get; set; }
    public string RefIdStr { get; set; }
}

[DataContract]
public class CustomAuthUserSession : AuthUserSession
{
    [DataMember]
    public string CustomField { get; set; }
}
    
public class CustomOrmLiteAuthRepositoryTests
{
    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(CustomOrmLiteAuthRepositoryTests), typeof(CustomOrmLiteAuthRepositoryTests).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = true,
            });

            Plugins.Add(new AuthFeature(() => new CustomAuthUserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(AppSettings),
                }) {
                IncludeRegistrationService = true
            });

            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository<CustomUserAuth, CustomUserAuthDetails>(c.Resolve<IDbConnectionFactory>()));

            container.Resolve<IAuthRepository>().InitSchema();

            container.RegisterAs<RegistrationValidator, IValidator<Register>>();

            var authRepo = container.Resolve<IAuthRepository>();
            var userAuth = authRepo.CreateUserAuth(new CustomUserAuth
            {
                UserName = "admin",
                Email = "admin@if.com",
                DisplayName = "Admin User",
                FirstName = "Admin",
                LastName = "User",
                Roles = new List<string> { RoleNames.Admin }
            }, "p@55w0rd");

            userAuth = authRepo.GetUserAuth(userAuth.Id.ToString());
            Assert.That(userAuth, Is.Not.Null);
            Assert.That(userAuth.UserName, Is.EqualTo("admin"));
        }
    }

    private readonly ServiceStackHost appHost;

    public CustomOrmLiteAuthRepositoryTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_register_user_using_Custom_UserAuth_and_UserAuthDetails()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        var response = client.Post(new Register
        {
            UserName = "user",
            Password = "pass",
            Email = "as@if.com",
            DisplayName = "DisplayName",
            FirstName = "FirstName",
            LastName = "LastName",
        });

        Assert.That(response.UserId, Is.Not.Null);
    }

    [Test]
    public void Can_assign_roles_to_Custom_UserAuth()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        client.Post(new Register
        {
            UserName = "user2",
            Password = "pass2",
            Email = "as2@if.com",
            DisplayName = "DisplayName2",
            FirstName = "FirstName2",
            LastName = "LastName2",
        });

        client.Post(new Authenticate
        {
            provider = "credentials",
            UserName = "admin",
            Password = "p@55w0rd",
            RememberMe = true
        });

        var response = client.Post(new AssignRoles
        {
            UserName = "user2",
            Roles = new List<string> { "role1", "role2" },
            Permissions = new List<string> { "perm1", "perm2" },
        });

        Assert.That(response.AllRoles, Is.EquivalentTo(new[] { "role1", "role2" }));
        Assert.That(response.AllPermissions, Is.EquivalentTo(new[] { "perm1", "perm2" }));

        var currentRoles = client.Post(new UnAssignRoles
        {
            UserName = "user2",
            Roles = new List<string> { "role1" },
            Permissions = new List<string> { "perm2" },
        });

        Assert.That(currentRoles.AllRoles, Is.EquivalentTo(new[] { "role2" }));
        Assert.That(currentRoles.AllPermissions, Is.EquivalentTo(new[] { "perm1" }));
    }
}