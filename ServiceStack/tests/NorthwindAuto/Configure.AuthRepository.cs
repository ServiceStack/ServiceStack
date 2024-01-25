/*
using System.Data;
using Bogus;
using MyApp.Data;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using TalentBlazor.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuthRepository))]

namespace MyApp;

// Custom User Table with extended Metadata properties

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path fill='currentColor' d='M10.277 2.084a.5.5 0 0 0-.554 0a15.05 15.05 0 0 1-6.294 2.421A.5.5 0 0 0 3 5v4.5c0 3.891 2.307 6.73 6.82 8.467a.5.5 0 0 0 .36 0C14.693 16.23 17 13.39 17 9.5V5a.5.5 0 0 0-.43-.495a15.05 15.05 0 0 1-6.293-2.421ZM10 9.5a2 2 0 1 1 0-4a2 2 0 0 1 0 4Zm0 5c-2.5 0-3.5-1.25-3.5-2.5A1.5 1.5 0 0 1 8 10.5h4a1.5 1.5 0 0 1 1.5 1.5c0 1.245-1 2.5-3.5 2.5Z'/></svg>")]
public class AppUser : IUserAuth
{
    [AutoIncrement]
    public int Id { get; set; }
    public string DisplayName { get; set; }

    [Index]
    [Format(FormatMethods.LinkEmail)]
    public string? Email { get; set; }

    // Custom Properties
    [Format(FormatMethods.IconRounded)]
    public string ProfileUrl { get; set; }

    public Department Department { get; set; }
    public string? Title { get; set; }
    public string? JobArea { get; set; }
    public string? Location { get; set; }
    public int Salary { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? About { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string? LastLoginIp { get; set; }

    // UserAuth Properties
    public string? UserName { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Company { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? BirthDateRaw { get; set; }
    public string? Address { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Culture { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Language { get; set; }
    public string? MailAddress { get; set; }
    public string? Nickname { get; set; }
    public string? PostalCode { get; set; }
    public string? TimeZone { get; set; }
    public string? Salt { get; set; }
    public string? PasswordHash { get; set; }
    public string? DigestHa1Hash { get; set; }
    public List<string>? Roles { get; set; } = new();
    public List<string>? Permissions { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int InvalidLoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public DateTime? LockedDate { get; set; }
    public string? RecoveryToken { get; set; }

    //Custom Reference Data
    public int? RefId { get; set; }
    public string? RefIdStr { get; set; }
    public Dictionary<string, string>? Meta { get; set; }
}

public class AppUserAuthEvents : AuthEvents
{
    public override async Task OnAuthenticatedAsync(IRequest httpReq, IAuthSession session, IServiceBase authService,
        IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
    {
        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(httpReq);
        using (authRepo as IDisposable)
        {
            var userAuth = (AppUser)await authRepo.GetUserAuthAsync(session.UserAuthId, token);
            userAuth.ProfileUrl = session.GetProfileUrl();
            userAuth.LastLoginIp = httpReq.UserHostAddress;
            userAuth.LastLoginDate = DateTime.UtcNow;
            await authRepo.SaveUserAuthAsync(userAuth, token);
        }
    }
}

public class ConfigureAuthRepository : IHostingStartup
{
    public static void RecreateUsers(IAuthRepository authRepo, IDbConnection db)
    {
        db.DropTable<UserAuthDetails>();
        db.DropTable<UserAuthRole>();
        db.DropTable<AppUser>();
        db.CreateTable<AppUser>();
        db.CreateTable<UserAuthRole>();
        db.CreateTable<UserAuthDetails>();

        authRepo.InitSchema();
        CreateUsers(authRepo);
    }
    
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddSingleton<IAuthRepository>(c =>
            new OrmLiteAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IDbConnectionFactory>()) {
                UseDistinctRoleTables = true
            }))
        .ConfigureAppHost(appHost => {
            // Executed in Configure.Db
            //var authRepo = appHost.Resolve<IAuthRepository>();
            //using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            //RecreateUsers(authRepo, db);

            //Populate with lots of demo users
            // for (var i = 1; i < 102; i++)
            // {
            //     CreateUser(authRepo, $"employee{i}@email.com", $"Employee {i}", "p@55wOrd", roles: new[] { "Employee" });
            // }

            // Removing unused UserName in Admin Users UI 
            appHost.Plugins.Add(new ServiceStack.Admin.AdminUsersFeature {
                // Show custom fields in Search Results
                QueryUserAuthProperties =
                [
                    nameof(AppUser.Id),
                    nameof(AppUser.Email),
                    nameof(AppUser.DisplayName),
                    nameof(AppUser.Department),
                    nameof(AppUser.CreatedDate),
                    nameof(AppUser.LastLoginDate)
                ],

                QueryMediaRules =
                [
                    MediaRules.ExtraSmall.Show<AppUser>(x => new { x.Id, x.Email, x.DisplayName }),
                    MediaRules.Small.Show<AppUser>(x => x.Department)
                ],

                // Add Custom Fields to Create/Edit User Forms
                FormLayout =
                [
                    Input.For<AppUser>(x => x.Email),
                    Input.For<AppUser>(x => x.DisplayName),
                    Input.For<AppUser>(x => x.Company),
                    Input.For<AppUser>(x => x.Department, c => c.FieldsPerRow(2)),
                    Input.For<AppUser>(x => x.PhoneNumber, c =>
                    {
                        c.Type = Input.Types.Tel;
                        c.FieldsPerRow(2);
                    }),
                    Input.For<AppUser>(x => x.Nickname, c =>
                    {
                        c.Help = "Public alias (3-12 lower alpha numeric chars)";
                        c.Pattern = "^[a-z][a-z0-9_.-]{3,12}$";
                        //c.Required = true;
                    }),
                    Input.For<AppUser>(x => x.ProfileUrl, c => c.Type = Input.Types.Url),
                    Input.For<AppUser>(x => x.IsArchived), Input.For<AppUser>(x => x.ArchivedDate)
                ]
            });

        },
        afterConfigure: appHost => {
            appHost.AssertPlugin<AuthFeature>().AuthEvents.Add(new AppUserAuthEvents());
        });

    private static Faker<AppUser> appUserFaker = new Faker<AppUser>()
        .RuleFor(a => a.About, faker => faker.Lorem.Paragraph())
        .RuleFor(a => a.Department, faker => faker.Random.Enum<Department>())
        .RuleFor(a => a.BirthDate, faker => faker.Date.Between(new DateTime(1990, 1, 1), new DateTime(1950, 1, 1)))
        .RuleFor(a => a.FirstName, faker => faker.Name.FirstName())
        .RuleFor(a => a.LastName, faker => faker.Name.LastName())
        .RuleFor(a => a.Title, faker => faker.Name.JobTitle())
        .RuleFor(a => a.JobArea, faker => faker.Name.JobArea())
        .RuleFor(a => a.PhoneNumber, faker => faker.Phone.PhoneNumber())
        .RuleFor(a => a.Salary, faker => faker.Random.Int(90, 250) * 1000);

    // Add initial Users to the configured Auth Repository
    public record SeedUser(string Email, string Name, string Password = "p@55wOrd", string[]? Roles = null);
    public static void CreateUsers(IAuthRepository authRepo) => new List<SeedUser>
    {
        new("admin@email.com", "Admin User", Roles: [RoleNames.Admin]),
        new("manager@email.com", "The Manager", Roles: [Roles.Employee, Roles.Manager]),
        new("employee@email.com", "A Employee", Roles: [Roles.Employee]),
        new("employee1@email.com", "Employee 2", Roles: [Roles.Employee]),
        new("employee2@email.com", "Employee 3", Roles: [Roles.Employee]),
        new("test", "Test User", Password:"test"),
    }.ForEach(user => CreateUser(authRepo, user));
    
    public static void CreateUser(IAuthRepository authRepo, SeedUser user)
    {
        if (authRepo.GetUserAuthByUserName(user.Email) == null)
        {
            var newUser = appUserFaker.Generate();
            if (user.Email.Contains('@'))
                newUser.Email = user.Email;
            else
                newUser.UserName = user.Email;
            var dbUser = (AppUser)authRepo.CreateUserAuth(newUser, user.Password);
            newUser.ProfileUrl = $"/profiles/users/{newUser.Id}.jpg";
            authRepo.UpdateUserAuth(dbUser, newUser);
            authRepo.AssignRoles(newUser, user.Roles);
        }
    }
}
*/