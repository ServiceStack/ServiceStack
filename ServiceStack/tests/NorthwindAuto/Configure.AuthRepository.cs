using Bogus;
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

public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Employee = nameof(Employee);
    public const string Manager = nameof(Manager);

    public static string[] All { get; set; } = { Admin, Employee, Manager };
}

// Custom User Table with extended Metadata properties

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
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddSingleton<IAuthRepository>(c =>
            new OrmLiteAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IDbConnectionFactory>()) {
                UseDistinctRoleTables = true
            }))
        .ConfigureAppHost(appHost => {
            var authRepo = appHost.Resolve<IAuthRepository>();

            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            db.DropTable<AppUser>();
            db.DropTable<UserAuthDetails>();
            db.DropTable<UserAuthRole>();
            authRepo.InitSchema();

            CreateUsers(authRepo);

            //Populate with lots of demo users
            // for (var i = 1; i < 102; i++)
            // {
            //     CreateUser(authRepo, $"employee{i}@email.com", $"Employee {i}", "p@55wOrd", roles: new[] { "Employee" });
            // }

            // Removing unused UserName in Admin Users UI 
            appHost.Plugins.Add(new ServiceStack.Admin.AdminUsersFeature {
                // Show custom fields in Search Results
                QueryUserAuthProperties = new() {
                    nameof(AppUser.Id),
                    nameof(AppUser.Email),
                    nameof(AppUser.DisplayName),
                    nameof(AppUser.Department),
                    nameof(AppUser.CreatedDate),
                    nameof(AppUser.LastLoginDate),
                },

                QueryMediaRules = new()
                {
                    MediaRules.ExtraSmall.Show<AppUser>(x => new { x.Id, x.Email, x.DisplayName }),
                    MediaRules.Small.Show<AppUser>(x => x.Department),
                },

                // Add Custom Fields to Create/Edit User Forms
                FormLayout = new() {
                    Input.For<AppUser>(x => x.Email),
                    Input.For<AppUser>(x => x.DisplayName),
                    Input.For<AppUser>(x => x.Company),
                    Input.For<AppUser>(x => x.Department, c => c.FieldsPerRow(2)),
                    Input.For<AppUser>(x => x.PhoneNumber, c => {
                        c.Type = Input.Types.Tel;
                        c.FieldsPerRow(2);
                    }),
                    Input.For<AppUser>(x => x.Nickname, c => {
                        c.Help = "Public alias (3-12 lower alpha numeric chars)";
                        c.Pattern = "^[a-z][a-z0-9_.-]{3,12}$";
                        //c.Required = true;
                    }),
                    Input.For<AppUser>(x => x.ProfileUrl, c => c.Type = Input.Types.Url),
                    Input.For<AppUser>(x => x.IsArchived), Input.For<AppUser>(x => x.ArchivedDate),
                }
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
    public void CreateUsers(IAuthRepository authRepo) => new List<SeedUser>
    {
        new("admin@email.com", "Admin User", Roles: new[] { RoleNames.Admin }),
        new("manager@email.com", "The Manager", Roles: new[] { AppRoles.Employee, AppRoles.Manager }),
        new("employee@email.com", "A Employee", Roles: new[] { AppRoles.Employee }),
        new("employee1@email.com", "Employee 2", Roles: new[] { AppRoles.Employee }),
        new("employee2@email.com", "Employee 3", Roles: new[] { AppRoles.Employee }),
    }.ForEach(user => CreateUser(authRepo, user));
    
    public void CreateUser(IAuthRepository authRepo, SeedUser user)
    {
        if (authRepo.GetUserAuthByUserName(user.Email) == null)
        {
            var newUser = appUserFaker.Generate();
            newUser.Email = user.Email;
            newUser.DisplayName = user.Name;
            var dbUser = (AppUser)authRepo.CreateUserAuth(newUser, user.Password);
            newUser.ProfileUrl = $"/profiles/users/{newUser.Id}.jpg";
            authRepo.UpdateUserAuth(dbUser, newUser);
            authRepo.AssignRoles(newUser, user.Roles);
        }
    }
}