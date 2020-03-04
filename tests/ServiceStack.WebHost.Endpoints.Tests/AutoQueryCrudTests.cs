using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RockstarBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    [Alias(nameof(Rockstar))]
    public class RockstarAuto : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
    }
    
    public class RockstarAutoGuid : RockstarBase
    {
        [AutoId]
        public Guid Id { get; set; }
    }
    
    public class CreateRockstar : RockstarBase, ICreateDb<RockstarAuto>, IReturn<CreateRockstarResponse>
    {
    }

    public class CreateRockstarResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarWithReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturn<CreateRockstarWithResultResponse>
    {
    }
    public class CreateRockstarWithVoidReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturnVoid
    {
    }

    public class CreateRockstarWithAutoGuid : RockstarBase, ICreateDb<RockstarAutoGuid>, IReturn<CreateRockstarWithReturnGuidResponse>
    {
    }
    
    public class CreateRockstarWithResultResponse
    {
        public int Id { get; set; }
        public RockstarAuto Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class CreateRockstarWithReturnGuidResponse
    {
        public Guid Id { get; set; }
        public RockstarAutoGuid Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarAdhocNonDefaults : ICreateDb<RockstarAuto>, IReturn<CreateRockstarWithResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Eval = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoDefault(Value = global::ServiceStack.WebHost.Endpoints.Tests.LivingStatus.Dead)]
        public LivingStatus? LivingStatus { get; set; }
    }

    public class CreateRockstarAutoMap : ICreateDb<RockstarAuto>, IReturn<CreateRockstarWithResultResponse>
    {
        [AutoMap(nameof(RockstarAuto.FirstName))]
        public string MapFirstName { get; set; }

        [AutoMap(nameof(RockstarAuto.LastName))]
        public string MapLastName { get; set; }
        
        [AutoMap(nameof(RockstarAuto.Age))]
        [AutoDefault(Value = 21)]
        public int? MapAge { get; set; }
        
        [AutoMap(nameof(RockstarAuto.DateOfBirth))]
        [AutoDefault(Eval = "date(2001,1,1)")]
        public DateTime MapDateOfBirth { get; set; }

        [AutoMap(nameof(RockstarAuto.DateDied))]
        [AutoDefault(Eval = "utcNow")]
        public DateTime? MapDateDied { get; set; }
        
        [AutoMap(nameof(RockstarAuto.LivingStatus))]
        [AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus? MapLivingStatus { get; set; }
    }

    public class UpdateRockstar : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    public class PatchRockstar : RockstarBase, IPatchDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    [AutoUpdate(AutoUpdateStyle.NonDefaults)]
    public class UpdateRockstarNonDefaults : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateRockstarAdhocNonDefaults : IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults)]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Eval = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class DeleteRockstar : IDeleteDb<Rockstar>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }
    
    public class DeleteRockstarFilters : IDeleteDb<Rockstar>, IReturn<DeleteRockstarCountResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
    }

    public class DeleteRockstarCountResponse
    {
        public int Count { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class AutoQueryCrudTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public AutoQueryCrudTests()
        {
            appHost = new AutoQueryAppHost()
                .Init()
                .Start(Config.ListeningOn);
            
            using var db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.CreateTable<RockstarAutoGuid>();

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
        public void Can_UpdateRockstarNonDefaults()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new UpdateRockstarNonDefaults {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Put(request);

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
                client.Post(new UpdateRockstar {
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

    }
}