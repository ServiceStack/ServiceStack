using System;
using System.Reflection;
using Funq;
using ServiceStack.Host;

namespace ServiceStack.Testing
{
    public class BasicAppHost : ServiceStackHost
    {
        public BasicAppHost(params Assembly[] serviceAssemblies)
            : base(typeof(BasicAppHost).Name,
                   serviceAssemblies.Length > 0 ? serviceAssemblies : new[] { Assembly.GetExecutingAssembly() }) {}

        public override void Configure(Container container)
        {
            if (ConfigureContainer != null)
                ConfigureContainer(container);

            if (ConfigureAppHost != null)
                ConfigureAppHost(this);
        }

        public Action<Container> ConfigureContainer { get; set; }

        public Action<BasicAppHost> ConfigureAppHost { get; set; }

        public Action<HostConfig> ConfigFilter { get; set; }

        public Func<BasicAppHost, ServiceController> UseServiceController
        {
            set { ServiceController = value(this); }
        } 

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            if (ConfigFilter != null)
                ConfigFilter(Config);
        }
    }
}