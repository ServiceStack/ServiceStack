using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Razor;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Auth.Tests
{
    public class DataSource
    {
        public string[] Items = new[] { "Eeny", "meeny", "miny", "moe" };
    }

    public class Rockstar
    {
        public static Rockstar[] SeedData = new[] {
            new Rockstar(1, "Jimi", "Hendrix", 27), 
            new Rockstar(2, "Janis", "Joplin", 27), 
            new Rockstar(3, "Jim", "Morrisson", 27), 
            new Rockstar(4, "Kurt", "Cobain", 27),              
            new Rockstar(5, "Elvis", "Presley", 42), 
            new Rockstar(6, "Michael", "Jackson", 50), 
        };

        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public bool Alive { get; set; }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }
    
    public class RazorAppHost : AppHostHttpListenerBase
    {
        public RazorAppHost()
            : base("Test Razor", typeof(RazorAppHost).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());

            container.Register(new DataSource());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", false, SqliteOrmLiteDialectProvider.Instance));

            using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                db.CreateTable<Rockstar>(overwrite: false);
                db.Insert(Rockstar.SeedData);
            }
        }
    }


    [Route("/rockstars")]
    [Route("/rockstars/aged/{Age}")]
    [Route("/rockstars/delete/{Delete}")]
    [Route("/rockstars/{Id}")]
    public class Rockstars
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string Delete { get; set; }
    }

    [DataContract]
    public class RockstarsResponse
    {
        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int? Aged { get; set; }

        [DataMember]
        public List<Rockstar> Results { get; set; }
    }

    [DefaultRequest(typeof(Rockstars))]
    public class RockstarsService : ServiceInterface.Service
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public object Get(Rockstars request)
        {
            if (request.Delete == "reset")
            {
                Db.DeleteAll<Rockstar>();
                Db.Insert(Rockstar.SeedData);
            }
            else if (request.Delete.IsInt())
            {
                Db.DeleteById<Rockstar>(request.Delete.ToInt());
            }

            return new RockstarsResponse
            {
                Aged = request.Age,
                Total = Db.GetScalar<int>("select count(*) from Rockstar"),
                Results = request.Id != default(int) ?
                    Db.Select<Rockstar>(q => q.Id == request.Id)
                      : request.Age.HasValue ?
                    Db.Select<Rockstar>(q => q.Age == request.Age.Value)
                      : Db.Select<Rockstar>()
            };
        }

        public object Post(Rockstars request)
        {
            Db.Insert(request.TranslateTo<Rockstar>());
            return Get(new Rockstars());
        }
    }

    [Route("/viewmodel/{Id}")]
    public class ViewThatUsesLayoutAndModel
    {
        public string Id { get; set; }
    }

    public class ViewThatUsesLayoutAndModelResponse
    {
        public string Name { get; set; }
        public List<string> Results { get; set; }
    }

    public class ViewService : ServiceInterface.Service
    {
        public object Any(ViewThatUsesLayoutAndModel request)
        {
            return new ViewThatUsesLayoutAndModelResponse {
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

    [NUnit.Framework.Ignore]
    [TestFixture]
    public class RazorAppHostTests
    {
        [Test]
        public void Hold_open_for_10Mins()
        {
            using (var appHost = new RazorAppHost())
            {
                appHost.Init();
                appHost.Start("http://localhost:11000/");
                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }
    }
}
