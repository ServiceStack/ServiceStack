using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor2.Managers
{
    /// <summary>
    /// This build manager is responsible for building & compiling razor pages.
    /// </summary>
    public class BuildManager
    {
        public BuildManager( IAppHost appHost, BuildConfig buildConfig )
        {
        }


        public void EnsureCompiled( RazorPage page, IHttpResponse response )
        {
            if ( page == null ) return;
            if ( page.IsValid ) return;

            var type = page.PageHost.Compile();

            page.PageType = type;

            page.IsValid = true;
        }
    }


    public class BuildConfig
    {
        public int? CompileInParallelWithNoOfThreads { get; set; }
    }
}