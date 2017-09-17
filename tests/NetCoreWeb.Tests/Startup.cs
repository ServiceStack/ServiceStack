using System;
using System.IO;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace NetCoreWeb.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseServiceStack(new AppHost());

            app.Run(async context =>
            {
                await context.Response.WriteAsync(
@"<!doctype html>
<html lang=en>
<head>
<meta charset=utf-8>
<title>ImageTest</title>
</head>
<body>
<img src=""/img"">
</body>
</html>");
            });
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("StreamTest", typeof(AppHost).Assembly)
        {
            AppSettings = new AppSettings();
        }

        public override void Configure(Container container)
        {
        }
    }

    public class ImageService : Service
    {
        private readonly CloudBlobContainer _container;

        public ImageService()
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AZURE_BLOB_UBIXAR"));
            var storageClient = storageAccount.CreateCloudBlobClient();
            _container = storageClient.GetContainerReference("test");
        }

        public async Task<HttpResult> Get(ImageRequest request)
        {
            var blobReference = _container.GetBlobReference("rockstars/dead/cobain/splash.jpg");
            var stream = await blobReference.OpenReadAsync();
            return new HttpResult(stream, "image/jpeg");
        }
    }

    [Route("/img")]
    public class ImageRequest
    {
    }
}