using Microsoft.AspNetCore.Hosting;

namespace ServiceStack
{
    public static class HostExtensions
    {
        public static IWebHostBuilder UseModularStartup<TStartup>(this IWebHostBuilder hostBuilder)
            where TStartup : class
        {
            return hostBuilder.UseStartup(ModularStartup.Create<TStartup>());
        }
    }
}