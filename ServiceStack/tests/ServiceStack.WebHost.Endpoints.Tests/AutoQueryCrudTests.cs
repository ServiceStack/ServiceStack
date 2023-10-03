using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoCrudGatewayServices : Service
    {
        public async Task<object> Any(CreateRockstarAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<CreateRockstarAuditTenant>();
            var sync = Gateway.Send(gatewayRequest);
            var response = await Gateway.SendAsync(gatewayRequest);
            return response;
        }
        
        public async Task<object> Any(UpdateRockstarAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<UpdateRockstarAuditTenant>();
            var sync = Gateway.Send(gatewayRequest);
            var response = await Gateway.SendAsync(gatewayRequest);
            return response;
        }
        
        public async Task<object> Any(PatchRockstarAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<PatchRockstarAuditTenant>();
            var sync = Gateway.Send(gatewayRequest);
            var response = await Gateway.SendAsync(gatewayRequest);
            return response;
        }
        
        public async Task<object> Any(RealDeleteAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<RealDeleteAuditTenant>();
            var sync = Gateway.Send(gatewayRequest);
            var response = await Gateway.SendAsync(gatewayRequest);
            return response;
        }
        
        public void Any(CreateRockstarAuditTenantMq request)
        {
            var mqRequest = request.ConvertTo<CreateRockstarAuditTenant>();
            Request.PopulateRequestDtoIfAuthenticated(mqRequest);
            PublishMessage(mqRequest);
        }
        
        public void Any(UpdateRockstarAuditTenantMq request)
        {
            var mqRequest = request.ConvertTo<UpdateRockstarAuditTenant>();
            Request.PopulateRequestDtoIfAuthenticated(mqRequest);
            PublishMessage(mqRequest);
        }
        
        public void Any(PatchRockstarAuditTenantMq request)
        {
            var mqRequest = request.ConvertTo<PatchRockstarAuditTenant>();
            mqRequest.BearerToken = Request.GetJwtToken();
            PublishMessage(mqRequest);
        }
        
        public void Any(RealDeleteAuditTenantMq request)
        {
            var mqRequest = request.ConvertTo<RealDeleteAuditTenant>();
            mqRequest.BearerToken = Request.GetJwtToken();
            PublishMessage(mqRequest);
        }
    }

    [ConnectionInfo(NamedConnection = AutoQueryAppHost.SqlServerNamedConnection)]
    public class AutoCrudConnectionInfoServices : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        public Task<object> Any(CreateConnectionInfoRockstar request) => 
            AutoQuery.CreateAsync(request, Request);

        public Task<object> Any(UpdateConnectionInfoRockstar request) => 
            AutoQuery.UpdateAsync(request, Request);
    }

    public class AutoCrudBatchServices : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        protected virtual async Task<object> BatchCreateAsync<T>(IEnumerable<ICreateDb<T>> requests)
        {
            using var db = AutoQuery.GetDb<T>(Request);
            using var dbTrans = db.OpenTransaction();

            var results = new List<object>();
            foreach (var request in requests)
            {
                var response = await AutoQuery.CreateAsync(request, Request, db);
                results.Add(response);
            }

            dbTrans.Commit();
            return results;            
        }

        public object Any(CustomCreateBooking[] requests) => BatchCreateAsync(requests);
    }

    /*
    public abstract class B
    {
        public virtual async Task<object> BatchCreateAsync<T>(IEnumerable<ICreateDb<T>> requests) => Task.FromResult("A");
    }
    public class A : B
    {
        public object Any(CustomCreateBooking[] requests) => BatchCreateAsync(requests);
    }
    */

    public partial class AutoQueryCrudTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;
        private const string TenantId = nameof(TenantId);
        private static readonly byte[] AuthKey = AesUtils.CreateKey();
        public static string JwtUserToken = null;

        partial void OnConfigure(AutoQueryAppHost host, Funq.Container container);
        
        public AutoQueryCrudTests()
        {
            appHost = new AutoQueryAppHost {
                    ConfigureFn = (host,container) => {

                        container.AddSingleton<ICrudEvents>(c =>
                            new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()) {
                                NamedConnections = { AutoQueryAppHost.SqlServerNamedConnection }
                            }.Reset() //Drop and re-create AutoCrudEvent Table
                        );
                        container.Resolve<ICrudEvents>().InitSchema();
                        
                        container.AddSingleton<IAuthRepository>(c =>
                            new InMemoryAuthRepository());
                        host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                            new IAuthProvider[] {
                                new CredentialsAuthProvider(host.AppSettings),
                                new JwtAuthProvider(host.AppSettings) {
                                    RequireSecureConnection = false,
                                    AuthKey = AuthKey,
                                    CreatePayloadFilter = (obj, session) => {
                                        obj[nameof(AuthUserSession.City)] = ((AuthUserSession)session).City;
                                    }
                                }, 
                            }));
                        
                        var jwtProvider = host.GetPlugin<AuthFeature>().AuthProviders.OfType<JwtAuthProvider>().First();
                        JwtUserToken = jwtProvider.CreateJwtBearerToken(new AuthUserSession {
                            Id = SessionExtensions.CreateRandomSessionId(),
                            UserName = "jwtuser",
                            FirstName = "JWT",
                            LastName = "User",
                            DisplayName = "JWT User",
                            City = "Japan",
                        });
                        
                        var authRepo = container.Resolve<IAuthRepository>();
                        authRepo.InitSchema();
                        
                        authRepo.CreateUserAuth(new UserAuth {
                            Id = 1,
                            Email = "admin@email.com", 
                            DisplayName = "Admin User",
                            City = "London",
                            Roles = new List<string> {
                                RoleNames.Admin
                            }
                        }, "p@55wOrd");
                        
                        authRepo.CreateUserAuth(new UserAuth {
                            Id = 2,
                            UserName = "manager", 
                            DisplayName = "The Manager",
                            City = "Perth",
                            Roles = new List<string> {
                                "Employee",
                                "Manager",
                            }
                        }, "p@55wOrd");
                        
                        authRepo.CreateUserAuth(new UserAuth {
                            Id = 3,
                            Email = "employee@email.com", 
                            DisplayName = "An Employee",
                            City = "Manhattan",
                            Roles = new List<string> {
                                "Employee",
                            }
                        }, "p@55wOrd");
                        
                        void AddTenantId(IRequest req, IResponse res, object dto)
                        {
                            var userSession = req.SessionAs<AuthUserSession>();
                            if (userSession.IsAuthenticated)
                            {
                                req.SetItem(TenantId, userSession.City switch {
                                    "London" => 10,
                                    "Perth"  => 10,
                                    _        => 20,
                                });
                            }
                        }
                            
                        host.GlobalRequestFilters.Add(AddTenantId);
                        host.GlobalMessageRequestFilters.Add(AddTenantId);

                        container.AddSingleton<IMessageService>(c => new BackgroundMqService());
                        var mqService = container.Resolve<IMessageService>();
                        mqService.RegisterHandler<CreateRockstarAuditTenant>(host.ExecuteMessage);
                        mqService.RegisterHandler<UpdateRockstarAuditTenant>(host.ExecuteMessage);
                        mqService.RegisterHandler<PatchRockstarAuditTenant>(host.ExecuteMessage);
                        mqService.RegisterHandler<RealDeleteAuditTenant>(host.ExecuteMessage);
                        mqService.RegisterHandler<CreateRockstarAuditMqToken>(host.ExecuteMessage);
                        host.AfterInitCallbacks.Add(_ => mqService.Start());
                        
                        OnConfigure(host, container);
                    }
                }
                .Init()
                .Start(Config.ListeningOn);
            
            using var db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.CreateTable<RockstarAudit>();
            db.CreateTable<RockstarAuditTenant>();
            db.CreateTable<RockstarAutoGuid>();
            db.CreateTable<RockstarVersion>();
            db.CreateTable<Bookmark>();
            db.CreateTable<DefaultValue>();
            db.CreateTable<Booking>();

            AutoMapping.RegisterPopulator((Dictionary<string,object> target, CreateRockstarWithAutoGuid source) => {
                if (source.FirstName == "Created")
                {
                    target[nameof(source.LivingStatus)] = LivingStatus.Dead;
                }
            });
            
            client = new JsonServiceClient(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        public List<Rockstar> Rockstars => AutoQueryAppHost.SeedRockstars.ToList();

        public List<PagingTest> PagingTests => AutoQueryAppHost.SeedPagingTest.ToList();

        private static JsonServiceClient CreateAuthClient()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });
            return authClient;
        }

        [Test]
        public void Can_CreateRockstar()
        {
            var request = new CreateRockstar {
                FirstName = "Return",
                LastName = "Empty",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var response = client.Post(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "Empty");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Return"));
        }

        [Test]
        public void Can_CreateRockstarWithReturn()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Return",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,2,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.GreaterThan(0));
            var newRockstar = response.Result;
            Assert.That(newRockstar.LastName, Is.EqualTo("Result"));
        }
 
        [Test]
        public void Can_CreateRockstarWithVoidReturn()
        {
            var request = new CreateRockstarWithVoidReturn {
                FirstName = "Return",
                LastName = "Void",
                Age = 20,
                DateOfBirth = new DateTime(2001,3,1),
                LivingStatus = LivingStatus.Alive,
            };

            client.Post(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "Void");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Return"));
        }
 
        [Test]
        public void Can_CreateRockstarWithAutoGuid()
        {
            var request = new CreateRockstarWithAutoGuid {
                FirstName = "Create",
                LastName = "AutoId",
                Age = 20,
                DateOfBirth = new DateTime(2001,4,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.Not.Null);
            var newRockstar = response.Result;
            Assert.That(newRockstar.Id, Is.EqualTo(response.Id));
            Assert.That(newRockstar.LastName, Is.EqualTo("AutoId"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }

        [Test]
        public void Can_CreateRockstarWithAutoGuid_with_Custom_Mapping()
        {
            var request = new CreateRockstarWithAutoGuid {
                FirstName = "Created",
                LastName = "AutoId",
                Age = 20,
                DateOfBirth = new DateTime(2001,5,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.Not.Null);
            var newRockstar = response.Result;
            Assert.That(newRockstar.Id, Is.EqualTo(response.Id));
            Assert.That(newRockstar.LastName, Is.EqualTo("AutoId"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead)); //overridden by RegisterPopulator
        }

        [Test]
        public void Can_UpdateRockstar()
        {
            var createResponse = client.Post(new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            });
            
            var request = new UpdateRockstar {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Put(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.Null);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }
 
        [Test]
        public void Can_PatchRockstar()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new PatchRockstar {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Patch(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(createRequest.Age));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
        }
  
        [Test]
        public void Can_UpdateRockstarAdhocNonDefaults()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new UpdateRockstarAdhocNonDefaults {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            using (JsConfig.With(new Text.Config { AssumeUtc = true }))
            {
                var response = client.Put(request);
            }

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName)); //[AutoUpdate(AutoUpdateStyle.NonDefaults)]
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth, Is.EqualTo(new DateTime(2001,1,1)));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = LivingStatus.Dead)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
        }

        [Test]
        public void Does_throw_when_no_rows_updated()
        {
            try
            {
                client.Put(new UpdateRockstar {
                    Id = 100,
                    LastName = "UpdateRockstar",
                });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
        }

        [Test]
        public void Can_Delete_CreateRockstarWithReturn()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Rockstar",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(request);

            using var db = appHost.GetDbConnection();

            var newRockstar = db.Single<Rockstar>(x => x.Id == createResponse.Id);
            Assert.That(newRockstar, Is.Not.Null);

            var response = client.Delete(new DeleteRockstar {
                Id = createResponse.Id
            });

            newRockstar = db.Single<Rockstar>(x => x.Id == createResponse.Id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public void Does_throw_for_Delete_without_filters()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Rockstar",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(request);

            try
            {
                var response = client.Delete(new DeleteRockstar());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(NotSupportedException)));
            }
        }

        [Test]
        public void Can_delete_with_multiple_non_PrimaryKey_filters()
        {
            var requests = 5.Times(i => new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Filter" + i,
                Age = 23,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            });
            
            requests.Each(x => client.Post(x));

            try
            {
                client.Delete(new DeleteRockstarFilters());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(NotSupportedException)));
            }

            using var db = appHost.GetDbConnection();

            var response = client.Delete(new DeleteRockstarFilters { Age = 23, LastName = "Filter1" });
            Assert.That(response.Count, Is.EqualTo(1));
            var remaining = db.Select<Rockstar>(x => x.Age == 23);
            Assert.That(remaining.Count, Is.EqualTo(5 - 1));

            response = client.Delete(new DeleteRockstarFilters { Age = 23 });
            Assert.That(response.Count, Is.EqualTo(4));
            remaining = db.Select<Rockstar>(x => x.Age == 23);
            Assert.That(remaining.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void Can_CreateRockstarAdhocNonDefaults()
        {
            var createRequest = new CreateRockstarAdhocNonDefaults {
                FirstName = "Create",
                LastName = "Defaults",
            };

            using var jsScope = JsConfig.With(new Text.Config { AssumeUtc = true });
            var createResponse = client.Post(createRequest);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("Defaults"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth, Is.EqualTo(new DateTime(2001,1,1)));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoDefault(Value = global::ServiceStack.WebHost.Endpoints.Tests.LivingStatus.Dead)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead));
        }
        
        [Test]
        public void Can_CreateRockstarAutoMap()
        {
            var createRequest = new CreateRockstarAutoMap {
                MapFirstName = "Map",
                MapLastName = "Defaults",
                MapDateOfBirth = new DateTime(2002,2,2),
                MapLivingStatus = LivingStatus.Alive,
            };

            var createResponse = client.Post(createRequest);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("Defaults"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.MapFirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(new DateTime(2002,2,2).Date));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoDefault(Value = LivingStatus.Alive)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }

        [Test]
        public void Can_CreateRockstarAudit()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });
 
            var createResponse = authClient.Post(new CreateRockstarAudit {
                FirstName = "Create",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            });
            
            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Create"));
            Assert.That(newRockstar.LastName, Is.EqualTo("Audit"));
            Assert.That(newRockstar.Age, Is.EqualTo(20));
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(new DateTime(2002,2,2).Date));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead));
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("Admin User (London)"));

            authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "manager",
                Password = "p@55wOrd",
                RememberMe = true,
            });
 
            authClient.Patch(new UpdateRockstarAudit {
                Id = createResponse.Id,
                FirstName = "Updated",
                LivingStatus = LivingStatus.Alive,
            });

            newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Updated"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("The Manager (Perth)"));

            authClient.Delete(new DeleteRockstarAudit {
                Id = createResponse.Id,
            });

            newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public async Task Can_CreateRockstarAuditTenant_with_Events()
        {
            var dbEvents = (OrmLiteCrudEvents) appHost.Resolve<ICrudEvents>();
            dbEvents.Clear();

            var authClient = CreateAuthClient();
            var id = CreateAndSoftDeleteRockstarAuditTenant(authClient);
            
            using var db = appHost.GetDbConnection();
            
            void assertState(RockstarAuditTenant result)
            {
                Assert.That(result.Id, Is.EqualTo(id));
                Assert.That(result.FirstName, Is.EqualTo("Updated & Patched"));
                Assert.That(result.LastName, Is.EqualTo("Audit"));
                Assert.That(result.Age, Is.EqualTo(20));
                Assert.That(result.DateOfBirth.Date, Is.EqualTo(new DateTime(2002, 2, 2).Date));
                Assert.That(result.LivingStatus, Is.EqualTo(LivingStatus.Alive));

                Assert.That(result.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
                Assert.That(result.CreatedBy, Is.EqualTo("admin@email.com"));
                Assert.That(result.CreatedInfo, Is.EqualTo("Admin User (London)"));
                Assert.That(result.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
                Assert.That(result.ModifiedBy, Is.EqualTo("manager"));
                Assert.That(result.ModifiedInfo, Is.EqualTo("The Manager (Perth)"));
            }

            var crudEvents = db.Select<CrudEvent>();
            // events.PrintDump();
            Assert.That(crudEvents.Count, Is.EqualTo(4));
            Assert.That(crudEvents.Count(x => x.RequestType == nameof(CreateRockstarAuditTenant)), Is.EqualTo(1));
            Assert.That(crudEvents.Count(x => x.RequestType == nameof(UpdateRockstarAuditTenant)), Is.EqualTo(1));
            Assert.That(crudEvents.Count(x => x.RequestType == nameof(PatchRockstarAuditTenant)), Is.EqualTo(1));
            Assert.That(crudEvents.Count(x => x.RequestType == nameof(SoftDeleteAuditTenant)), Is.EqualTo(1));

            var newRockstar = db.SingleById<RockstarAuditTenant>(id);
            assertState(newRockstar);

            db.DeleteById<RockstarAuditTenant>(id);
            Assert.That(db.SingleById<RockstarAuditTenant>(id), Is.Null);
            
            // OrmLiteUtils.PrintSql();

            var eventsPlayer = new CrudEventsExecutor(appHost);
            foreach (var crudEvent in dbEvents.GetEvents(db))
            {
                await eventsPlayer.ExecuteAsync(crudEvent);
            }

            crudEvents = db.Select<CrudEvent>();
            Assert.That(crudEvents.Count, Is.EqualTo(4)); // Should not be any new events created by executor
            
            newRockstar = db.SingleById<RockstarAuditTenant>(id); //uses the same Id
            assertState(newRockstar); // State should be the same
        }

        [Test]
        public void Can_CreateRockstarAuditTenant()
        {
            var authClient = CreateAuthClient();
            CreateAndSoftDeleteRockstarAuditTenant(authClient);
        }

        private int CreateAndSoftDeleteRockstarAuditTenant(JsonServiceClient authClient)
        {
            using var db = appHost.GetDbConnection();
            db.DeleteAll<RockstarAuditTenant>();
            
            var createRequest = new CreateRockstarAuditTenant {
                FirstName = "Create",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002, 2, 2),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = authClient.Post(createRequest);
            var id = createResponse.Id;
            Assert.That(id, Is.GreaterThan(0));
            var result = createResponse.Result;

            Assert.That(result.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(createRequest.LivingStatus));

            var newRockstar = db.SingleById<RockstarAuditTenant>(id);
            Assert.That(newRockstar.TenantId, Is.EqualTo(10)); //admin.City London => 10
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(newRockstar.Age, Is.EqualTo(createRequest.Age));
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));

            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("Admin User (London)"));

            Assert.That(authClient.Get(new QueryRockstarAudit {Id = id}).Results.Count,
                Is.EqualTo(1));

            authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "manager",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var updateRequest = new UpdateRockstarAuditTenant {
                Id = id,
                FirstName = "Updated",
                LivingStatus = LivingStatus.Alive,
            };
            var updateResponse = authClient.Put(updateRequest);

            void assertUpdated(RockstarAuto result)
            {
                Assert.That(result.FirstName, Does.StartWith(updateRequest.FirstName));
                Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
                Assert.That(result.Age, Is.EqualTo(createRequest.Age));
                Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
                Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));
            }

            Assert.That(updateResponse.Id, Is.EqualTo(id));
            assertUpdated(updateResponse.Result);

            newRockstar = db.SingleById<RockstarAuditTenant>(id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Updated"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));

            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("The Manager (Perth)"));

            Assert.That(authClient.Get(new QueryRockstarAuditSubOr {
                    FirstNameStartsWith = "Up",
                    AgeOlderThan = 18,
                }).Results.Count,
                Is.EqualTo(1));

            var patchRequest = new PatchRockstarAuditTenant {
                Id = id,
                FirstName = updateRequest.FirstName + " & Patched"
            };
            var patchResponse = authClient.Patch(patchRequest);
            Assert.That(patchResponse.Result.FirstName, Is.EqualTo("Updated & Patched"));
            assertUpdated(patchResponse.Result);

            var softDeleteResponse = authClient.Put(new SoftDeleteAuditTenant {
                Id = id,
            });

            Assert.That(softDeleteResponse.Id, Is.EqualTo(id));
            assertUpdated(softDeleteResponse.Result);

            newRockstar = db.SingleById<RockstarAuditTenant>(id);
            Assert.That(newRockstar.SoftDeletedDate.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.SoftDeletedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.SoftDeletedInfo, Is.EqualTo("The Manager (Perth)"));

            Assert.That(authClient.Get(new QueryRockstarAudit {Id = id}).Results.Count,
                Is.EqualTo(0));

            Assert.That(authClient.Get(new QueryRockstarAuditSubOr {
                    FirstNameStartsWith = "Up",
                    AgeOlderThan = 18,
                }).Results.Count,
                Is.EqualTo(0));

            return id;
        }

        [Test]
        public void Can_CreateRockstarAuditTenant_with_RealDelete()
        {
            var authClient = CreateAuthClient();
            var id = CreateAndSoftDeleteRockstarAuditTenant(authClient);

            using var db = appHost.GetDbConnection();

            var realDeleteResponse = authClient.Delete(new RealDeleteAuditTenant {
                Id = id,
                Age = 99 //non matching filter
            });
            Assert.That(realDeleteResponse.Id, Is.EqualTo(id));
            Assert.That(realDeleteResponse.Count, Is.EqualTo(0));
            var newRockstar = db.SingleById<RockstarAuditTenant>(id);
            Assert.That(newRockstar, Is.Not.Null);

            realDeleteResponse = authClient.Delete(new RealDeleteAuditTenant {
                Id = id,
            });
            Assert.That(realDeleteResponse.Id, Is.EqualTo(id));
            Assert.That(realDeleteResponse.Count, Is.EqualTo(1));
            newRockstar = db.SingleById<RockstarAuditTenant>(id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public void Can_CreateRockstarAuditTenantGateway_Gateway()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var createRequest = new CreateRockstarAuditTenantGateway {
                FirstName = "CreateGateway",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };

            var createResponse = authClient.Post(createRequest);
            Assert.That(createResponse.Id, Is.GreaterThan(0));
            var result = createResponse.Result;

            var updateRequest = new UpdateRockstarAuditTenantGateway {
                Id = createResponse.Id,
                FirstName = "UpdatedGateway",
                LivingStatus = LivingStatus.Alive,
            };
            var updateResponse = authClient.Put(updateRequest);
            result = updateResponse.Result;
            
            Assert.That(updateResponse.Id, Is.EqualTo(createResponse.Id));
            Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));

            var patchRequest = new PatchRockstarAuditTenantGateway {
                Id = createResponse.Id,
                FirstName = "PatchedGateway",
                LivingStatus = LivingStatus.Alive,
            };
            var patchResponse = authClient.Patch(patchRequest);
            result = patchResponse.Result;
            
            Assert.That(updateResponse.Id, Is.EqualTo(createResponse.Id));
            Assert.That(result.FirstName, Is.EqualTo(patchRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(patchRequest.LivingStatus));

            var deleteRequest = authClient.Delete(new RealDeleteAuditTenantGateway {
                Id = createResponse.Id,
            });
            Assert.That(deleteRequest.Id, Is.EqualTo(createResponse.Id));
        }

        [Test]
        public void Can_CreateRockstarAuditTenantMq()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });
            
            Assert.That(authClient.GetTokenCookie(), Is.Not.Null);

            var createRequest = new CreateRockstarAuditTenantMq {
                FirstName = nameof(CreateRockstarAuditTenantMq),
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };

            authClient.Post(createRequest);
            
            using var db = appHost.GetDbConnection();

            ExecUtils.RetryUntilTrue(() => 
                db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(CreateRockstarAuditTenantMq)),
                TimeSpan.FromSeconds(2));
            var result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(CreateRockstarAuditTenantMq));
            
            var updateRequest = new UpdateRockstarAuditTenantMq {
                Id = result.Id,
                FirstName = nameof(UpdateRockstarAuditTenantMq),
                LivingStatus = LivingStatus.Alive,
            };
            authClient.Put(updateRequest);

            ExecUtils.RetryUntilTrue(() => 
                    db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(UpdateRockstarAuditTenantMq)),
                TimeSpan.FromSeconds(2));
            result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(UpdateRockstarAuditTenantMq));
            
            Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));

            var patchRequest = new PatchRockstarAuditTenantMq {
                Id = result.Id,
                FirstName = nameof(PatchRockstarAuditTenantMq),
                LivingStatus = LivingStatus.Alive,
            };
            authClient.Patch(patchRequest);

            ExecUtils.RetryUntilTrue(() => 
                    db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq)),
                TimeSpan.FromSeconds(2));
            result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq));
            
            Assert.That(result.FirstName, Is.EqualTo(patchRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(patchRequest.LivingStatus));

            authClient.Delete(new RealDeleteAuditTenantMq {
                Id = result.Id,
            });
            
            ExecUtils.RetryUntilTrue(() => 
                    !db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq)),
                TimeSpan.FromSeconds(2));
            
            Assert.That(db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq)), Is.False);
        }

        [Test]
        public void Can_CreateRockstarAuditTenant_OneWay()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            var authResponse = authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var createRequest = new CreateRockstarAuditTenant {
                FirstName = nameof(CreateRockstarAuditTenant),
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };

            authClient.SendOneWay(createRequest);
           
            ExecUtils.RetryUntilTrue(() => {
                    using var d = appHost.GetDbConnection();
                    return d.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(CreateRockstarAuditTenant));
                },
                TimeSpan.FromSeconds(2));
            
            using var db = appHost.GetDbConnection();
            var result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(CreateRockstarAuditTenant));
            
            var updateRequest = new UpdateRockstarAuditTenant {
                Id = result.Id,
                FirstName = nameof(UpdateRockstarAuditTenant),
                LivingStatus = LivingStatus.Alive,
            };
            authClient.SendOneWay(updateRequest);

            ExecUtils.RetryUntilTrue(() => {
                    using var d = appHost.GetDbConnection();
                    return d.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(UpdateRockstarAuditTenant));
                },
                TimeSpan.FromSeconds(2));
            result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(UpdateRockstarAuditTenant));
            
            Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));

            var patchRequest = new PatchRockstarAuditTenant {
                Id = result.Id,
                FirstName = nameof(PatchRockstarAuditTenant),
                LivingStatus = LivingStatus.Alive,
            };
            authClient.SendOneWay(patchRequest);

            ExecUtils.RetryUntilTrue(() => {
                    using var d = appHost.GetDbConnection();
                    return d.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenant));
                },
                TimeSpan.FromSeconds(2));
            result = db.Single<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenant));
            
            Assert.That(result.FirstName, Is.EqualTo(patchRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(patchRequest.LivingStatus));

            authClient.Delete(new RealDeleteAuditTenant {
                Id = result.Id,
            });
            
            ExecUtils.RetryUntilTrue(() => {
                    using var d = appHost.GetDbConnection();
                    return !d.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq));
                },
                TimeSpan.FromSeconds(2));
            
            Assert.That(db.Exists<RockstarAuditTenant>(x => x.FirstName == nameof(PatchRockstarAuditTenantMq)), Is.False);
        }

        [Test]
        public void Can_CreateRockstarAuditMqToken_OneWay()
        {
            var createRequest = new CreateRockstarAuditMqToken {
                BearerToken = JwtUserToken,
                FirstName = nameof(CreateRockstarAuditMqToken),
                LastName = "JWT",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };

            client.SendOneWay(createRequest);
            
            ExecUtils.RetryUntilTrue(() => {
                    using var db = appHost.GetDbConnection();
                    return db.Exists<RockstarAudit>(x => x.FirstName == nameof(CreateRockstarAuditMqToken));
                },
                TimeSpan.FromSeconds(2));

            using var db = appHost.GetDbConnection();
            var result = db.Single<RockstarAudit>(x => x.FirstName == nameof(CreateRockstarAuditMqToken));
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.FirstName, Is.EqualTo(nameof(CreateRockstarAuditMqToken)));
            Assert.That(result.LastName, Is.EqualTo("JWT"));
            Assert.That(result.LivingStatus, Is.EqualTo(LivingStatus.Dead));
            Assert.That(result.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(result.CreatedBy, Is.EqualTo("jwtuser"));
            Assert.That(result.CreatedInfo, Is.EqualTo("JWT User (Japan)"));
            Assert.That(result.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(result.ModifiedBy, Is.EqualTo("jwtuser"));
            Assert.That(result.ModifiedInfo, Is.EqualTo("JWT User (Japan)"));
        }
        
        [Test]
        public void Can_UpdateRockstarVersion()
        {
            var createResponse = client.Post(new CreateRockstarVersion {
                FirstName = "Create",
                LastName = "Version",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            });

            try 
            {
                client.Patch(new UpdateRockstarVersion {
                    Id = createResponse.Id, 
                    LastName = "UpdateVersion2",
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
            
            var response = client.Patch(new UpdateRockstarVersion {
                Id = createResponse.Id, 
                LastName = "UpdateVersion3",
                RowVersion = createResponse.RowVersion,
            });

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<RockstarVersion>(createResponse.Id);
            Assert.That(newRockstar.RowVersion, Is.Not.EqualTo(default(uint)));
            Assert.That(newRockstar.FirstName, Is.EqualTo("Create"));
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateVersion3"));

            try 
            {
                client.Patch(new UpdateRockstarVersion {
                    Id = createResponse.Id, 
                    LastName = "UpdateVersion4",
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
        }

        [Test]
        public void Can_NamedConnection_AutoCrud_Services()
        {
            var createRequest = new CreateNamedRockstar {
                Id = 10,
                FirstName = "Named",
                LastName = "SqlServer",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(createRequest);
            Assert.That(createResponse.Id, Is.EqualTo(10));
            Assert.That(createResponse.Result, Is.Not.Null);

            using var db = appHost.Resolve<IDbConnectionFactory>()
                .OpenDbConnection(AutoQueryAppHost.SqlServerNamedConnection);
            
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "SqlServer");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Named"));

            var updateRequest = new UpdateNamedRockstar {
                Id = 10,
                FirstName = "Updated",
                Age = 21,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Dead,
            };

            var updateResponse = client.Put(updateRequest);

            Assert.That(updateResponse.Id, Is.EqualTo(10));
            Assert.That(updateResponse.Result.FirstName, Is.EqualTo("Updated"));
            Assert.That(updateResponse.Result.Age, Is.EqualTo(21));
            Assert.That(updateResponse.Result.LivingStatus, Is.EqualTo(LivingStatus.Dead));
        }

        [Test]
        public void Can_ConnectionInfo_AutoCrud_Services()
        {
            var createRequest = new CreateConnectionInfoRockstar {
                Id = 11,
                FirstName = "Named",
                LastName = "SqlServer",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(createRequest);
            Assert.That(createResponse.Id, Is.EqualTo(11));
            Assert.That(createResponse.Result, Is.Not.Null);

            using var db = appHost.Resolve<IDbConnectionFactory>()
                .OpenDbConnection(AutoQueryAppHost.SqlServerNamedConnection);
            
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "SqlServer");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Named"));

            var updateRequest = new UpdateConnectionInfoRockstar {
                Id = 11,
                FirstName = "Updated",
                Age = 21,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Dead,
            };

            var updateResponse = client.Put(updateRequest);

            Assert.That(updateResponse.Id, Is.EqualTo(11));
            Assert.That(updateResponse.Result.FirstName, Is.EqualTo("Updated"));
            Assert.That(updateResponse.Result.Age, Is.EqualTo(21));
            Assert.That(updateResponse.Result.LivingStatus, Is.EqualTo(LivingStatus.Dead));
        }

        [Test]
        public void Can_Patch_DefaultFields_to_default_values()
        {
            var createRequest = new CreateDefaultValues {
                Id = 1,
                Bool = true,
                NBool = false,
                Int = 2,
                NInt = 3,
                String = "A",
            };
            var createResponse = client.Post(createRequest);
            AssertCreateDefaultValues(createRequest);
            
            var request = new PatchDefaultValues {
                Id = createRequest.Id,
                Reset = new[] {
                    nameof(PatchDefaultValues.Bool),
                    nameof(PatchDefaultValues.NBool),
                    nameof(PatchDefaultValues.Int),
                    nameof(PatchDefaultValues.NInt),
                    nameof(PatchDefaultValues.String),
                },
            };
            client.Patch(request);
            
            using var db = appHost.GetDbConnection();
            var row = db.SingleById<DefaultValue>(createRequest.Id);
            Assert.That(row.Bool, Is.EqualTo(default(bool)));
            Assert.That(row.NBool, Is.EqualTo(default(bool?)));
            Assert.That(row.Int, Is.EqualTo(default(int)));
            Assert.That(row.NInt, Is.EqualTo(default(int?)));
            Assert.That(row.String, Is.EqualTo(default(string)));
            
            Assert.Throws<WebServiceException>(() => client.Post(new PatchDefaultValues {
                Id = createRequest.Id,
                Reset = new[] { nameof(PatchDefaultValues.Id) },
            }));
        }

        private void AssertCreateDefaultValues(CreateDefaultValues createRequest)
        {
            using var db = appHost.GetDbConnection();
            var row = db.SingleById<DefaultValue>(createRequest.Id);
            Assert.That(row.Id, Is.EqualTo(createRequest.Id));
            Assert.That(row.Bool, Is.EqualTo(createRequest.Bool));
            Assert.That(row.NBool, Is.EqualTo(createRequest.NBool));
            Assert.That(row.Int, Is.EqualTo(createRequest.Int));
            Assert.That(row.NInt, Is.EqualTo(createRequest.NInt));
            Assert.That(row.String, Is.EqualTo(createRequest.String));
        }

        [Test]
        public void Does_ignore_unknown_properties_not_on_DataModel()
        {
            var createResponse = client.Post(new CreateRockstarUnknownField {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
                Unknown = "Field",
            });

            var queryResponse = client.Get(new QueryRockstarsUnknownField {
                Id = createResponse.Id,
                Unknown = "Field",
            });
            
            Assert.That(queryResponse.Results.Count, Is.EqualTo(1));
            
            var updateResponse = client.Put(new UpdateRockstarUnknownField {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
                Unknown = "Field",
            });
            
            var patchResponse = client.Patch(new PatchRockstarUnknownField {
                Id = createResponse.Id, 
                LastName = "PatchResult",
                Unknown = "Field",
            });
            
            var deleteResponse = client.Delete(new DeleteRockstarUnknownField {
                Id = createResponse.Id, 
                Unknown = "Field",
            });
        }

        [Test]
        public async Task Does_ignore_unknown_properties_not_on_DataModel_Async()
        {
            var createResponse = await client.PostAsync(new CreateRockstarUnknownField {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
                Unknown = "Field",
            });

            var queryResponse = await client.GetAsync(new QueryRockstarsUnknownField {
                Id = createResponse.Id,
                Unknown = "Field",
            });
            
            Assert.That(queryResponse.Results.Count, Is.EqualTo(1));
            
            var updateResponse = await client.PutAsync(new UpdateRockstarUnknownField {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
                Unknown = "Field",
            });
            
            var patchResponse = await client.PatchAsync(new PatchRockstarUnknownField {
                Id = createResponse.Id, 
                LastName = "PatchResult",
                Unknown = "Field",
            });
            
            var deleteResponse = await client.DeleteAsync(new DeleteRockstarUnknownField {
                Id = createResponse.Id, 
                Unknown = "Field",
            });
        }

        [Test]
        public void Does_not_allow_inserting_with_default_primary_key()
        {
            try
            {
                var response = client.Post(new CreateRockstarWithId());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Errors[0].ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Errors[0].FieldName, Is.EqualTo(nameof(Rockstar.Id)));
            }
        }

        [Test]
        public async Task Does_not_allow_inserting_with_default_primary_key_Async()
        {
            try
            {
                var response = await client.PostAsync(new CreateRockstarWithId());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Errors[0].ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Errors[0].FieldName, Is.EqualTo(nameof(Rockstar.Id)));
            }
        }

        [Test]
        public void Does_apply_Audit_behavior()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var booking1Id = authClient.Post(new CreateBooking {
                RoomNumber = 1,
                BookingStartDate = DateTime.Today.AddDays(1),
                BookingEndDate = DateTime.Today.AddDays(5),
                Cost = 100,
            }).Id.ToInt();
            var booking2Id = authClient.Post(new CreateBooking {
                RoomNumber = 2,
                BookingStartDate = DateTime.Today.AddDays(2),
                BookingEndDate = DateTime.Today.AddDays(6),
                Cost = 200,
            }).Id.ToInt();

            var bookings = client.Get(new QueryBookings {
                Ids = new []{ booking1Id, booking2Id }
            });
            
            // bookings.PrintDump();
            Assert.That(bookings.Results.Count, Is.EqualTo(2));

            Assert.That(bookings.Results.All(x => x.CreatedBy != null));
            Assert.That(bookings.Results.All(x => x.CreatedDate >= DateTime.UtcNow.Date));
            Assert.That(bookings.Results.All(x => x.ModifiedBy != null));
            Assert.That(bookings.Results.All(x => x.ModifiedDate >= DateTime.UtcNow.Date));
            Assert.That(bookings.Results.All(x => x.ModifiedDate == x.CreatedDate));

            authClient.Patch(new UpdateBooking {
                Id = booking1Id,
                Cancelled = true,
                Notes = "Missed Flight",
            });
            var booking1 = client.Get(new QueryBookings {
                Ids = new[] { booking1Id }
            }).Results[0];
            Assert.That(booking1.Cancelled, Is.True);
            Assert.That(booking1.Notes, Is.EqualTo("Missed Flight"));
            Assert.That(booking1.ModifiedDate, Is.Not.EqualTo(booking1.CreatedDate));

            authClient.Delete(new DeleteBooking {
                Id = booking2Id,
            });
            var booking2 = client.Get(new QueryBookings {
                Ids = new[] { booking2Id }
            }).Results?.FirstOrDefault();
            Assert.That(booking2, Is.Null);

            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            booking2 = db.SingleById<Booking>(booking2Id);
            // booking2.PrintDump();
            Assert.That(booking2, Is.Not.Null);
            Assert.That(booking2.DeletedBy, Is.Not.Null);
            Assert.That(booking2.DeletedDate, Is.Not.Null);
            
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "manager",
                Password = "p@55wOrd",
                RememberMe = true,
            });
            var booking3Id = authClient.Post(new CreateBooking {
                RoomNumber = 3,
                BookingStartDate = DateTime.Today.AddDays(3),
                BookingEndDate = DateTime.Today.AddDays(7),
                Cost = 100,
            }).Id.ToInt();

            var managerBookings = authClient.Get(new QueryUserBookings());
            Assert.That(managerBookings.Results.Count, Is.EqualTo(1));
            Assert.That(managerBookings.Results[0].RoomNumber, Is.EqualTo(3));

            managerBookings = authClient.Get(new QueryUserMapBookings());
            Assert.That(managerBookings.Results.Count, Is.EqualTo(1));
            Assert.That(managerBookings.Results[0].RoomNumber, Is.EqualTo(3));

            managerBookings = authClient.Get(new QueryEnsureUserBookings());
            Assert.That(managerBookings.Results.Count, Is.EqualTo(1));
            Assert.That(managerBookings.Results[0].RoomNumber, Is.EqualTo(3));
        }

        [Test]
        public void Can_override_custom_Batch_Crud_Operation()
        {
            using var db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.DropAndCreateTable<Booking>();
            
            var items = new CustomCreateBooking[] {
                new() { RoomType = RoomType.Double, RoomNumber = 10, Cost = 100, BookingStartDate = new DateTime(2021,01,01) }, 
                new() { RoomType = RoomType.Queen, RoomNumber = 11, Cost = 200, BookingStartDate = new DateTime(2021,01,02) }, 
                new() { RoomType = RoomType.Single, RoomNumber = 12, Cost = 300, BookingStartDate = new DateTime(2021,01,03) }, 
                new() { RoomType = RoomType.Suite, RoomNumber = 13, Cost = 400, BookingStartDate = new DateTime(2021,01,04) }, 
                new() { RoomType = RoomType.Twin, RoomNumber = 14, Cost = 500, BookingStartDate = new DateTime(2021,01,05) }, 
            };

            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var responses = authClient.SendAll(items);
            var responseIds = responses.Map(x => x.Id.ToInt());
            var results = db.SelectByIds<Booking>(responseIds);
            Assert.That(results.Map(x => x.RoomNumber), Is.EquivalentTo(new[]{ 10, 11, 12, 13, 14 }));
            
            db.DropAndCreateTable<Booking>();

            items[2].RoomNumber = 0; //Validation Error
            try
            {
                responses = authClient.SendAll(items);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.Message, Is.EqualTo("'Room Number' must be greater than '0'."));
            }
            Assert.That(db.SelectByIds<Booking>(responseIds).Count, Is.EqualTo(0));
            
            items[2].RoomNumber = 500; //DB Check Constraint Error
            try
            {
                responses = authClient.SendAll(items);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.Message, Does.Contain("CHECK constraint failed"));
            }
            Assert.That(db.SelectByIds<Booking>(responseIds).Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_execute_AutoBatch_CRUD_Create_Operation()
        {
            using var db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.DropAndCreateTable<Booking>();
            
            var items = new CreateBooking[] {
                new() { RoomType = RoomType.Double, RoomNumber = 10, Cost = 100, BookingStartDate = new DateTime(2021,01,01) }, 
                new() { RoomType = RoomType.Queen, RoomNumber = 11, Cost = 200, BookingStartDate = new DateTime(2021,01,02) }, 
                new() { RoomType = RoomType.Single, RoomNumber = 12, Cost = 300, BookingStartDate = new DateTime(2021,01,03) }, 
                new() { RoomType = RoomType.Suite, RoomNumber = 13, Cost = 400, BookingStartDate = new DateTime(2021,01,04) }, 
                new() { RoomType = RoomType.Twin, RoomNumber = 14, Cost = 500, BookingStartDate = new DateTime(2021,01,05) }, 
            };
            
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var responses = authClient.SendAll(items);
            var responseIds = responses.Map(x => x.Id.ToInt());
            var results = db.SelectByIds<Booking>(responseIds);
            Assert.That(results.Map(x => x.RoomNumber), Is.EquivalentTo(new[]{ 10, 11, 12, 13, 14 }));
            
            db.DropAndCreateTable<Booking>();

            items[2].RoomNumber = 0; //Validation Error
            try
            {
                responses = authClient.SendAll(items);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.Message, Is.EqualTo("'Room Number' must be greater than '0'."));
            }
            Assert.That(db.SelectByIds<Booking>(responseIds).Count, Is.EqualTo(0));
            
            items[2].RoomNumber = 500; //DB Check Constraint Error
            try
            {
                responses = authClient.SendAll(items);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.Message, Does.Contain("CHECK constraint failed"));
            }
            Assert.That(db.SelectByIds<Booking>(responseIds).Count, Is.EqualTo(0));
        }
    }
}