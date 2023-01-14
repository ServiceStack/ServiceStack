using ServiceStack;
using ServiceStack.Mvc;
using System.Net;

[assembly: HostingStartup(typeof(MyApp.ConfigureUi))]

namespace MyApp;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost => {
            //Can't detect page at custom route, use app.UseStatusCodePagesWithReExecute("/NotFound"); instead
            //appHost.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");
            appHost.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = new RazorHandler("/Forbidden");
            
            View.NavItems.AddRange(new List<NavItem> {
                new() { Href = "/",         Label = "Home",    Exact = true },
                new() { Href = "/About",    Label = "About" },
                new() { Href = "/Privacy",  Label = "Privacy" },
                new() { Href = "/TodoMvc",  Label = "Todo Mvc" },
                new() { Href = "/SignIn",   Label = "Sign In", Hide = "auth" },
                new() { Href = "/Profile",  Label = "Profile", Show = "auth" },
                new() { Href = "/Contacts", Label = "Contacts", Show = "auth" },
                new() { Href = "/Admin",    Label = "Admin",   Show = "role:Admin" },
            });
        });
}
