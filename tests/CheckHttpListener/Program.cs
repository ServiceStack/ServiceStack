using System;
using System.Diagnostics;
using System.IO;
using Funq;
using ServiceStack;

namespace CheckHttpListener
{
    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Check HttpListener Tests", typeof(AppHost).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new DtoGenFeature());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Licensing.RegisterLicenseFromFileIfExists(@"c:\src\appsettings.license.txt");

            new AppHost()
                .Init()
                .Start("http://localhost:2020/");

            Process.Start("http://localhost:2020/dtogen/csharp");
            Console.ReadLine();
        }
    }
}
