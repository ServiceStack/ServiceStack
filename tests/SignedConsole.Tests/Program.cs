using System;
using System.Diagnostics;
using Funq;
using ServiceStack;
using ServiceStack.Text;

namespace SignedConsole.Tests
{
    public class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base("SelfHost", typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = true
            });
        }
    }

    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new AppHost().Init().Start("http://*:8088/");
            "ServiceStack Self Host with Razor listening at http://127.0.0.1:8088".Print();
            Process.Start("http://127.0.0.1:8088/");

            Console.ReadLine();
        }
    }
}
