// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    // Request DTOs
    public class FindRockstars
    {
        public int? Aged { get; set; }
        public bool? Alive { get; set; }
    }

    public class GetStatus
    {
        public string LastName { get; set; }
    }

    public class RockstarStatus
    {
        public int Age { get; set; }
        public bool Alive { get; set; }
    }

    // Types
    public class Rockstar
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }

    // Implementation
    public class SimpleService : Service
    {
        public IRockstarRepository RockstarRepository { get; set; }

        public List<Rockstar> Get(FindRockstars request)
        {
            return request.Aged.HasValue
                ? Db.Select<Rockstar>(q => q.Age == request.Aged.Value)
                : Db.Select<Rockstar>();
        }

        public RockstarStatus Get(GetStatus request)
        {
            var rockstar = RockstarRepository.GetByLastName(request.LastName);
            if (rockstar == null)
                throw HttpError.NotFound("'{0}' is not a Rockstar".Fmt(request.LastName));

            var status = new RockstarStatus
            {
                Alive = RockstarRepository.IsAlive(request.LastName)
            }.PopulateWith(rockstar); //Populates with matching fields

            return status;
        }
    }

    //Custom Repository
    public interface IRockstarRepository
    {
        Rockstar GetByLastName(string lastName);
        bool IsAlive(string lastName);
    }

    public class RockstarRepository : RepositoryBase, IRockstarRepository
    {
        public Rockstar GetByLastName(string lastName)
        {
            return Db.FirstOrDefault<Rockstar>(q => q.LastName == lastName);
        }

        readonly HashSet<string> fallenLegends = new HashSet<string> {
            "Hendrix", "Hendrix", "Cobain", "Presley", "Jackson"
        };

        public bool IsAlive(string lastName)
        {
            return !fallenLegends.Contains(lastName);
        }
    }

    //Use base class to keep common boilerplate
    public class RepositoryBase : IDisposable
    {
        public IDbConnectionFactory DbFactory { get; set; }

        IDbConnection db;
        protected IDbConnection Db
        {
            get { return db ?? (db = DbFactory.Open()); }
        }

        public void Dispose()
        {
            if (db != null)
                db.Dispose();
        }
    }

    [TestFixture]
    public class UnitTestExample
    {
        public static List<Rockstar> SeedData = new[] {
            new Rockstar(1, "Jimi", "Hendrix", 27), 
            new Rockstar(2, "Jim", "Hendrix", 27), 
            new Rockstar(3, "Kurt", "Cobain", 27),              
            new Rockstar(4, "Elvis", "Presley", 42), 
            new Rockstar(5, "David", "Grohl", 44), 
            new Rockstar(6, "Eddie", "Vedder", 48), 
            new Rockstar(7, "Michael", "Jackson", 50), 
        }.ToList();

        private Container container;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            container = new Container();

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", false, SqliteDialect.Provider));

            container.RegisterAutoWiredAs<RockstarRepository, IRockstarRepository>();

            container.RegisterAutoWired<SimpleService>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(SeedData);
            }
        }

        [Test]
        public void Using_in_memory_database()
        {
            var service = container.Resolve<SimpleService>();  //Resolve auto-wired service
            service.SetResolver(new BasicResolver(container)); //For Service base class dependencies

            var rockstars = service.Get(new FindRockstars { Aged = 27 });

            rockstars.PrintDump(); //Print results to screen

            Assert.That(rockstars.Count, Is.EqualTo(SeedData.Count(x => x.Age == 27)));

            var status = service.Get(new GetStatus { LastName = "Vedder" });
            Assert.That(status.Age, Is.EqualTo(48));
            Assert.That(status.Alive, Is.True);

            status = service.Get(new GetStatus { LastName = "Hendrix" });
            Assert.That(status.Age, Is.EqualTo(27));
            Assert.That(status.Alive, Is.False);

            Assert.Throws<HttpError>(() =>
                service.Get(new GetStatus { LastName = "Unknown" }));
        }

        public class RockstarRepositoryMock : IRockstarRepository
        {
            public Rockstar GetByLastName(string lastName)
            {
                return lastName == "Vedder"
                    ? new Rockstar(6, "Eddie", "Vedder", 48)
                    : null;
            }

            public bool IsAlive(string lastName)
            {
                return lastName == "Grohl" || lastName == "Vedder";
            }
        }

        [Test]
        public void Using_manual_dependency_injection()
        {
            var service = new SimpleService
            {
                RockstarRepository = new RockstarRepositoryMock()
            };

            var status = service.Get(new GetStatus { LastName = "Vedder" });
            Assert.That(status.Age, Is.EqualTo(48));
            Assert.That(status.Alive, Is.True);

            Assert.Throws<HttpError>(() =>
                service.Get(new GetStatus { LastName = "Hendrix" }));
        }
    }
}