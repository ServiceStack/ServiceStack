using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ServiceStack.CefGlue.Win64.AspNetCore
{
    class Program
    {
        
#if DEBUG
        private static bool Debug = true;
#else
        private static bool Debug = false;
#endif

        static int Main(string[] args)
        {
            var startUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000/";

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(startUrl)
                .Build();

            host.StartAsync();
            
            var config = new CefConfig(Debug)
            {
                Args = args,
                StartUrl = startUrl,
                HideConsoleWindow = false,
                Width = 1200,
                Height = 1024,
                // Kiosk = true,
                // FullScreen = true,
            };
            
            return CefPlatformWindows.Start(config);
        }
    }
}
