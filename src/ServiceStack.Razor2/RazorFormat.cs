using ServiceStack.Logging;
using ServiceStack.Razor2.Managers;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor2
{
    public class RazorFormat : IPlugin, IRazor2Plugin
    {
        private static readonly ILog Log = LogManager.GetLogger( typeof( RazorFormat ) );

        protected IAppHost AppHost { get; private set; }

        public RazorFormat()
        {
            this.BuildConfig = new BuildConfig();
            this.ViewConfig = new ViewConfig();
            this.ResolveConfig = new PageResolverConfig();
        }

        //configs
        public virtual PageResolverConfig ResolveConfig { get; protected set; }
        public virtual ViewConfig ViewConfig { get; protected set; }
        public virtual BuildConfig BuildConfig { get; protected set; }

        //managers
        protected ViewManager ViewManager;
        protected BuildManager BuildManager;
        protected PageResolver PageResolver;

        public void Register( IAppHost appHost )
        {
            this.AppHost = appHost;
            this.ViewManager = new ViewManager( appHost, this.ViewConfig );
            this.BuildManager = new BuildManager( appHost, this.BuildConfig );
            this.PageResolver = new PageResolver( appHost, this.ResolveConfig, this.ViewManager, this.BuildManager );
        }
    }

}