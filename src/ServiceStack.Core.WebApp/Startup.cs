using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Logging;
using Funq;
using ServiceStack.Host;
using ServiceStack.Text;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

namespace ServiceStack.Core.WebApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost());

            app.Run(async context =>
            {
                context.Request.EnableRewind();
                await context.Response.WriteAsync("Hello World!!!");
            });
        }
    }


    [Route("/hello/{Name}")]
    public class Hello
    {
        public string Name { get; set; }
    }

    public class HelloService : Service
    {
        public object Any(Hello request)
        {
            return request;
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base(".NET Core Test", typeof(HelloService).GetAssembly()) { }

        public override void Configure(Container container)
        {
        }
    }

}
