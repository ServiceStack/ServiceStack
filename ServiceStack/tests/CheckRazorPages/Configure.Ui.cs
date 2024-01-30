using ServiceStack.Mvc;

[assembly: HostingStartup(typeof(MyApp.ConfigureUi))]

namespace MyApp;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost =>
        {
            RazorPage.Config = new() {
                ForbiddenPartial = "~/Pages/Shared/Forbidden.cshtml", //Optional: Render partial in same page instead
            };
            
            View.NavItems.AddRange([
                new() { Href = "/",         Label = "Home",     Exact = true },
                new() { Href = "/About",    Label = "About" },
                new() { Href = "/Privacy",  Label = "Privacy" },
                new() { Href = "/TodoMvc",  Label = "Todo Mvc" },
                new() { Href = "/SignIn",   Label = "Sign In",  Hide = When.IsAuthenticated },
                new() { Href = "/Profile",  Label = "Profile",  Show = When.IsAuthenticated },
                new() { Href = "/Contacts", Label = "Contacts", Show = When.IsAuthenticated },
                new() { Href = "/Admin",    Label = "Admin",    Show = When.HasRole("Admin") },
            ]);
        });
}
