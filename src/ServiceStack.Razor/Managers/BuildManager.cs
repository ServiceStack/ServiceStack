using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.Managers
{
    /// <summary>
    /// This build manager is responsible for building & compiling razor pages.
    /// </summary>
    public class BuildManager
    {
        IRazorConfig Config { get; set; }

        public BuildManager(IAppHost appHost, IRazorConfig config)
        {
            this.Config = config;
        }

        public void EnsureCompiled(RazorPage page, IHttpResponse response)
        {
            if (page == null) return;
            if (page.IsValid) return;

            var type = page.PageHost.Compile();

            page.PageType = type;

            page.IsValid = true;
        }
    }
}