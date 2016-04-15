using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Check.ServiceModel;
using Funq;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

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

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
            Plugins.Add(new AdminFeature());

            var msgs = new List<string>();
            int count = 0;

            var client = new ServerEventsClient("http://chat.servicestack.net", "home")
            {
                OnMessage = m =>
                {
                    try
                    {
                        lock (msgs)
                        {
                            msgs.Add(m.Dump());
                        }
                    }
                    catch (Exception ex)
                    {
                        msgs.Add("OnMessage Exception: " + ex.Message);
                    }
                }
            }.Start();

            timer = new Timer(_ =>
            {
                lock (msgs)
                {
                    for (; count < msgs.Count; count++)
                    {
                        Console.WriteLine(msgs[count]);
                    }
                }
                timer.Change(1000, Timeout.Infinite);
            }, null, 1000, Timeout.Infinite);

        }

        Timer timer;
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
            var appHost = new AppSelfHost()
                .Init()
                .Start("http://127.0.0.1:2222/");

            Process.Start("http://127.0.0.1:2222/");
            Console.ReadLine();
        }
    }
}