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
                   serviceAssemblies.Length > 0 ? serviceAssemblies : new[]
                   {
#if !NETSTANDARD1_6
                       Assembly.GetExecutingAssembly()
#else
                       typeof(BasicAppHost).GetTypeInfo().Assembly
#endif
                   })
        {
            this.ExcludeAutoRegisteringServiceTypes = new HashSet<Type>();
            this.TestMode = true;
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
            set { ServiceController = value(this); }
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();

            ConfigFilter?.Invoke(Config);
        }
    }
}