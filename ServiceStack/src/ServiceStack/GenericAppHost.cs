using System;
using System.Reflection;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack;

public class GenericAppHost : ServiceStackHost
{
    public GenericAppHost(params Assembly[] serviceAssemblies)
        : base(typeof (GenericAppHost).GetOperationName(),
            serviceAssemblies.Length > 0 ? serviceAssemblies : new[]
            {
#if NETFRAMEWORK
                Assembly.GetExecutingAssembly()
#else
                typeof(GenericAppHost).Assembly
#endif
            })
    {
        Plugins.Clear();
    }

    public override void Configure(Container container)
    {
        ConfigureAppHost?.Invoke(this);
        ConfigureContainer?.Invoke(container);
    }

    public Action<Container> ConfigureContainer { get; set; }

    public Action<GenericAppHost> ConfigureAppHost { get; set; }

    public Action<HostConfig> ConfigFilter { get; set; }

    public override IServiceGateway GetServiceGateway(IRequest req) => 
        base.GetServiceGateway(req ?? new BasicRequest());

    public override void OnConfigLoad()
    {
        base.OnConfigLoad();

        ConfigFilter?.Invoke(Config);
    }
    
#if !NETFRAMEWORK
    public Microsoft.Extensions.Hosting.IHost Host { get; set; }
#endif    
}

public static class GenericAppHostExtensions
{
#if !NETFRAMEWORK
    public static Microsoft.Extensions.Hosting.IHost UseServiceStack(this Microsoft.Extensions.Hosting.IHost host, GenericAppHost appHost)
    {
        appHost.Host = host;
        appHost.Container.Adapter = new NetCore.NetCoreContainerAdapter(host.Services);
       
        var logFactory = host.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        if (logFactory != null)
        {
            NetCore.NetCoreLogFactory.FallbackLoggerFactory = logFactory;
            if (Logging.LogManager.LogFactory.IsNullOrNullLogFactory())
                Logging.LogManager.LogFactory = new NetCore.NetCoreLogFactory(logFactory);
        }
        
        appHost.Init();
        return host;
    }
#endif    
}

