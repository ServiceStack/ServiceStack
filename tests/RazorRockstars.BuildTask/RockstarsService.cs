using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace RazorRockstars
{
    [Route("/rockstars")]
    [Route("/rockstars/{Id}")]
    [Route("/rockstars/aged/{Age}")]
    public class Rockstars
    {
        public int? Age { get; set; }
        public int Id { get; set; }
    }

    [Route("/rockstars/delete/{Id}")]
    public class DeleteRockstar
    {
        public int Id { get; set; }
    }

    [Route("/rockstars/delete/reset")]
    public class ResetRockstars { }

    [Csv(CsvBehavior.FirstEnumerable)]
    public class RockstarsResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Rockstar> Results { get; set; }
    }

    //Poco Data Model for OrmLite + SeedData 
    [Route("/rockstars", "POST")]
    public class Rockstar
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public bool Alive { get; set; }

        public string Url
        {
            get { return "/stars/{0}/{1}".Fmt(Alive ? "alive" : "dead", LastName.ToLower()); }
        }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age, bool alive)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            Alive = alive;
        }
    }

    [ClientCanSwapTemplates]
    [DefaultView("Rockstars")]
    public class RockstarsService : Service
    {
        public static Rockstar[] SeedData = new[] {
            new Rockstar(1, "Jimi", "Hendrix", 27, false), 
            new Rockstar(2, "Janis", "Joplin", 27, false), 
            new Rockstar(4, "Kurt", "Cobain", 27, false),              
            new Rockstar(5, "Elvis", "Presley", 42, false), 
            new Rockstar(6, "Michael", "Jackson", 50, false), 
            new Rockstar(7, "Eddie", "Vedder", 47, true), 
            new Rockstar(8, "Dave", "Grohl", 43, true), 
            new Rockstar(9, "Courtney", "Love", 48, true), 
            new Rockstar(10, "Bruce", "Springsteen", 62, true), 
        };

        public object Get(Rockstars request)
        {
            return new RockstarsResponse {
                Aged = request.Age,
                Total = Db.Scalar<int>("select count(*) from Rockstar"),
                Results = request.Id != default(int) 
                    ? Db.Select<Rockstar>(q => q.Id == request.Id)
                    : request.Age.HasValue 
                        ? Db.Select<Rockstar>(q => q.Age == request.Age.Value)
                        : Db.Select<Rockstar>()
            };
        }

        public object Any(DeleteRockstar request)
        {
            Db.DeleteById<Rockstar>(request.Id);
            return Get(new Rockstars());
        }

        public object Post(Rockstar request)
        {
            Db.Insert(request);
            return Get(new Rockstars());
        }

        public object Any(ResetRockstars request)
        {
            Db.DropAndCreateTable<Rockstar>();
            Db.InsertAll(SeedData);
            return Get(new Rockstars());
        }
    }
}
