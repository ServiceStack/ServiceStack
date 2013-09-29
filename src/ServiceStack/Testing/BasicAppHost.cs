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
                   serviceAssemblies.Length > 0 ? serviceAssemblies : new[] { Assembly.GetExecutingAssembly() })
        {
        }

        public override void Configure(Container container)
        {
            if (ConfigureFilter != null)
                ConfigureFilter(container);
        }

        public Action<Container> ConfigureFilter { get; set; }

        public Action<HostConfig> ConfigFilter { get; set; }

        public Func<Container, ServiceManager> UseServiceManager
        {
            set { ServiceManager = value(Container); }
        } 

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            if (ConfigFilter != null)
                ConfigFilter(Config);
        }
    }
}