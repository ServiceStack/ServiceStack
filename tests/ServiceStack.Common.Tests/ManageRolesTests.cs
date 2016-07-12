#if !NETCORE_SUPPORT
using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ManageRolesTests
    {
        private static Register CreateNewUserRegistration(bool? autoLogin = null)
        {
            var userId = Environment.TickCount % 10000;

            var newUserRegistration = new Register
            {
                UserName = "UserName" + userId,
                DisplayName = "DisplayName" + userId,
                Email = "user{0}@sf.com".Fmt(userId),
                FirstName = "FirstName" + userId,
                LastName = "LastName" + userId,
                Password = "Password" + userId,
                AutoLogin = autoLogin,
            };
            return newUserRegistration;
        }

        [Test]
        public void By_default_assigned_roles_are_saved_in_UserAuth_table()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = container =>
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                    container.Register<IAuthRepository>(c =>
                        new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

                    container.Resolve<IAuthRepository>().InitSchema();
                }
            }.Init())
            {
                using (var db = appHost.Container.Resolve<IDbConnectionFactory>().Open())
                {
                    var register = CreateNewUserRegistration();
                    var req = new BasicRequest(register);
                    req.QueryString["authSecret"] = appHost.Config.AdminAuthSecret = "allow";

                    var response = (RegisterResponse)appHost.ExecuteService(register, req);
                    var userAuth = db.SingleById<UserAuth>(response.UserId);

                    var assignResponse = (AssignRolesResponse)appHost.ExecuteService(new AssignRoles
                    {
                        UserName = userAuth.UserName,
                        Roles = { "TestRole" },
                        Permissions = { "TestPermission" },
                    }, req);
                    Assert.That(assignResponse.AllRoles[0], Is.EqualTo("TestRole"));
                    Assert.That(assignResponse.AllPermissions[0], Is.EqualTo("TestPermission"));

                    userAuth = db.SingleById<UserAuth>(response.UserId);
                    Assert.That(userAuth.Roles[0], Is.EqualTo("TestRole"));
                    Assert.That(userAuth.Permissions[0], Is.EqualTo("TestPermission"));

                    appHost.ExecuteService(new UnAssignRoles
                    {
                        UserName = userAuth.UserName,
                        Roles = { "TestRole" },
                        Permissions = { "TestPermission" },
                    }, req);

                    userAuth = db.SingleById<UserAuth>(response.UserId);
                    Assert.That(userAuth.Roles.Count, Is.EqualTo(0));
                    Assert.That(userAuth.Permissions.Count, Is.EqualTo(0));
                }
            }
        }

        [Test]
        public void Can_assign_roles_that_persist_to_UserAuthRole_table()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = container =>
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                    container.Register<IAuthRepository>(c =>
                        new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
                            UseDistinctRoleTables = true,
                        });

                    container.Resolve<IAuthRepository>().InitSchema();
                }
            }.Init())
            {
                using (var db = appHost.Container.Resolve<IDbConnectionFactory>().Open())
                {
                    var register = CreateNewUserRegistration();
                    var req = new BasicRequest(register);
                    req.QueryString["authSecret"] = appHost.Config.AdminAuthSecret = "allow";

                    var response = (RegisterResponse)appHost.ExecuteService(register, req);
                    var userAuth = db.SingleById<UserAuth>(response.UserId);

                    var assignResponse = (AssignRolesResponse)appHost.ExecuteService(new AssignRoles
                    {
                        UserName = userAuth.UserName,
                        Roles = { "TestRole" },
                        Permissions = { "TestPermission" },
                    }, req);
                    Assert.That(assignResponse.AllRoles[0], Is.EqualTo("TestRole"));
                    Assert.That(assignResponse.AllPermissions[0], Is.EqualTo("TestPermission"));

                    Assert.That(userAuth.Roles.Count, Is.EqualTo(0));
                    Assert.That(userAuth.Permissions.Count, Is.EqualTo(0));

                    var manageRoles = (IManageRoles)appHost.Container.Resolve<IAuthRepository>();
                    Assert.That(manageRoles.HasRole(userAuth.Id.ToString(), "TestRole"));
                    Assert.That(manageRoles.HasPermission(userAuth.Id.ToString(), "TestPermission"));

                    appHost.ExecuteService(new UnAssignRoles
                    {
                        UserName = userAuth.UserName,
                        Roles = { "TestRole" },
                        Permissions = { "TestPermission" },
                    }, req);

                    Assert.That(!manageRoles.HasRole(userAuth.Id.ToString(), "TestRole"));
                    Assert.That(!manageRoles.HasPermission(userAuth.Id.ToString(), "TestPermission"));
                }
            }
        }

        [Test]
        public void Can_assign_roles_that_persist_to_UserAuthRole_table_in_DynamoDb()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = container =>
                {
                    container.Register<IPocoDynamo>(c => new PocoDynamo(DynamoConfig.CreateDynamoDBClient()));
                    //DynamoMetadata.Reset();
                    container.Resolve<IPocoDynamo>().DeleteAllTables(TimeSpan.FromMinutes(1));

                    container.Register<IAuthRepository>(c => new DynamoDbAuthRepository(c.Resolve<IPocoDynamo>()));
                    container.Resolve<IAuthRepository>().InitSchema();
                }
            }.Init())
            {
                var db = appHost.Container.Resolve<IPocoDynamo>();

                var register = CreateNewUserRegistration();
                var req = new BasicRequest(register);
                req.QueryString["authSecret"] = appHost.Config.AdminAuthSecret = "allow";

                var response = (RegisterResponse)appHost.ExecuteService(register, req);
                var userAuth = db.GetItem<UserAuth>(response.UserId);

                var assignResponse = (AssignRolesResponse)appHost.ExecuteService(new AssignRoles
                {
                    UserName = userAuth.UserName,
                    Roles = { "TestRole" },
                    Permissions = { "TestPermission" },
                }, req);
                Assert.That(assignResponse.AllRoles[0], Is.EqualTo("TestRole"));
                Assert.That(assignResponse.AllPermissions[0], Is.EqualTo("TestPermission"));

                Assert.That(userAuth.Roles.Count, Is.EqualTo(0));
                Assert.That(userAuth.Permissions.Count, Is.EqualTo(0));

                var manageRoles = (IManageRoles)appHost.Container.Resolve<IAuthRepository>();
                Assert.That(manageRoles.HasRole(userAuth.Id.ToString(), "TestRole"));
                Assert.That(manageRoles.HasPermission(userAuth.Id.ToString(), "TestPermission"));

                appHost.ExecuteService(new UnAssignRoles
                {
                    UserName = userAuth.UserName,
                    Roles = { "TestRole" },
                    Permissions = { "TestPermission" },
                }, req);

                Assert.That(!manageRoles.HasRole(userAuth.Id.ToString(), "TestRole"));
                Assert.That(!manageRoles.HasPermission(userAuth.Id.ToString(), "TestPermission"));
            }
        }

    }
}
#endif