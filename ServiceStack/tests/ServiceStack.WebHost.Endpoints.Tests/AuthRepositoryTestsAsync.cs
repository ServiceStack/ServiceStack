using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Testing;
using MongoDB.Driver;
using ServiceStack.Authentication.MongoDb;

using ServiceStack.Authentication.RavenDb;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using Raven.Client.Documents;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MemoryAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        container.Register<IAuthRepository>(c => new InMemoryAuthRepository<AppUser,AppUserDetails>());
    }
}
    
public class UserAuthRepositoryAsyncWrapperTests : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        container.Register<IAuthRepositoryAsync>(c => 
            new UserAuthRepositoryAsyncWrapper(new InMemoryAuthRepository<AppUser,AppUserDetails>()));
    }
}
    
public class RedisAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        container.Register<IRedisClientsManager>(c => new RedisManagerPool());
        container.Register<IAuthRepository>(c => 
            new RedisAuthRepository<AppUser,AppUserDetails>(c.Resolve<IRedisClientsManager>()));
    }
}
    
public class OrmLiteAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        container.Register<IDbConnectionFactory>(c =>
            new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                AutoDisposeConnection = false,
            });

        container.Register<IAuthRepository>(c => 
            new OrmLiteAuthRepository<AppUser,AppUserDetails>(c.Resolve<IDbConnectionFactory>()));
    }
}
    
public class OrmLiteAuthRepositoryDistinctRolesTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        container.Register<IDbConnectionFactory>(c =>
            new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                AutoDisposeConnection = false,
            });

        container.Register<IAuthRepository>(c => 
            new OrmLiteAuthRepository<AppUser,AppUserDetails>(c.Resolve<IDbConnectionFactory>()) {
                UseDistinctRoleTables = true,
            });
    }
}

[NUnit.Framework.Ignore("Requires RavenDB")]
public class RavenDbAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        var store = new DocumentStore
        {
            Urls = new[]                        // URL to the Server,
            {                                   // or list of URLs 
                "http://localhost:8080"         // to all Cluster Servers (Nodes)
            },
            Database = "test",                  // Default database that DocumentStore will interact with
            Conventions = {
            }
        };
        store.Conventions.FindIdentityProperty = RavenDbUserAuthRepository.FindIdentityProperty;
            
        container.AddSingleton(store.Initialize());

        container.Register<IAuthRepository>(c =>
            new RavenDbUserAuthRepository<AppUser, AppUserDetails>(c.Resolve<IDocumentStore>()));

        var response = $"http://localhost:8080/databases/test/queries?allowStale=False&maxOpsPerSec=&details=False"
            .SendStringToUrl(HttpMethods.Delete, "{\"Query\":\"from AppUsers\",\"QueryParameters\":null}");
    }
}

[NUnit.Framework.Ignore("Requires MongoDB")]
public class MongoDbAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public override void ConfigureAuthRepo(Container container)
    {
        var mongoClient = new MongoClient();
        mongoClient.DropDatabase("MyApp");
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase("MyApp");

        container.AddSingleton(mongoDatabase);
        container.AddSingleton<IAuthRepository>(c => 
            new MongoDbAuthRepository(c.Resolve<IMongoDatabase>(), createMissingCollections:true));
    }
}

[NUnit.Framework.Ignore("Requires DynamoDB")]
public class DynamoDbAuthRepositoryTestsAsync : AuthRepositoryTestsAsyncBase
{
    public static IPocoDynamo CreatePocoDynamo()
    {
        var dynamoClient = CreateDynamoDbClient();
        var db = new PocoDynamo(dynamoClient);
        return db;
    }

    public static string DynamoDbUrl = Environment.GetEnvironmentVariable("CI_DYNAMODB") 
                                       ?? "http://localhost:8000";

    public static AmazonDynamoDBClient CreateDynamoDbClient()
    {
        var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
            ServiceURL = DynamoDbUrl,
        });
        return dynamoClient;
    }

    public override void ConfigureAuthRepo(Container container)
    {
        var db = CreatePocoDynamo();
        container.AddSingleton(db);
        container.Register<IAuthRepository>(c => 
            new DynamoDbAuthRepository<AppUser,AppUserDetails>(c.Resolve<IPocoDynamo>()));
    }
}
    
public abstract class AuthRepositoryTestsAsyncBase
{
    private ServiceStackHost appHost;

    public abstract void ConfigureAuthRepo(Container container); 
        
    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new BasicAppHost(typeof(AuthRepositoryQueryTestsBase).Assembly) 
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] {
                    new CredentialsAuthProvider(), 
                })
                {
                    IncludeRegistrationService = true,
                });
                    
                host.Plugins.Add(new SharpPagesFeature());
            },
            ConfigureContainer = container => {
                ConfigureAuthRepo(container);
                var authRepo = container.TryResolve<IAuthRepositoryAsync>()
                               ?? container.TryResolve<IAuthRepository>() as IAuthRepositoryAsync
                               ?? new UserAuthRepositoryAsyncWrapper(container.TryResolve<IAuthRepository>());
                    
                if (authRepo is IClearableAsync clearable)
                {
                    try { clearable.ClearAsync().Wait(); } catch {}
                }

                authRepo.InitSchema();
            }
        }.Init();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    const string Password = "p@55wOrd";

    [Test]
    public async Task Can_CreateUserAuth()
    {
        var authRepo = appHost.GetAuthRepositoryAsync();

        var newUser = await authRepo.CreateUserAuthAsync(new AppUser
        {
            DisplayName = "Test User",
            Email = "user@gmail.com",
            FirstName = "Test",
            LastName = "User",
        }, Password);
            
        Assert.That(newUser.Email, Is.EqualTo("user@gmail.com"));

        var fromDb = await authRepo.GetUserAuthAsync((newUser as AppUser)?.Key ?? newUser.Id.ToString());
        Assert.That(fromDb.Email, Is.EqualTo("user@gmail.com"));

        newUser.FirstName = "Updated";
        await authRepo.SaveUserAuthAsync(newUser);
            
        var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
        newSession.PopulateSession(newUser);

        var updatedUser = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.FirstName, Is.EqualTo("Updated"));

        var authUser = await authRepo.TryAuthenticateAsync(newUser.Email, Password);
        Assert.That(authUser, Is.Not.Null);
        Assert.That(authUser.FirstName, Is.EqualTo(updatedUser.FirstName));
            
        await authRepo.DeleteUserAuthAsync(newSession.UserAuthId);
        var deletedUserAuth = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
        Assert.That(deletedUserAuth, Is.Null);
    }

    [Test]
    public async Task Can_AddUserAuthDetails()
    {
        var authRepo = appHost.GetAuthRepositoryAsync();
            
        var newUser = await authRepo.CreateUserAuthAsync(new AppUser
        {
            DisplayName = "Facebook User",
            Email = "user@fb.com",
            FirstName = "Face",
            LastName = "Book",
        }, "p@55wOrd");
            
        var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
        newSession.PopulateSession(newUser);
        Assert.That(newSession.Email, Is.EqualTo("user@fb.com"));

        var fbAuthTokens = new AuthTokens
        {
            Provider = FacebookAuthProvider.Name,
            AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
            UserId = "123456",
            DisplayName = "FB User",
            FirstName = "FB",
            LastName = "User",
            Email = "user@fb.com",
        };
            
        var userAuthDetails = await authRepo.CreateOrMergeAuthSessionAsync(newSession, fbAuthTokens);
        Assert.That(userAuthDetails.Email, Is.EqualTo("user@fb.com"));

        var userAuthDetailsList = await authRepo.GetUserAuthDetailsAsync(newSession.UserAuthId);
        Assert.That(userAuthDetailsList.Count, Is.EqualTo(1));
        Assert.That(userAuthDetailsList[0].Email, Is.EqualTo("user@fb.com"));
            
        await authRepo.DeleteUserAuthAsync(newSession.UserAuthId);
        userAuthDetailsList = await authRepo.GetUserAuthDetailsAsync(newSession.UserAuthId);
        Assert.That(userAuthDetailsList, Is.Empty);
        var deletedUserAuth = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
        Assert.That(deletedUserAuth, Is.Null);
    }
}