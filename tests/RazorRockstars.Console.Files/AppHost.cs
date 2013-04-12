using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Funq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Razor2;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

//The entire C# code for the stand-alone RazorRockstars demo.
namespace RazorRockstars.Console.Files
{
    public class AppHost : AppHostHttpListenerBase 
    {
        public AppHost() : base("Test Razor", typeof(AppHost).Assembly) { }

        public bool EnableRazor = true;

        public override void Configure(Container container)
        {
            if (EnableRazor)
                Plugins.Add(new RazorFormat());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", false, SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                db.DropAndCreateTable<Rockstar>(); //Create table if not exists
                db.Insert(Rockstar.SeedData); //Populate with seed data
            }
		}

        private static void Main(string[] args)
        {
            var appHost = new AppHost();
            appHost.Init();
            appHost.Start("http://*:1337/");
            System.Console.WriteLine("Listening on http://localhost:1337/ ...");
            System.Console.ReadLine();
			System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
		}
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

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
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
        public string View { get; set; }
        public string Template { get; set; }
    }

    [DataContract] //Attrs for CSV Format to recognize it's a DTO and serialize the Enumerable property
    public class RockstarsResponse
    {
        [DataMember] public int Total { get; set; }
        [DataMember] public int? Aged { get; set; }
        [DataMember] public List<Rockstar> Results { get; set; }
    }

    [Route("/ilist1/{View}")]
    public class IList1
    {
        public string View { get; set; }
    }

    [Route("/ilist2/{View}")]
    public class IList2
    {
        public string View { get; set; }
    }

    [Route("/ilist3/{View}")]
    public class IList3
    {
        public string View { get; set; }
    }

    [Route("/partialmodel")]
    public class PartialModel
    {
        public IEnumerable<PartialChildModel> Items { get; set; }
    }
    public class PartialChildModel
    {
        public string SomeProperty { get; set; }
    }

    public class RockstarsService : Service
    {
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

            var response = new RockstarsResponse {
                Aged = request.Age,
                Total = Db.GetScalar<int>("select count(*) from Rockstar"),
                Results = request.Id != default(int) ?
                    Db.Select<Rockstar>(q => q.Id == request.Id)
                      : request.Age.HasValue ?
                    Db.Select<Rockstar>(q => q.Age == request.Age.Value)
                      : Db.Select<Rockstar>()
            };

            if (request.View != null || request.Template != null)
                return new HttpResult(response) {
                    View = request.View,
                    Template = request.Template,
                };

            return response;
        }

        public object Post(Rockstars request)
        {
            Db.Insert(request.TranslateTo<Rockstar>());
            return Get(new Rockstars());
        }
        
        public IList<Rockstar> Get(IList1 request)
        {
            base.Request.Items["View"] = request.View;
            return Db.Select<Rockstar>();
        }

        public List<Rockstar> Get(IList2 request)
        {
            base.Request.Items["View"] = request.View;
            return Db.Select<Rockstar>();
        }

        public object Get(IList3 request)
        {
            base.Request.Items["View"] = request.View;
            return Db.Select<Rockstar>();
        }

        public PartialModel Any(PartialModel request)
        {
            return new PartialModel
            {
                Items = 5.Times(x => new PartialChildModel {
                    SomeProperty = "value " + x
                })
            };
        }
    }
}
