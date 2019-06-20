using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Authentication.MongoDb;
using MongoDB.Driver;

namespace CheckWebCore
{
    public class ConfigureMongoDb : IConfigureServices
    {
        IConfiguration Configuration { get; }
        public ConfigureMongoDb(IConfiguration configuration) => Configuration = configuration;

        public void Configure(IServiceCollection services)
        {
            var mongoClient = new MongoClient();
            mongoClient.DropDatabase("MyApp");
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase("MyApp");
            services.AddSingleton(mongoDatabase);
        }
    }    
    
    public class ConfigureAuthRepository : IConfigureAppHost, IConfigureServices
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IAuthRepository>(c => 
                new MongoDbAuthRepository(c.Resolve<IMongoDatabase>(), createMissingCollections:true));
        }

        public void Configure(IAppHost appHost)
        {
            var authRepo = appHost.Resolve<IAuthRepository>();
            authRepo.InitSchema();

            CreateUser(authRepo, "admin@email.com", "Admin User", "p@55wOrd", roles:new[]{ RoleNames.Admin });
        }

        // Add initial Users to the configured Auth Repository
        public void CreateUser(IAuthRepository authRepo, string email, string name, string password, string[] roles)
        {
            if (authRepo.GetUserAuthByUserName(email) == null)
            {
                var newAdmin = new UserAuth { Email = email, DisplayName = name };
                var user = authRepo.CreateUserAuth(newAdmin, password);
                authRepo.AssignRoles(user, roles);
            }
        }
    }
}