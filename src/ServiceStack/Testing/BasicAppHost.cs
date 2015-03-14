using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Host;

namespace ServiceStack.Testing
{
    public class BasicAppHost : ServiceStackHost
    {
        public BasicAppHost(params Assembly[] serviceAssemblies)
            : base(typeof (BasicAppHost).GetOperationName(),
                   serviceAssemblies.Length > 0 ? serviceAssemblies : new[] {Assembly.GetExecutingAssembly()})
        {
            this.ExcludeAutoRegisteringServiceTypes = new HashSet<Type>();
            this.TestMode = true;
        }

        public override void Configure(Container container)
        {
            if (ConfigureAppHost != null)
                ConfigureAppHost(this);

            if (ConfigureContainer != null)
                ConfigureContainer(container);
        }

        public Action<Container> ConfigureContainer { get; set; }

        public Action<BasicAppHost> ConfigureAppHost { get; set; }

        public Action<HostConfig> ConfigFilter { get; set; }

        public Func<BasicAppHost, ServiceController> UseServiceController
        {
            set { ServiceController = value(this); }
        }

        public override void OnBeforeInit()
        {
            base.OnBeforeInit();
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            if (ConfigFilter != null)
                ConfigFilter(Config);
        }
    }
}