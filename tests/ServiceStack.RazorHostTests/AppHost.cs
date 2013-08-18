using System.Collections.Generic;
using System.Runtime.Serialization;
using Funq;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Razor;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.RazorHostTests
{
    [Route( "/rockstars" )]
    [Route( "/rockstars/aged/{Age}" )]
    [Route( "/rockstars/delete/{Delete}" )]
    [Route( "/rockstars/{Id}" )]
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

    public class RockstarsService : RestServiceBase<Rockstars>
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public override object OnGet( Rockstars request )
        {
            using( var db = DbFactory.OpenDbConnection() )
            {
                if( request.Delete == "reset" )
                {
                    db.DeleteAll<Rockstar>();
                    db.Insert( Rockstar.SeedData );
                }
                else if( request.Delete.IsInt() )
                {
                    db.DeleteById<Rockstar>( request.Delete.ToInt() );
                }

                return new RockstarsResponse
                {
                    Aged = request.Age,
                    Total = db.GetScalar<int>( "select count(*) from Rockstar" ),
                    Results = request.Id != default( int ) ?
                        db.Select<Rockstar>( q => q.Id == request.Id )
                          : request.Age.HasValue ?
                        db.Select<Rockstar>( q => q.Age == request.Age.Value )
                          : db.Select<Rockstar>()
                };
            }
        }

        public override object OnPost( Rockstars request )
        {
            using( var db = DbFactory.OpenDbConnection() )
            {
                db.Insert( request.TranslateTo<Rockstar>() );
                return OnGet( new Rockstars() );
            }
        }
    }

    [Route( "/viewmodel/{Id}" )]
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
            return new ViewThatUsesLayoutAndModelResponse
            {
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

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

        public Rockstar() { }
        public Rockstar( int id, string firstName, string lastName, int age )
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }



    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Razor Test", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add( new RazorFormat()
                {
                    //TemplateProvider = {CompileInParallelWithNoOfThreads = 0}
                } );

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
}