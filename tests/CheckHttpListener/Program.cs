using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Check.ServiceModel;
using Funq;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace CheckHttpListener
{
    public class AppSelfHost : AppSelfHostBase
    {
        public static Rockstar[] SeedRockstars = new[] {
            new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 },
            new Rockstar { Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27 },
            new Rockstar { Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27 },
            new Rockstar { Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42 },
            new Rockstar { Id = 5, FirstName = "David", LastName = "Grohl", Age = 44 },
            new Rockstar { Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48 },
            new Rockstar { Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50 },
        };

        public AppSelfHost()
            : base("DocuRec Services", typeof(TestService).Assembly)
        { }

        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(SeedRockstars);
            }
            
            Plugins.Add(new TemplatePagesFeature());
            
            Plugins.Add(new OpenApiFeature());

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
            Plugins.Add(new AdminFeature());

            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new[] { new BasicAuthProvider(AppSettings) })
            {
                //ServiceRoutes = new Dictionary<Type, string[]> {
                //  { typeof(AuthenticateService), new[] { "/api/auth", "/api/auth/{provider}" } },
                //}
            });

            Plugins.Add(new RequestLogsFeature());

            SetConfig(new HostConfig
            {
                HandlerFactoryPath = "api",
                CompressFilesWithExtensions = { "html", "js" },
                DebugMode = true
            });
        }

        public override string ResolvePathInfo(IRequest request, string originalPathInfo) =>
            base.ResolvePathInfo(request, originalPathInfo.Replace("/testsite", "/TestSite"));

//        public override RouteAttribute[] GetRouteAttributes(Type requestType)
//        {
//            var routes = base.GetRouteAttributes(requestType);
//            routes.Each(x => x.Path = "/api" + x.Path);
//            return routes;
//        }
    }

    [Route("/query/rockstars")]
    public class QueryRockstars : QueryDb<Rockstar> { }

    //public class Hello { }

    public class TestService : Service
    {
        //public object Any(Hello request)
        //{
        //    return request;
        //}
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseUrl = "http://localhost:8000/";
            var appHost = new AppSelfHost()
                .Init()
                .Start(baseUrl);

            Console.WriteLine(baseUrl);
            Process.Start(baseUrl);
            Console.ReadLine();
        }
    }
}