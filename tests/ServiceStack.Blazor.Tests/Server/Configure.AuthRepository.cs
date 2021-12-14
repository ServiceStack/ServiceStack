using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using MyApp.Client;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuthRepository))]

namespace MyApp;

public enum Department
{
    None,
    Marketing,
    Accounts,
    Legal,
    HumanResources,
}

// Custom User Table with extended Metadata properties
public class AppUser : UserAuth
{
    public Department Department { get; set; }
    public string? ProfileUrl { get; set; }
    public string? LastLoginIp { get; set; }
    
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }

    public DateTime? LastLoginDate { get; set; }
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
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddSingleton<IAuthRepository>(c =>
            new OrmLiteAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IDbConnectionFactory>()) {
                UseDistinctRoleTables = true
            }))
        .ConfigureAppHost(appHost => {
            var authRepo = appHost.Resolve<IAuthRepository>();
            authRepo.InitSchema();
            CreateUser(authRepo, "admin@email.com", "Admin User", "p@55wOrd", roles: new[] { RoleNames.Admin });
            CreateUser(authRepo, "manager@email.com", "The Manager", "p@55wOrd", roles: new[] { AppRoles.Employee, AppRoles.Manager });
            CreateUser(authRepo, "employee@email.com", "A Employee", "p@55wOrd", roles: new[] { AppRoles.Employee });

            //Populate with lots of demo users
            //for (var i = 1; i < 102; i++)
            //{
            //    CreateUser(authRepo, $"employee{i}@email.com", $"Employee {i}", "p@55wOrd", roles: new[] { AppRoles.Employee });
            //}

            // Removing unused UserName in Admin Users UI 
            appHost.Plugins.Add(new ServiceStack.Admin.AdminUsersFeature {
                // Show custom fields in Search Results
                QueryUserAuthProperties = {
                    nameof(AppUser.Department),
                    nameof(AppUser.LastLoginDate),
                },
                // Add Custom Fields to Create/Edit User Forms
                GridFieldLayout = {
                    new() {
                        Input.For<AppUser>(x => x.Department),
                    },
                    new() { 
                        Input.For<AppUser>(x => x.ProfileUrl, c => c.Type = Input.Types.Url) 
                    },
                    new() {
                        Input.For<AppUser>(x => x.IsArchived), Input.For<AppUser>(x => x.ArchivedDate),
                    },
                }
            }
            // When Display Name already contains both
            .RemoveFromQueryResults(
                nameof(AppUser.FirstName), nameof(AppUser.LastName), nameof(AppUser.ModifiedDate))
            // When using Email as Username
            .RemoveFields(nameof(AppUser.UserName)));
        },
        afterConfigure: appHost => {
            appHost.AssertPlugin<AuthFeature>().AuthEvents.Add(new AppUserAuthEvents());
        });

    // Add initial Users to the configured Auth Repository
    public void CreateUser(IAuthRepository authRepo, string email, string name, string password, string[] roles)
    {
        if (authRepo.GetUserAuthByUserName(email) == null)
        {
            var newAdmin = new AppUser { Email = email, DisplayName = name };
            var user = authRepo.CreateUserAuth(newAdmin, password);
            authRepo.AssignRoles(user, roles);
        }
    }
}