// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !NETCORE_SUPPORT
using System.Net;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests.OAuth
{
    public class OrmLiteUserAuthRepositoryTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost 
            {
                ConfigureAppHost = host =>
                {
                    host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] {
                        new CredentialsAuthProvider(), 
                    }));
                },
                ConfigureContainer = container => 
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                            AutoDisposeConnection = false,
                        });

                    container.Register<IUserAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
                    container.Resolve<IUserAuthRepository>().InitSchema(); 
                }
            }.Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private object RegisterUser(string email = "as@if.com")
        {
            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                db.Delete<UserAuth>(q => q.Email == email);
            }

            var response = appHost.ExecuteService(new Register
            {
                Password = "p@55word",
                Email = email,
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            });

            Assert.That(response as RegisterResponse, Is.Not.Null, response.ToString());

            return response;
        }

        [Test]
        public void Can_attempt_multiple_invalid_logins_without_being_locked_out()
        {
            RegisterUser(email: "as@if.com");

            3.Times(() =>
            {
                var response = appHost.ExecuteService(new Authenticate
                {
                    UserName = "as@if.com",
                    Password = "wrongpassword"
                });
            });

            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                var user = db.Single<UserAuth>(q => q.Email == "as@if.com");
                Assert.That(user.LockedDate, Is.Null);
            }
        }

        [Test]
        public void Does_lockout_user_after_reaching_max_invalid_logins_limit()
        {
            RegisterUser(email: "as@if.com");

            var feature = appHost.GetPlugin<AuthFeature>();
            feature.MaxLoginAttempts = 3;

            feature.MaxLoginAttempts.Value.Times(i =>
            {
                appHost.ExecuteService(new Authenticate {
                    UserName = "as@if.com",
                    Password = "wrongpassword"
                });

                using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
                {
                    var user = db.Single<UserAuth>(q => q.Email == "as@if.com");
                    Assert.That(user.LastLoginAttempt, Is.Not.Null); 
                    Assert.That(user.InvalidLoginAttempts, Is.EqualTo(i + 1)); //0 index
                }
            });

            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                var user = db.Single<UserAuth>(q => q.Email == "as@if.com");
                Assert.That(user.LockedDate, Is.Not.Null);
            }

            var response = appHost.ExecuteService(new Authenticate
            {
                UserName = "as@if.com",
                Password = "p@55word"
            });

            var httpError = (HttpError)response;
            Assert.That(httpError.Message, Is.EqualTo("This account has been locked"));
            Assert.That(httpError.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
#endif