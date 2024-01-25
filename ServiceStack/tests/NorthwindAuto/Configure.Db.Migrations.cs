using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp;
using MyApp.Data;
using MyApp.Migrations;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(ConfigureDbMigrations))]

namespace MyApp;

public class ConfigureDbMigrations : IHostingStartup
{
    public static List<string> UserIds =
    [
        "6237F4BB-867D-419D-AC20-5C2BAC657B0E",
        "59A4E19B-A9E2-462E-BC3B-7CE9B372C222",
        "F2F0F6A0-5F0A-4F6C-9F0B-8F6F5E7F6F5E",
        "0623A397-8174-406E-A2F2-F2D33D084BA1",
    ];
    
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(afterAppHostInit:appHost => {
            var migrator = new Migrator(appHost.Resolve<IDbConnectionFactory>(), typeof(Migration1000).Assembly);
            AppTasks.Register("migrate", _ =>
            {
                var log = appHost.GetApplicationServices().GetRequiredService<ILogger<ConfigureDbMigrations>>();

                log.LogInformation("Running EF Migrations...");
                var scopeFactory = appHost.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                using var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                db.Database.Migrate();
                
                // Only seed users if DB was just created
                if (!dbContext.Users.Any())
                {
                    log.LogInformation("Adding Seed Users...");
                    AddSeedUsers(scope.ServiceProvider).Wait();
                }

                using var conn = migrator.DbFactory.Open();
                ConfigureDb.SeedData(conn);

                log.LogInformation("Running OrmLite Migrations...");
                migrator.Run();
            });
            AppTasks.Register("migrate.revert", args => migrator.Revert(args[0]));
            AppTasks.Run();
        });
    
    public static async Task AddSeedUsers(IServiceProvider services)
    {
        //initializing custom roles 
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        string[] allRoles = [Roles.Admin, Roles.Manager, Roles.Employee];

        void assertResult(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        async Task EnsureUserAsync(ApplicationUser user, string password, string[]? roles = null)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email!);
            if (existingUser != null) return;

            await userManager!.CreateAsync(user, password);
            if (roles?.Length > 0)
            {
                var newUser = await userManager.FindByEmailAsync(user.Email!);
                assertResult(await userManager.AddToRolesAsync(user, roles));
            }
        }

        foreach (var roleName in allRoles)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                //Create the roles and seed them to the database
                assertResult(await roleManager.CreateAsync(new IdentityRole(roleName)));
            }
        }

        await EnsureUserAsync(new ApplicationUser
        {
            Id = UserIds[0],
            DisplayName = "Test User",
            Email = "test@email.com",
            UserName = "test@email.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/user1.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            Id = UserIds[1],
            DisplayName = "Test Employee",
            Email = "employee@email.com",
            UserName = "employee@email.com",
            FirstName = "Test",
            LastName = "Employee",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/user2.svg",
        }, "p@55wOrd", [Roles.Employee]);

        await EnsureUserAsync(new ApplicationUser
        {
            Id = UserIds[2],
            DisplayName = "Test Manager",
            Email = "manager@email.com",
            UserName = "manager@email.com",
            FirstName = "Test",
            LastName = "Manager",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/user3.svg",
        }, "p@55wOrd", [Roles.Manager, Roles.Employee]);

        await EnsureUserAsync(new ApplicationUser
        {
            Id = UserIds[3],
            DisplayName = "Admin User",
            Email = "admin@email.com",
            UserName = "admin@email.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
        }, "p@55wOrd", allRoles);
    }

}
