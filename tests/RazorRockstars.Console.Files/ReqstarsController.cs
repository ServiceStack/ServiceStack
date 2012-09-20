using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace Alternate.ExpressLike.Controller.Proposal
{
    /// <summary>
    /// Proposal for new Express routes controller, optimized for capturing loosely-related, unstructured adhoc routes...  see comments in-line
    /// To align with ServiceStack's message-based design, a "Controller Method": 
    ///   - only supports a single argument, either a typed Request DTO or a Dynamic Request
    ///   - only returns object or void
    ///
    /// Notes: 
    /// Content Negotiation built-in, i.e. by default each method/route is automatically available in every registered Content-Type (HTTP Only).
    /// Express routes wont be accessible in ServiceStack's typed service clients.
    /// Any Views rendered is based on Returned DTO type, see: http://razor.servicestack.net/#unified-stack
    /// </summary>
    public class ReqstarsController : Express
    {
        public ReqstarsController() : base("/reqexpress") { } //equivalent to /reqexpress prefix on every route

        public IDbConnectionFactory DbFactory { get; set; } //auto-wired

        [Route("/", "GET")]
        [Route("/aged/{Age}", "GET")]
        public object GetAll(Reqstars request) //Allow any typed Request DTO 
        {
            using (var db = DbFactory.Open())
            {
                return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
                {
                    Aged = request.Age,
                    Total = db.GetScalar<int>("select count(*) from Reqstar"),
                    Results = request.Age.HasValue ?
                        db.Select<Reqstar>(q => q.Age == request.Age.Value)
                          : db.Select<Reqstar>()
                };
            }
        }

        [Route("/{Id}", "GET")]
        public object Get(IntId request) //Returning generic types (or collections) directly wont match HTML views (still good for JSON,XML,etc)
        {
            return DbFactory.Run(db => db.Select<Reqstar>(q => q.Id == request.Id));
        }

        [Route("/", "POST")]
        public object Create(Reqstar request)
        {
            using (var db = DbFactory.Open())
            {
                db.Insert(request.TranslateTo<Reqstar>());
                return GetAll(new Reqstars());
            }
        }

        //public methods with no routes uses its name - i.e. equivalent to [Route("/reset")] 
        public void Reset(Empty request)
        {
            using (var db = DbFactory.Open())
            {
                db.DeleteAll<Reqstar>();
                db.Insert(Reqstar.SeedData);
            }
        }

        [Route("/{Id}/delete")]
        public void Delete(DynamicRequest request) //example of Dynamic/Expando request
        {
            using (var db = DbFactory.Open())
            {
                db.DeleteById<Reqstar>(request.Id);
            }
        }
    }
    

    //Types used by the Controller
    public class Reqstar
    {
        public static Reqstar[] SeedData = new[] {
            new Reqstar(1, "Foo", "Bar", 20), 
            new Reqstar(2, "Something", "Else", 30), 
        };

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
    public class ReqstarsResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Reqstar> Results { get; set; }
    }

    public class ReqstarResponse
    {
        public Reqstar Result { get; set; }
    }

    public class Reqstars
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string Delete { get; set; }
    }

    public class Empty { }

    public class IntId
    {
        public int Id { get; set; }
    }

    public class DynamicRequest /*: Will be dynamic / Expando */
    {
        public int Id { get; set; }
    }

}