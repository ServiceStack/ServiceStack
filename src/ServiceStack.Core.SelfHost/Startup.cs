//#define USE_AWS
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Funq;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Api.Swagger;
using ServiceStack.Metadata;
using ServiceStack.Web;
using ServiceStack.IO;
using ServiceStack.Aws.S3;
using Amazon;
using Amazon.S3;

namespace ServiceStack.Core.SelfHost
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(async (context, next) => {
                Console.WriteLine(context.Request.Path.Value);
                await next();
            });

            app.UseServiceStack(new AppHost());

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Unhandled Request!");
            });
        }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    [FallbackRoute("/{Path*}")]
    public class Error404
    {
        public string Path { get; set; }
    }

    [Route("/test")]
    public class Test : IReturn<string> { }

    public class MyServices : Service
    {
        public object Any(Hello request) => 
            new HelloResponse { Result = $"Hello, {request.Name ?? "World"}!" };

        //Uncomment to process Unhandled requests
        //public object Any(Error404 request) => request;

        public UploadStreamResponse Any(UploadStream request)
        {
            return new UploadStreamResponse();
        }

        public object Any(Test request)
        {
            var r = new JsonMetadataHandler();

            var response = r.CreateResponse(typeof(Stream));

            return response;
        }

        public object Any(TestRequest request) => new TestResponse();
    }

    [Api("Test request")]
    [Route("/test/{Input}", "GET")]
    [Route("/test")]
    public class TestRequest : IReturn<TestResponse>
    {
        [ApiMember(Name = "Parameter name", Description = "Parameter Description",
            ParameterType = "body", DataType = "string", IsRequired = true)]
        public string Input { get; set; }
    }
    public class TestResponse
    {
        public string Output { get; set; }
    }

    [Route("/uploadStream", "POST", Summary = "Upload stream")]
    public class UploadStream : IReturn<UploadStreamResponse>, IRequiresRequestStream
    {
        public Stream RequestStream { get; set; }
    }

    public class UploadStreamResponse { }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base(".NET Core Test", typeof(MyServices).GetAssembly()) { }

        public override void Configure(Container container)
        {
#if USE_AWS
            var s3Client = new AmazonS3Client(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"), 
                Environment.GetEnvironmentVariable("S3_SECRET_KEY"), 
                RegionEndpoint.USEast1);

            VirtualFiles = new S3VirtualFiles(s3Client, "s3-postgresql");
#endif
            SetConfig(new HostConfig {
                DebugMode = true,
                AdminAuthSecret = Environment.GetEnvironmentVariable("AUTH_SECRET"),
            });

            Plugins.Add(new TemplatePagesFeature { 
                EnableDebugTemplateToAll = true,
            });
            Plugins.Add(new SwaggerFeature());
        }
        public override List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var fileSources = base.GetVirtualFileSources();
            fileSources.Add(VirtualFiles);
            return fileSources;
        }

    }

}
