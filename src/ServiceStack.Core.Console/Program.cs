using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ServiceStack.Core.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cwd = Directory.GetCurrentDirectory();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(cwd)
                .UseUrls("http://localhost:55000")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
