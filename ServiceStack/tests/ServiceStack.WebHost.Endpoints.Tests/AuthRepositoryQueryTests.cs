using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Script;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MemoryAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) =>
        new MemoryAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
public class RedisAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) =>
        new RedisAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
public class OrmLiteAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) =>
        new OrmLiteAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
public class OrmLiteAuthRepositoryQueryDistinctRolesTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        new OrmLiteAuthRepositoryDistinctRolesTests().ConfigureAuthRepo(container);
    }
}
    
[NUnit.Framework.Ignore("Requires RavenDB")]
public class RavenDbAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) => 
        new RavenDbAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
[NUnit.Framework.Ignore("Requires MongoDB")]
public class MongoDbAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) => 
        new MongoDbAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
[NUnit.Framework.Ignore("Requires DynamoDB")]
public class DynamoDbAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
{
    public override void ConfigureAuthRepo(Container container) => 
        new DynamoDbAuthRepositoryTests().ConfigureAuthRepo(container);
}
    
[TestFixture]
public abstract class AuthRepositoryQueryTestsBase
{
    protected ServiceStackHost appHost;

    public abstract void ConfigureAuthRepo(Container container); 
        
    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new BasicAppHost(typeof(AuthRepositoryQueryTestsBase).Assembly) 
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [
                    new CredentialsAuthProvider()
                ])
                {
                    IncludeRegistrationService = true,
                });
                    
                host.Plugins.Add(new SharpPagesFeature());
            },
            ConfigureContainer = container => {
                    
                ConfigureAuthRepo(container);
                    
                var authRepo = container.Resolve<IAuthRepository>();
                if (authRepo is IClearable clearable)
                {
                    try { clearable.Clear(); } catch {}
                }

                authRepo.InitSchema();
                    
                SeedData(authRepo);
            }
        }.Init();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose(); 

    void SeedData(IAuthRepository authRepo)
    {
        var newUser = authRepo.CreateUserAuth(new AppUser
        {
            Id = 1,
            DisplayName = "Test User",
            Email = "user@gmail.com",
            FirstName = "Test",
            LastName = "User",
        }, "p@55wOrd");

        newUser = authRepo.CreateUserAuth(new AppUser
        {
            Id = 2,
            DisplayName = "Test Manager",
            Email = "manager@gmail.com",
            FirstName = "Test",
            LastName = "Manager",
        }, "p@55wOrd");
        authRepo.AssignRoles(newUser, roles:new[]{ "Manager" });

        newUser = authRepo.CreateUserAuth(new AppUser
        {
            Id = 3,
            DisplayName = "Admin User",
            Email = "admin@gmail.com",
            FirstName = "Admin",
            LastName = "Super User",
        }, "p@55wOrd");
        authRepo.AssignRoles(newUser, roles:new[]{ "Admin" });
    }

    private static bool IsRavenDb(IAuthRepository authRepo) => authRepo.GetType().Name.StartsWith("Raven");

    private static void AssertHasIdentity(IAuthRepository authRepo, List<IUserAuth> allUsers)
    {
        Assert.That(IsRavenDb(authRepo)
            ? allUsers.Cast<AppUser>().All(x => x.Key != null)
            : allUsers.All(x => x.Id > 0));
    }

    [Test]
    public void Can_QueryUserAuth_GetUserAuths()
    {
        var authRepo = appHost.GetAuthRepository();
        using (authRepo as IDisposable)
        {
            var allUsers = authRepo.GetUserAuths();
            Assert.That(allUsers.Count, Is.EqualTo(3));
            AssertHasIdentity(authRepo, allUsers);
                
            Assert.That(allUsers.All(x => x.Email != null));
                
            allUsers = authRepo.GetUserAuths(skip:1);
            Assert.That(allUsers.Count, Is.EqualTo(2));
            allUsers = authRepo.GetUserAuths(take:2);
            Assert.That(allUsers.Count, Is.EqualTo(2));
            allUsers = authRepo.GetUserAuths(skip:1,take:2);
            Assert.That(allUsers.Count, Is.EqualTo(2));
        }
    }

    [Test]
    public void Can_QueryUserAuth_GetUserAuths_OrderBy()
    {
        var authRepo = appHost.GetAuthRepository();
        using (authRepo as IDisposable)
        {
            var allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.Id));
            Assert.That(allUsers.Count, Is.EqualTo(3));
            Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));

            var idField = IsRavenDb(authRepo)
                ? nameof(AppUser.Key)
                : nameof(UserAuth.Id);
            allUsers = authRepo.GetUserAuths(orderBy: idField + " DESC");
            Assert.That(allUsers.Count, Is.EqualTo(3));
            Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
            allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.DisplayName));
            Assert.That(allUsers.Count, Is.EqualTo(3));
            Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
                
            allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.Email));
            Assert.That(allUsers.Count, Is.EqualTo(3));
            Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
            allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.CreatedDate) + " DESC");
            Assert.That(allUsers.Count, Is.EqualTo(3));
            Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
        }
    }

    [Test]
    public void Can_QueryUserAuth_SearchUserAuths()
    {
        var authRepo = appHost.GetAuthRepository();
        using (authRepo as IDisposable)
        {
            var allUsers = authRepo.SearchUserAuths("gmail.com");
            Assert.That(allUsers.Count, Is.EqualTo(3));
            AssertHasIdentity(authRepo, allUsers);
            Assert.That(allUsers.All(x => x.Email != null));
                
            allUsers = authRepo.SearchUserAuths(query:"gmail.com",skip:1);
            Assert.That(allUsers.Count, Is.EqualTo(2));
            allUsers = authRepo.SearchUserAuths(query:"gmail.com",take:2);
            Assert.That(allUsers.Count, Is.EqualTo(2));
            allUsers = authRepo.SearchUserAuths(query:"gmail.com",skip:1,take:2);
            Assert.That(allUsers.Count, Is.EqualTo(2));

            if (!IsRavenDb(authRepo)) // RavenDB only searches UserName/Email and only StartsWith/EndsWith
            {
                allUsers = authRepo.SearchUserAuths(query:"Test");
                Assert.That(allUsers.Count, Is.EqualTo(2));

                allUsers = authRepo.SearchUserAuths(query:"Admin");
                Assert.That(allUsers.Count, Is.EqualTo(1));

                allUsers = authRepo.SearchUserAuths(query:"Test",skip:1,take:1,orderBy:nameof(UserAuth.Email));
                Assert.That(allUsers.Count, Is.EqualTo(1));
                Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));
            }
        }
    }

    [Test]
    public void Can_QueryUserAuth_in_Script()
    {
        var context = appHost.AssertPlugin<SharpPagesFeature>();
        Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths() | count }}"), Is.EqualTo("3"));
        Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
        Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));
        Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
        Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));

        Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com',orderBy:'Id'}) | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
        Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com',skip:1,take:1,orderBy:'Id'}) | map => it.Id | join }}"), Is.EqualTo("2"));

        if (!IsRavenDb(appHost.TryResolve<IAuthRepository>()))
        {
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com', orderBy:'LastName DESC' }) | map => it.Id | join }}"), Is.EqualTo("1,3,2"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Email' }) | map => it.Id | join }}"), Is.EqualTo("2,1"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
        }
    }
 
    [Test]
    public void Can_fetch_roles_and_permissions()
    {
        var authRepo = appHost.GetAuthRepository();
        using (authRepo as IDisposable)
        {
            if (authRepo is IManageRoles manageRoles)
            {
                manageRoles.GetRolesAndPermissions("3", 
                    out var roles, out var permissions);
                    
                Assert.That(roles, Is.EquivalentTo(new[] { "Admin" }));
            }
        }
    }
}