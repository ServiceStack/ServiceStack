using CheckIdentity.ServiceInterface;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Text;
[assembly: HostingStartup(typeof(CheckIdentity.TestStartupServices))]

namespace CheckIdentity
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base(nameof(CheckIdentity), typeof(MyServices).Assembly) {}

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig {
                AdminAuthSecret = "secretz"
            });
            
            Plugins.Add(new AuthFeature(IdentityAuth.For<IdentityUser>(options => {
                options.IncludeRegisterService = true;
                options.IncludeAssignRoleServices = true;
            })));
        }
    }
    
    public class TestStartupServices : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                $"{nameof(TestStartupServices)}.Configure(services)".Print();
            });
            // kills the app?
            // builder.Configure(app => {
            //     $"{nameof(TestStartupServices)}.Configure(app)".Print();
            // });
        }
    }

    // public class TestStartupApp : IConfigureApp
    // {
    //     public void Configure(IApplicationBuilder app)
    //     {
    //         $"{nameof(TestStartupApp)}.Configure(app)".Print();
    //     }
    // }

    public static class AppExt
    {
        public static WebApplicationBuilder AddModularStartup<THost>(this WebApplicationBuilder builder)
            where THost : AppHostBase
        {
            builder.Services.AddModularStartup<THost>(builder.Configuration);
            return builder;
        }
    }

}