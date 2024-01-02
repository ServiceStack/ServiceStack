using System;
using System.Reflection;
using Funq;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Testing;

public class BasicAppHost : ServiceStackHost
{
    public BasicAppHost(params Assembly[] serviceAssemblies)
        : base(typeof (BasicAppHost).GetOperationName(),
            serviceAssemblies.Length > 0 ? serviceAssemblies : new[]
            {
#if !NETCORE
                Assembly.GetExecutingAssembly()
#else
                typeof(BasicAppHost).Assembly
#endif
            })
    {
        this.TestMode = true;
        Plugins.Clear();
        InitOptions.Plugins.Clear();
    }

    public override void Configure(Container container)
    {
        ConfigureAppHost?.Invoke(this);
        ConfigureContainer?.Invoke(container);
    }

    public Action<Container> ConfigureContainer { get; set; }

    public Action<BasicAppHost> ConfigureAppHost { get; set; }

    public Action<HostConfig> ConfigFilter { get; set; }

    public Func<BasicAppHost, ServiceController> UseServiceController
    {
        set => ServiceController = value(this);
    }

    public override IServiceGateway GetServiceGateway(IRequest req) => 
        base.GetServiceGateway(req ?? new GatewayRequest());

    public override void OnConfigLoad()
    {
        base.OnConfigLoad();

        ConfigFilter?.Invoke(Config);
    }
}