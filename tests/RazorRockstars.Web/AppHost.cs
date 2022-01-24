using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Funq;
using RazorRockstars.Web.Tests;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.MsgPack;
using ServiceStack.OrmLite;
using ServiceStack.Razor;

//The entire C# code for the stand-alone RazorRockstars demo.
namespace RazorRockstars.Web
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("Test Razor", typeof (AppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());
            Plugins.Add(new MsgPackFormat());

            var metadata = (MetadataFeature)Plugins.First(x => x is MetadataFeature);
            metadata.IndexPageFilter = page => {
                page.OperationNames.Sort((x,y) => y.CompareTo(x));
            };

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            InitData(container);

            SetConfig(new HostConfig {
                DebugMode = true,
            });

            this.CustomErrorHttpHandlers[HttpStatusCode.ExpectationFailed] = new RazorHandler("/expectationfailed");
        }

        public static void InitData(Container container)
        {
            using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                db.CreateTableIfNotExists<Rockstar>();
                db.Insert(Rockstar.SeedData); //Populate with seed data

                db.DropAndCreateTable<Reqstar>();
                db.Insert(SeedData.Reqstars);
            }
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
        [DataMember]
        public int Total { get; set; }
        [DataMember]
        public int? Aged { get; set; }
        [DataMember]
        public List<Rockstar> Results { get; set; }
    }

    public class SimpleModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [FallbackRoute("/{Path}")]
    public class Fallback
    {
        public string Path { get; set; }
        public string PathInfo { get; set; }
    }

    public class RockstarsService : Service
    {
        public object Any(Fallback request)
        {
            request.PathInfo = base.Request.PathInfo;
            return request;
        }

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

            var response = new RockstarsResponse
            {
                Aged = request.Age,
                Total = Db.Scalar<int>("select count(*) from Rockstar"),
                Results = request.Id != default(int) ?
                    Db.Select<Rockstar>(q => q.Id == request.Id)
                      : request.Age.HasValue ?
                    Db.Select<Rockstar>(q => q.Age == request.Age.Value)
                      : Db.Select<Rockstar>()
            };

            if (request.View != null || request.Template != null)
                return new HttpResult(response)
                {
                    View = request.View,
                    Template = request.Template,
                };

            return response;
        }

        public object Post(Rockstars request)
        {
            Db.Insert(request.ConvertTo<Rockstar>());
            return Get(new Rockstars());
        }
    }

    [Route("/routeinfo/{Path*}")]
    public class GetRouteInfo
    {
        public string Path { get; set; }
    }

    public class GetRouteInfoResponse
    {
        public string BaseUrl { get; set; }
        public string ResolvedUrl { get; set; }
    }

    public class RouteInfoService : Service
    {
        public object Any(GetRouteInfo request)
        {
            return new GetRouteInfoResponse
            {
                BaseUrl = base.Request.GetBaseUrl(),
                ResolvedUrl = base.Request.ResolveAbsoluteUrl("~/resolved")
            };
        }
   }
}