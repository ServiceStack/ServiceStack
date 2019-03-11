namespace ServiceStack
{
    /// <summary>
    /// Run "no-touch" Startup logic before the ServiceController is created and Config is initialized
    /// Only classes in AppHost or Service Assemblies are discovered and run.
    /// </summary>
    public interface IPreConfigureAppHost
    {
        void Configure(IAppHost appHost);
    }

    /// <summary>
    /// Run "no-touch" Startup logic before AppHost.Configure() is run.
    /// Only classes in AppHost or Service Assemblies are discovered and run.
    /// </summary>
    public interface IConfigureAppHost
    {
        void Configure(IAppHost appHost);
    }

    /// <summary>
    /// Run "no-touch" Startup logic after AppHost.Configure() is run.
    /// Only classes in AppHost or Service Assemblies are discovered and run.
    /// </summary>
    public interface IPostConfigureAppHost
    {
        void Configure(IAppHost appHost);
    }
}