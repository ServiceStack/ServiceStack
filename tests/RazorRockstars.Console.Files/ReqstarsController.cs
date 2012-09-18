using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    public class Reqstar
    {
        public static Reqstar[] SeedData = new[] {
            new Reqstar(1, "Jimi", "Hendrix", 27), 
            new Reqstar(2, "Janis", "Joplin", 27), 
            new Reqstar(3, "Jim", "Morrisson", 27), 
            new Reqstar(4, "Kurt", "Cobain", 27),              
            new Reqstar(5, "Elvis", "Presley", 42), 
            new Reqstar(6, "Michael", "Jackson", 50), 
        };

        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        public Reqstar() { }
        public Reqstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }

    [Csv(CsvBehavior.FirstEnumerable)]
    public class ReqstarResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Reqstar> Results { get; set; }
    }

    public class Reqstars
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string Delete { get; set; }
        public string View { get; set; }
        public string Template { get; set; }
    }

    public class Empty {}

    public class IntId
    {
        public int Id { get; set; }
    }

    public class DynamicRequest /*: Expando*/
    {
        public int Id { get; set; }        
    } 


    /// <summary>
    /// Proposal for new Express routes controller... see comments in-line
    /// Only supports single argument, typed Request DTO or Dynamic Request
    /// Supports object or void return types only
    /// </summary>
    public class ReqstarsController : Express
    {
        public ReqstarsController() : base("/rockexpress") {} //equivalent to /rockexpress prefix on every route

        public IDbConnectionFactory DbFactory { get; set; } //auto-wired

        [Route("/", "GET")]
        [Route("/aged/{Age}", "GET")]
        public object GetAll(Reqstars request) //Allow any typed Request DTO 
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                var response = new RockstarsResponse {
                    Aged = request.Age,
                    Total = db.GetScalar<int>("select count(*) from Rockstar"),
                    Results = request.Age.HasValue ?
                        db.Select<Rockstar>(q => q.Age == request.Age.Value)
                          : db.Select<Rockstar>()
                };

                if (request.View != null || request.Template != null)
                    return new HttpResult(response) {
                        View = request.View,
                        Template = request.Template,
                    };

                return response;
            }
        }

        [Route("/", "POST")]
        public object Create(Reqstar request)
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                db.Insert(request.TranslateTo<Reqstar>());
                return GetAll(new Reqstars());
            }
        }
        
        [Route("/{Id}", "GET")]
        public object Get(IntId request)
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                return new RockstarsResponse {
                    Results = db.Select<Rockstar>(q => q.Id == request.Id),
                };
            }
        }

        //public methods with no routes uses its name - equivalent to [Route("/reset")] 
        public void Reset(Empty request)
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                db.DeleteAll<Rockstar>();
                db.Insert(Rockstar.SeedData);
            }
        }

        [Route("/{Id}/delete")]
        public void Delete(DynamicRequest request) //example of Dynamic/Expando request
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                db.DeleteById<Rockstar>(request.Id);
            }
        }
    }
}