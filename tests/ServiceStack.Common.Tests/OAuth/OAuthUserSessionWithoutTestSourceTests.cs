using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Redis;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.Utils;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture, Explicit("Manual OAuth Test with iteration over data stores")]
    public class OAuthUserSessionWithoutTestSourceTests
    {
        private OAuthUserSessionTests tests;
        private readonly List<IUserAuthRepository> userAuthRepositorys = new List<IUserAuthRepository>();

        OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory(
            ":memory:", false, SqliteOrmLiteDialectProvider.Instance);

        [SetUp]
        public void SetUp()
        {
			try
			{
                tests = new OAuthUserSessionTests();
				var inMemoryRepo = new InMemoryAuthRepository();
				inMemoryRepo.Clear();
				userAuthRepositorys.Add(inMemoryRepo);

                var appSettings = new AppSettings();
				var redisRepo = new RedisAuthRepository(new BasicRedisClientManager(new string[] { appSettings.GetString("Redis.Host") ?? "localhost" }));
				redisRepo.Clear();
				userAuthRepositorys.Add(redisRepo);

				if (OAuthUserSessionTestsBase.UseSqlServer)
				{
					var connStr = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\App_Data\auth.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";
					var sqlServerFactory = new OrmLiteConnectionFactory(connStr, SqlServerOrmLiteDialectProvider.Instance);
					var sqlServerRepo = new OrmLiteAuthRepository(sqlServerFactory);
					sqlServerRepo.DropAndReCreateTables();
				}
				else
				{
					var sqliteInMemoryRepo = new OrmLiteAuthRepository(dbFactory);
					dbFactory.Run(db => {
						db.CreateTable<UserAuth>(true);
						db.CreateTable<UserOAuthProvider>(true);
					});
					sqliteInMemoryRepo.Clear();
					userAuthRepositorys.Add(sqliteInMemoryRepo);

					var sqliteDbFactory = new OrmLiteConnectionFactory(
						"~/App_Data/auth.sqlite".MapProjectPath());
					var sqliteDbRepo = new OrmLiteAuthRepository(sqliteDbFactory);
					sqliteDbRepo.CreateMissingTables();
					userAuthRepositorys.Add(sqliteDbRepo);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				throw;
			}
		}
		
        [Test]
        public void Does_persist_TwitterOAuth()
        {
            userAuthRepositorys.ForEach(x => tests.Does_persist_TwitterOAuth(x));
        }

        [Test]
        public void Does_persist_FacebookOAuth()
        {
            userAuthRepositorys.ForEach(x => tests.Does_persist_FacebookOAuth(x));
        }

        [Test]
        public void Does_merge_FacebookOAuth_TwitterOAuth()
        {
            userAuthRepositorys.ForEach(x => tests.Does_merge_FacebookOAuth_TwitterOAuth(x));
        }

        [Test]
        public void Can_login_with_user_created_CreateUserAuth()
        {
            userAuthRepositorys.ForEach(x => tests.Can_login_with_user_created_CreateUserAuth(x));
        }


    }
}