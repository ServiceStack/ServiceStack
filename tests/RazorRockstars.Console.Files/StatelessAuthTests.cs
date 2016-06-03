using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    public class CustomUserSession : AuthUserSession
    {
        public int Counter { get; set; }
    }

    [Route("/secured")]
    public class Secured : IReturn<SecuredResponse>
    {
        public string Name { get; set; }
    }

    public class SecuredResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/secured-by-role")]
    public class SecuredByRole : IReturn<SecuredResponse>
    {
        public string Name { get; set; }
    }

    [Route("/secured-by-permission")]
    public class SecuredByPermission : IReturn<SecuredResponse>
    {
        public string Name { get; set; }
    }

    public class GetAuthUserSession : IReturn<AuthUserSession> { }

    [Authenticate]
    public class SecureService : Service
    {
        public object Any(Secured request)
        {
            return new SecuredResponse { Result = request.Name };
        }

        public object Any(GetAuthUserSession request)
        {
            return Request.GetSession() as AuthUserSession;
        }

        [RequiredRole("TheRole")]
        public object Any(SecuredByRole request)
        {
            return new SecuredResponse { Result = request.Name };
        }

        [RequiredPermission("ThePermission")]
        public object Any(SecuredByPermission request)
        {
            return new SecuredResponse { Result = request.Name };
        }
    }

    public class JsonHttpClientStatelessAuthTests : StatelessAuthTests
    {
        protected override IServiceClient GetClientWithUserPassword(bool alwaysSend = false, string userName = null)
        {
            return new JsonHttpClient(ListeningOn)
            {
                UserName = userName ?? Username,
                Password = Password,
                AlwaysSendBasicAuthHeader = alwaysSend,
            };
        }

        protected override IServiceClient GetClientWithApiKey(string apiKey = null)
        {
            return new JsonHttpClient(ListeningOn)
            {
                Credentials = new NetworkCredential(apiKey ?? ApiKey.Id, ""),
            };
        }

        protected override IServiceClient GetClientWithBearerToken(string bearerToken)
        {
            return new JsonHttpClient(ListeningOn)
            {
                BearerToken = bearerToken,
            };
        }

        protected override IServiceClient GetClient()
        {
            return new JsonHttpClient(ListeningOn);
        }
    }

    public class DynamoDbAuthRepoStatelessAuthTests : StatelessAuthTests
    {
        public static AmazonDynamoDBClient CreateDynamoDBClient()
        {
            var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
            {
                ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000"),
            });

            return dynamoClient;
        }
        protected override ServiceStackHost CreateAppHost()
        {
            var pocoDynamo = new PocoDynamo(CreateDynamoDBClient());
            pocoDynamo.DeleteAllTables(TimeSpan.FromMinutes(1));

            return new AppHost
            {
                EnableAuth = true,
                Use = container => container.Register<IAuthRepository>(c => new DynamoDbAuthRepository(pocoDynamo))
            };
        }
    }

    public class MemoryAuthRepoStatelessAuthTests : StatelessAuthTests
    {
        protected override ServiceStackHost CreateAppHost()
        {
            return new AppHost
            {
                EnableAuth = true,
                Use = container => container.Register<IAuthRepository>(c => new InMemoryAuthRepository())
            };
        }
    }

    public class RedisAuthRepoStatelessAuthTests : StatelessAuthTests
    {
        protected override ServiceStackHost CreateAppHost()
        {
            var redisManager = new RedisManagerPool();
            using (var redis = redisManager.GetClient())
            {
                redis.FlushAll();
            }

            return new AppHost
            {
                EnableAuth = true,
                Use = container => container.Register<IAuthRepository>(c => new RedisAuthRepository(redisManager))
            };
        }
    }

    public class OrmLiteStatelessAuthTests : StatelessAuthTests
    {
        [Test]
        public void Does_use_different_database_depending_on_ApiKey()
        {
            var apiKeys = apiRepo.GetUserApiKeys(userId);
            var testKey = apiKeys.First(x => x.Environment == "test");
            var liveKey = apiKeys.First(x => x.Environment == "live");

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = testKey.Id,
            };

            var response = client.Get(new GetAllRockstars());
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Test"));

            client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = liveKey.Id,
            };

            response = client.Get(new GetAllRockstars());
            Assert.That(response.Results.Count, Is.EqualTo(Rockstar.SeedData.Length));
        }
    }

    public class RsaJwtStatelessAuthTests : StatelessAuthTests
    {
        protected override ServiceStackHost CreateAppHost()
        {
            return new AppHost
            {
                EnableAuth = true,
                JwtRsaPrivateKey = RsaUtils.CreatePrivateKeyParams(),
                Use = container => container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()))
            };
        }
    }

    public class RsaJwtWithEncryptedPayloadsStatelessAuthTests : StatelessAuthTests
    {
        private RSAParameters privateKey;
        private byte[] cryptKey;
        private byte[] cryptIv;

        protected override ServiceStackHost CreateAppHost()
        {
            privateKey = RsaUtils.CreatePrivateKeyParams();
            AesUtils.CreateKeyAndIv(out cryptKey, out cryptIv);

            return new AppHost
            {
                EnableAuth = true,
                JwtRsaPrivateKey = privateKey,
                JwtEncryptPayload = true,
                JwtCryptKey = cryptKey,
                JwtCryptIv = cryptIv,
                Use = container => container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()))
            };
        }

        [Test]
        public void Can_populate_entire_session_using_JWT_Token()
        {
            var jwtProvider = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var payload = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                Roles = new List<string> { "TheRole" },
                Permissions = new List<string> { "ThePermission" },
                ProfileUrl = "http://example.org/profile.jpg"
            }, "external-jwt", TimeSpan.FromDays(14));

            JwtAuthProviderReaderTests.PopulateWithAdditionalMetadata(payload);

            var token = JwtAuthProvider.CreateJwtBearerToken(header, payload,
                data => RsaUtils.Authenticate(data, privateKey, "SHA256", JwtAuthProvider.UseRsaKeyLength),
                data => AesUtils.Encrypt(data, cryptKey, cryptIv));

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = token
            };

            var session = client.Get(new GetAuthUserSession());

            JwtAuthProviderReaderTests.AssertAdditionalMetadataWasPopulated(session);
        }
    }

    public class JwtAuthProviderReaderTests
    {
        public const string ListeningOn = "http://localhost:2337/";

        protected readonly ServiceStackHost appHost;

        private readonly RSAParameters privateKey;

        class JwtAuthProviderReaderAppHost : AppHostHttpListenerBase
        {
            public JwtAuthProviderReaderAppHost() : base("Test Razor", typeof(AppHost).Assembly) { }

            public override void Configure(Container container)
            {
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
                container.Register<IDbConnectionFactory>(dbFactory);
                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) { UseDistinctRoleTables = true });

                var authRepository = container.Resolve<IAuthRepository>();
                authRepository.InitSchema();
                authRepository.CreateUserAuth(new UserAuth
                {
                    UserName = "user",
                    Email = "as@if{0}.com",
                    DisplayName = "DisplayName",
                    FirstName = "FirstName",
                    LastName = "LastName",
                }, "p@55word");

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new JwtAuthProviderReader(AppSettings),
                    }));
            }
        }

        public JwtAuthProviderReaderTests()
        {
            privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);

            appHost = new JwtAuthProviderReaderAppHost
            {
                AppSettings = new DictionarySettings(new Dictionary<string, string> {
                        { "jwt.HashAlgorithm", "RS256" },
                        { "jwt.PublicKeyXml", privateKey.ToPublicKeyXml() },
                        { "jwt.RequireSecureConnection", "False" },
                    })
            }
               .Init()
               .Start("http://*:2337/");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_authenticate_with_RSA_token_created_from_external_source()
        {
            var jwtProvider = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var payload = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            }, "external-jwt", TimeSpan.FromDays(14));

            var token = JwtAuthProvider.CreateJwtBearerToken(header, payload,
                data => RsaUtils.Authenticate(data, privateKey, "SHA256", RsaKeyLengths.Bit2048));

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = token
            };

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));
        }

        [Test]
        public void Token_without_roles_or_permssions_cannot_access_SecuredBy_Role_or_Permission()
        {
            var jwtProvider = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var payload = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
            }, "external-jwt", TimeSpan.FromDays(14));

            var token = JwtAuthProvider.CreateJwtBearerToken(header, payload,
                data => RsaUtils.Authenticate(data, privateKey, "SHA256", RsaKeyLengths.Bit2048));

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = token
            };

            StatelessAuthTests.AssertNoAccessToSecuredByRoleAndPermission(client);
        }

        [Test]
        public void Token_with_roles_and_permssions_can_access_SecuredBy_Role_or_Permission()
        {
            var jwtProvider = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var payload = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                Roles = new List<string> { "TheRole" },
                Permissions = new List<string> { "ThePermission" },
            }, "external-jwt", TimeSpan.FromDays(14));

            var token = JwtAuthProvider.CreateJwtBearerToken(header, payload,
                data => RsaUtils.Authenticate(data, privateKey, "SHA256", RsaKeyLengths.Bit2048));

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = token
            };

            StatelessAuthTests.AssertAccessToSecuredByRoleAndPermission(client);
        }

        [Test]
        public void Can_populate_entire_session_using_JWT_Token()
        {
            var jwtProvider = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var payload = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                Roles = new List<string> { "TheRole" },
                Permissions = new List<string> { "ThePermission" },
                ProfileUrl = "http://example.org/profile.jpg"
            }, "external-jwt", TimeSpan.FromDays(14));

            PopulateWithAdditionalMetadata(payload);

            var token = JwtAuthProvider.CreateJwtBearerToken(header, payload,
                data => RsaUtils.Authenticate(data, privateKey, "SHA256", RsaKeyLengths.Bit2048));

            var client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = token
            };

            var session = client.Get(new GetAuthUserSession());

            AssertAdditionalMetadataWasPopulated(session);
        }

        public static void AssertAdditionalMetadataWasPopulated(AuthUserSession session)
        {
            Assert.That(session.Id, Is.EqualTo("SESSIONID"));
            Assert.That(session.ReferrerUrl, Is.EqualTo("http://example.org/ReferrerUrl"));
            Assert.That(session.UserAuthName, Is.EqualTo("UserAuthName"));
            Assert.That(session.TwitterUserId, Is.EqualTo("TwitterUserId"));
            Assert.That(session.TwitterScreenName, Is.EqualTo("TwitterScreenName"));
            Assert.That(session.FacebookUserId, Is.EqualTo("FacebookUserId"));
            Assert.That(session.FirstName, Is.EqualTo("FirstName"));
            Assert.That(session.LastName, Is.EqualTo("LastName"));
            Assert.That(session.Company, Is.EqualTo("Company"));
            Assert.That(session.PrimaryEmail, Is.EqualTo("PrimaryEmail"));
            Assert.That(session.PhoneNumber, Is.EqualTo("PhoneNumber"));
            Assert.That(session.BirthDate, Is.EqualTo(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(session.Address, Is.EqualTo("Address"));
            Assert.That(session.Address2, Is.EqualTo("Address2"));
            Assert.That(session.City, Is.EqualTo("City"));
            Assert.That(session.State, Is.EqualTo("State"));
            Assert.That(session.Country, Is.EqualTo("Country"));
            Assert.That(session.Culture, Is.EqualTo("Culture"));
            Assert.That(session.FullName, Is.EqualTo("FullName"));
            Assert.That(session.Gender, Is.EqualTo("Gender"));
            Assert.That(session.Language, Is.EqualTo("Language"));
            Assert.That(session.MailAddress, Is.EqualTo("MailAddress"));
            Assert.That(session.Nickname, Is.EqualTo("Nickname"));
            Assert.That(session.PostalCode, Is.EqualTo("PostalCode"));
            Assert.That(session.TimeZone, Is.EqualTo("TimeZone"));
            Assert.That(session.RequestTokenSecret, Is.EqualTo("RequestTokenSecret"));
            Assert.That(session.CreatedAt, Is.EqualTo(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(session.LastModified, Is.EqualTo(new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(session.Sequence, Is.EqualTo("Sequence"));
            Assert.That(session.Tag, Is.EqualTo(1));
        }

        public static void PopulateWithAdditionalMetadata(Dictionary<string, string> payload)
        {
            payload["Id"] = "SESSIONID";
            payload["ReferrerUrl"] = "http://example.org/ReferrerUrl";
            payload["UserAuthName"] = "UserAuthName";
            payload["TwitterUserId"] = "TwitterUserId";
            payload["TwitterScreenName"] = "TwitterScreenName";
            payload["FacebookUserId"] = "FacebookUserId";
            payload["FacebookUserName"] = "FacebookUserName";
            payload["FirstName"] = "FirstName";
            payload["LastName"] = "LastName";
            payload["Company"] = "Company";
            payload["PrimaryEmail"] = "PrimaryEmail";
            payload["PhoneNumber"] = "PhoneNumber";
            payload["BirthDate"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUnixTime().ToString();
            payload["Address"] = "Address";
            payload["Address2"] = "Address2";
            payload["City"] = "City";
            payload["State"] = "State";
            payload["Country"] = "Country";
            payload["Culture"] = "Culture";
            payload["FullName"] = "FullName";
            payload["Gender"] = "Gender";
            payload["Language"] = "Language";
            payload["MailAddress"] = "MailAddress";
            payload["Nickname"] = "Nickname";
            payload["PostalCode"] = "PostalCode";
            payload["TimeZone"] = "TimeZone";
            payload["RequestTokenSecret"] = "RequestTokenSecret";
            payload["CreatedAt"] = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUnixTime().ToString();
            payload["LastModified"] = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUnixTime().ToString();
            payload["Sequence"] = "Sequence";
            payload["Tag"] = 1.ToString();
        }
    }

    public abstract class StatelessAuthTests
    {
        public const string ListeningOn = "http://localhost:2337/";

        protected readonly ServiceStackHost appHost;
        protected ApiKey ApiKey;
        protected ApiKey ApiKeyWithRole;
        protected IManageApiKeys apiRepo;
        protected ApiKeyAuthProvider apiProvider;
        protected string userId;
        protected string userIdWithRoles;

        protected virtual ServiceStackHost CreateAppHost()
        {
            return new AppHost
            {
                EnableAuth = true,
                Use = container => container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()))
            };
        }

        public StatelessAuthTests()
        {
            //LogManager.LogFactory = new ConsoleLogFactory();
            appHost = CreateAppHost()
               .Init()
               .Start("http://*:2337/");

            var client = GetClient();
            var response = client.Post(new Register
            {
                UserName = "user",
                Password = "p@55word",
                Email = "as@if{0}.com",
                DisplayName = "DisplayName",
                FirstName = "FirstName",
                LastName = "LastName",
            });

            userId = response.UserId;
            apiRepo = (IManageApiKeys)appHost.Resolve<IAuthRepository>();
            ApiKey = apiRepo.GetUserApiKeys(userId).First(x => x.Environment == "test");

            apiProvider = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);

            response = client.Post(new Register
            {
                UserName = "user2",
                Password = "p@55word",
                Email = "as2@if{0}.com",
                DisplayName = "DisplayName2",
                FirstName = "FirstName2",
                LastName = "LastName2",
            });
            userIdWithRoles = response.UserId;
            ApiKeyWithRole = apiRepo.GetUserApiKeys(userIdWithRoles).First(x => x.Environment == "test");

            var authRepo = (IUserAuthRepository)appHost.Resolve<IAuthRepository>();
            var user2 = authRepo.GetUserAuth(userIdWithRoles);

            var manageRoles = authRepo as IManageRoles;
            if (manageRoles == null)
            {
                user2.Roles = new List<string> { "TheRole" };
                user2.Permissions = new List<string> { "ThePermission" };
                authRepo.SaveUserAuth(user2);
            }
            else
            {
                manageRoles.AssignRoles(userIdWithRoles, new[] { "TheRole" }, new[] { "ThePermission" });
            }
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Ignore("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Process.Start(ListeningOn);
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        public const string Username = "user";
        public const string Password = "p@55word";

        protected virtual IServiceClient GetClientWithUserPassword(bool alwaysSend = false, string userName = null)
        {
            return new JsonServiceClient(ListeningOn)
            {
                UserName = userName ?? Username,
                Password = Password,
                AlwaysSendBasicAuthHeader = alwaysSend,
            };
        }

        protected virtual IServiceClient GetClientWithApiKey(string apiKey = null)
        {
            return new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(apiKey ?? ApiKey.Id, ""),
            };
        }

        protected virtual IServiceClient GetClientWithBearerToken(string bearerToken)
        {
            return new JsonServiceClient(ListeningOn)
            {
                BearerToken = bearerToken,
            };
        }

        protected virtual IServiceClient GetClient()
        {
            return new JsonServiceClient(ListeningOn);
        }

        [Test]
        public void Does_create_multiple_ApiKeys()
        {
            var apiKeys = apiRepo.GetUserApiKeys(userId);
            Assert.That(apiKeys.Count, Is.EqualTo(
                apiProvider.Environments.Length * apiProvider.KeyTypes.Length));

            Assert.That(apiKeys.All(x => x.UserAuthId != null));
            Assert.That(apiKeys.All(x => x.Environment != null));
            Assert.That(apiKeys.All(x => x.KeyType != null));
            Assert.That(apiKeys.All(x => x.CreatedDate != default(DateTime)));
            Assert.That(apiKeys.All(x => x.CancelledDate == null));
            Assert.That(apiKeys.All(x => x.ExpiryDate == null));

            foreach (var apiKey in apiKeys)
            {
                var byId = apiRepo.GetApiKey(apiKey.Id);
                Assert.That(byId.Id, Is.EqualTo(apiKey.Id));
            }
        }

        [Test]
        public void Regenerating_AuthKeys_invalidates_existing_Keys_and_enables_new_keys()
        {
            var client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Id, ""),
            };

            var apiKeyResponse = client.Get(new GetApiKeys { Environment = "live" });
            apiKeyResponse.PrintDump();

            var oldApiKey = apiKeyResponse.Results[0].Key;
            client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = oldApiKey,
            };

            //Key IsValid
            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var regenResponse = client.Send(new RegenrateApiKeys { Environment = "live" });

            try
            {
                //Key is no longer valid
                apiKeyResponse = client.Get(new GetApiKeys { Environment = "live" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }

            //Change to new Valid Key
            client.BearerToken = regenResponse.Results[0].Key;
            apiKeyResponse = client.Get(new GetApiKeys { Environment = "live" });

            Assert.That(regenResponse.Results.Map(x => x.Key), Is.EquivalentTo(
                apiKeyResponse.Results.Map(x => x.Key)));
        }

        [Test]
        public void Doesnt_allow_using_expired_keys()
        {
            var client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Id, ""),
            };

            var authResponse = client.Get(new Authenticate());

            var apiKeys = apiRepo.GetUserApiKeys(authResponse.UserId)
                .Where(x => x.Environment == "live")
                .ToList();

            var oldApiKey = apiKeys[0].Id;
            client = new JsonServiceClient(ListeningOn)
            {
                BearerToken = oldApiKey,
            };

            //Key IsValid
            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            apiKeys[0].ExpiryDate = DateTime.UtcNow.AddMinutes(-1);
            apiRepo.StoreAll(new[] { apiKeys[0] });

            try
            {
                //Key is no longer valid
                client.Get(new GetApiKeys { Environment = "live" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }

            client = new JsonServiceClient(ListeningOn)
            {
                Credentials = new NetworkCredential(ApiKey.Id, ""),
            };
            var regenResponse = client.Send(new RegenrateApiKeys { Environment = "live" });

            //Change to new Valid Key
            client.BearerToken = regenResponse.Results[0].Key;
            var apiKeyResponse = client.Get(new GetApiKeys { Environment = "live" });

            Assert.That(regenResponse.Results.Map(x => x.Key), Is.EquivalentTo(
                apiKeyResponse.Results.Map(x => x.Key)));
        }

        [Test]
        public void Authenticating_once_with_BasicAuth_does_not_establish_auth_session()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = newClient.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_JWT_does_not_establish_auth_session()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);

            var authResponse = client.Send(new Authenticate());
            Assert.That(authResponse.BearerToken, Is.Not.Null);

            var jwtClient = GetClientWithBearerToken(authResponse.BearerToken);
            var request = new Secured { Name = "test" };
            var response = jwtClient.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(jwtClient.GetSessionId());

            try
            {
                response = newClient.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_with_JWT_cookie_does_allow_multiple_authenticated_requests()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);

            var authResponse = client.Send(new Authenticate());
            Assert.That(authResponse.BearerToken, Is.Not.Null);

            var jwtClient = GetClient();
            jwtClient.SetCookie("ss-jwt", authResponse.BearerToken);

            var request = new Secured { Name = "test" };
            var response = jwtClient.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            var cookieValue = jwtClient.GetCookieValues()["ss-jwt"];
            newClient.SetCookie("ss-jwt", cookieValue);
            response = newClient.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));
        }

        [Test]
        public void Authenticating_once_with_ApiKeyAuth_does_not_establish_auth_session()
        {
            var client = GetClientWithApiKey();

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = newClient.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public async Task Authenticating_once_with_ApiKeyAuth_does_not_establish_auth_session_Async()
        {
            var client = GetClientWithApiKey();

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = await newClient.SendAsync<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_ApiKeyAuth_BearerToken_does_not_establish_auth_session()
        {
            var client = GetClientWithBearerToken(ApiKey.Id);

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = newClient.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public async Task Authenticating_once_with_ApiKeyAuth_BearerToken_does_not_establish_auth_session_Async()
        {
            var client = GetClientWithBearerToken(ApiKey.Id);

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync<SecuredResponse>(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            try
            {
                response = await newClient.SendAsync<SecuredResponse>(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Authenticating_once_with_CredentialsAuth_does_establish_auth_session()
        {
            var client = GetClient();

            try
            {
                client.Send(new Authenticate());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }

            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });

            client.Send(new Authenticate());

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            var newClient = GetClient();
            newClient.SetSessionId(client.GetSessionId());
            response = newClient.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));
        }

        [Test]
        public void Can_not_access_Secured_Pages_without_Authentication()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(),
                Is.StringContaining("<!--page:Login.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_BasicAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBasicAuth(Username, Password)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_JWT()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);
            var authResponse = client.Send(new Authenticate());

            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(authResponse.BearerToken)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Id)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Id)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Id)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddApiKeyAuth(ApiKey.Id)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken()
        {
            Assert.That(ListeningOn.CombineWith("/secured").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey.Id)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(ListeningOn.CombineWith("/SecuredPage").GetStringFromUrl(
                requestFilter: req => req.AddBearerToken(ApiKey.Id)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public async Task Can_access_Secured_Pages_with_ApiKeyAuth_BearerToken_Async()
        {
            Assert.That(await ListeningOn.CombineWith("/secured").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey.Id)),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(await ListeningOn.CombineWith("/SecuredPage").GetStringFromUrlAsync(
                requestFilter: req => req.AddBearerToken(ApiKey.Id)),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        [Test]
        public void Can_access_Secured_Pages_with_CredentialsAuth()
        {
            var client = GetClient();
            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });

            Assert.That(client.Get<string>("/secured?format=html"),
                Is.StringContaining("<!--view:Secured.cshtml-->"));

            Assert.That(client.Get<string>("/SecuredPage?format=html"),
                Is.StringContaining("<!--page:SecuredPage.cshtml-->"));
        }

        public static void AssertNoAccessToSecuredByRoleAndPermission(IServiceClient client)
        {
            try
            {
                client.Send(new SecuredByRole { Name = "test" });
                Assert.Fail("Should Throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }

            try
            {
                client.Send(new SecuredByPermission { Name = "test" });
                Assert.Fail("Should Throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public void Can_not_access_SecuredBy_Role_or_Permission_without_TheRole_or_ThePermission()
        {
            var client = GetClientWithUserPassword(alwaysSend: true);
            AssertNoAccessToSecuredByRoleAndPermission(client);

            client = GetClientWithApiKey();
            AssertNoAccessToSecuredByRoleAndPermission(client);

            var bearerToken = client.Get(new Authenticate()).BearerToken;
            client = GetClientWithBearerToken(bearerToken);
            AssertNoAccessToSecuredByRoleAndPermission(client);

            client = GetClient();
            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });
            AssertNoAccessToSecuredByRoleAndPermission(client);
        }

        public static void AssertAccessToSecuredByRoleAndPermission(IServiceClient client)
        {
            var roleResponse = client.Send(new SecuredByRole { Name = "test" });
            Assert.That(roleResponse.Result, Is.EqualTo("test"));

            var permResponse = client.Send(new SecuredByPermission { Name = "test" });
            Assert.That(permResponse.Result, Is.EqualTo("test"));
        }

        [Test]
        public void Can_access_SecuredBy_Role_or_Permission_with_TheRole_and_ThePermission()
        {
            var client = GetClientWithUserPassword(alwaysSend: true, userName: "user2");
            AssertAccessToSecuredByRoleAndPermission(client);

            client = GetClientWithApiKey(ApiKeyWithRole.Id);
            AssertAccessToSecuredByRoleAndPermission(client);

            var bearerToken = client.Get(new Authenticate()).BearerToken;
            client = GetClientWithBearerToken(bearerToken);
            AssertAccessToSecuredByRoleAndPermission(client);

            client = GetClient();
            client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "user2",
                Password = Password,
            });
            AssertAccessToSecuredByRoleAndPermission(client);
        }

        [Test]
        public void Can_not_access_Secure_service_with_invalidated_token()
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);

            var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            });

            var client = GetClientWithBearerToken(token);

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            jwtProvider.InvalidateTokensIssuedBefore = DateTime.UtcNow.AddSeconds(1);

            try
            {
                response = client.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(TokenException).Name));
            }
            finally
            {
                jwtProvider.InvalidateTokensIssuedBefore = null;
            }
        }

        [Test]
        public void Can_not_access_Secure_service_with_expired_token()
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);
            jwtProvider.CreatePayloadFilter = jwtPayload =>
                jwtPayload["exp"] = DateTime.UtcNow.AddSeconds(-1).ToUnixTime().ToString();

            var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            });

            jwtProvider.CreatePayloadFilter = null;

            var client = GetClientWithBearerToken(token);

            try
            {
                var request = new Secured { Name = "test" };
                var response = client.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(TokenException).Name));
            }
        }

        [Test]
        public void Can_Auto_reconnect_after_expired_token()
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);
            jwtProvider.CreatePayloadFilter = jwtPayload =>
                jwtPayload["exp"] = DateTime.UtcNow.AddSeconds(-1).ToUnixTime().ToString();

            var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            });

            jwtProvider.CreatePayloadFilter = null;

            var authClient = GetClientWithUserPassword(alwaysSend: true);

            var called = 0;
            var client = new JsonServiceClient(ListeningOn);
            client.OnAuthenticationRequired = () =>
            {
                called++;
                client.BearerToken = authClient.Send(new Authenticate()).BearerToken;
            };

            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            Assert.That(called, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_Auto_reconnect_after_expired_token_Async()
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);
            jwtProvider.CreatePayloadFilter = jwtPayload =>
                jwtPayload["exp"] = DateTime.UtcNow.AddSeconds(-1).ToUnixTime().ToString();

            var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            });

            jwtProvider.CreatePayloadFilter = null;

            var authClient = GetClientWithUserPassword(alwaysSend: true);

            var called = 0;
            var client = new JsonServiceClient(ListeningOn);
            client.OnAuthenticationRequired = () =>
            {
                called++;
                client.BearerToken = authClient.Send(new Authenticate()).BearerToken;
            };

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            Assert.That(called, Is.EqualTo(1));
        }

        [Test]
        public void Can_not_access_Secure_service_on_unsecured_connection_when_RequireSecureConnection()
        {
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProvider.Name);
            jwtProvider.RequireSecureConnection = true;

            var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com"
            });

            var client = GetClientWithBearerToken(token);

            try
            {
                var request = new Secured { Name = "test" };
                var response = client.Send(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(ex.ErrorCode, Is.EqualTo("Forbidden"));
            }
            finally
            {
                jwtProvider.RequireSecureConnection = false;
            }
        }
    }

}