using System;
using System.Linq;
using System.Collections.Generic;

namespace ServiceStack
{
#if NETSTANDARD2_0

    using System;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Implement to register your App's dependencies in a "no-touch" Startup configuration class 
    /// </summary>
    public interface IConfigureServices 
    {
        void Configure(IServiceCollection services);
    }
    
    /// <summary>
    /// Implement to register your App's features in a "no-touch" Startup configuration class 
    /// </summary>
    public interface IConfigureApp
    {
        void Configure(IApplicationBuilder app);
    }

    /// <summary>
    /// Implement to have configuration injected in your "no-touch" Startup configuration class as an
    /// alternative for constructor injection.
    /// </summary>
    public interface IRequireConfiguration
    {
        IConfiguration Configuration { get; set; }
    }


    /// <summary>
    /// Execute "no touch" IStartup, IConfigureServices and IConfigureApp Startup configuration classes.
    /// 
    /// The "no touch" Startup configuration classes are executed in the following order:
    /// 
    ///  Configure Services:
    ///  Priority &lt; 0:
    ///  - IConfigureServices.Configure(services), IStartup.ConfigureServices(services)
    /// 
    ///  - this.ConfigureServices(services)
    /// 
    ///  Priority &gt;= 0: (no [Priority] == 0)
    ///  - IConfigureServices.Configure(services), IStartup.ConfigureServices(services)
    ///
    ///  Configure App:
    ///  Priority &lt; 0:
    ///  - IConfigureApp.Configure(app), IStartup.Configure(app)
    /// 
    ///  - this.Configure(app)
    /// 
    ///  Priority &gt;= 0: (no [Priority] == 0)
    ///  - IConfigureApp.Configure(app), IStartup.Configure(app)
    /// </summary>
    public abstract class ModularStartup : IStartup
    {
        public List<Assembly> ScanAssemblies { get; }

        public IConfiguration Configuration { get; }
        
        public Func<IEnumerable<Type>> TypeResolver { get; }

        /// <summary>
        /// Scan Types in Assemblies for Startup configuration classes
        /// </summary>
        protected ModularStartup(IConfiguration configuration, params Assembly[] assemblies)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ScanAssemblies = new List<Assembly>(assemblies) { GetType().Assembly };
            TypeResolver = () => ScanAssemblies.Distinct().SelectMany(x => x.GetTypes());
        }

        /// <summary>
        /// Manually specify Types of Startup configuration classes 
        /// </summary>
        protected ModularStartup(IConfiguration configuration, Type[] types)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            TypeResolver = types.Distinct;
        }

        /// <summary>
        /// Use a custom Type Resolver function to return Types of Startup configuration classes
        /// </summary>
        protected ModularStartup(IConfiguration configuration, Func<IEnumerable<Type>> typesResolver)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            TypeResolver = typesResolver ?? throw new ArgumentNullException(nameof(typesResolver));
        }

        public object CreateStartupInstance(Type type)
        {
            var ctorConfiguration = type.GetConstructor(new[] { typeof(IConfiguration) });
            if (ctorConfiguration != null)
            {
                var activator = ctorConfiguration.GetActivator();
                return activator.Invoke(Configuration);
            }

            var instance = type.CreateInstance();
            if (instance is IRequireConfiguration requiresConfig)
                requiresConfig.Configuration = Configuration;
            return instance;
        }

        private List<Tuple<object,int>> priorityInstances;
        public List<Tuple<object,int>> GetPriorityInstances()
        {
            if (priorityInstances == null)
            {
                var types = TypeResolver().Where(x =>
                    !typeof(ModularStartup).IsAssignableFrom(x) // exclude self 
                    && (
                        x.HasInterface(typeof(IStartup)) ||
                        x.HasInterface(typeof(IConfigureServices)) ||
                        x.HasInterface(typeof(IConfigureApp)))
                    );

                priorityInstances = new List<Tuple<object,int>>();
                foreach (var type in types)
                {
                    var instance = CreateStartupInstance(type);
                    priorityInstances.Add(new Tuple<object, int>(instance, type.FirstAttribute<PriorityAttribute>()?.Value ?? 0));
                }
                
                priorityInstances.Sort((x,y) => x.Item2.CompareTo(y.Item2));
            }
            return priorityInstances;
        }

        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            void RunConfigure(object instance)
            {
                if (instance is IConfigureServices config)
                    config.Configure(services);
                else if (instance is IStartup startup)
                    startup.ConfigureServices(services);
            }
            
            var startupConfigs = GetPriorityInstances();

            var preStartupConfigs = startupConfigs.PriorityBelowZero();
            preStartupConfigs.ForEach(RunConfigure);

            // Execute TStartup Instance ConfigureServices(services)
            var mi = GetType().GetMethod(nameof(ConfigureServices));
            if (mi != null)
            {
                mi.Invoke(this, new object[] { services });
            }

            var postStartupConfigs = startupConfigs.PriorityZeroOrAbove();
            postStartupConfigs.ForEach(RunConfigure);

            return services.BuildServiceProvider();
        }

        public virtual void Configure(IApplicationBuilder app)
        {
            void RunConfigure(object instance)
            {
                if (instance is IConfigureApp config)
                    config.Configure(app);
                else if (instance is IStartup startup)
                    startup.Configure(app);
            }
            
            var startupConfigs = GetPriorityInstances();

            var preStartupConfigs = startupConfigs.PriorityBelowZero();
            preStartupConfigs.ForEach(RunConfigure);

            // Execute TStartup Instance Configure(app) - can have var args
            var mi = GetType().GetMethods().Where(x => 
                    x.DeclaringType != typeof(ModularStartup) && // exclude self
                    x.Name == nameof(Configure) &&
                    x.GetParameters().Length > 0 &&
                    x.GetParameters()[0].ParameterType ==
                    typeof(IApplicationBuilder))
                .OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault();            
            if (mi != null)
            {
                var args = new List<object>();
                foreach (var pi in mi.GetParameters())
                {
                    if (pi.ParameterType == typeof(IApplicationBuilder))
                    {
                        args.Add(app);
                    }
                    else if (pi.ParameterType == typeof(IConfiguration))
                    {
                        args.Add(Configuration);
                    }
                    else
                    {
                        args.Add(app.ApplicationServices.GetRequiredService(pi.ParameterType));
                    }
                }
                
                mi.Invoke(this, args.ToArray());
            }
            
            var postStartupConfigs = startupConfigs.PriorityZeroOrAbove();
            postStartupConfigs.ForEach(RunConfigure);
        }
    }
#endif

    public static class ModularExtensions
    {
        public static List<Tuple<object, int>> WithPriority(this IEnumerable<object> instances) => 
            instances.Select(o => new Tuple<object, int>(o, o.GetType().FirstAttribute<PriorityAttribute>()?.Value ?? 0)).ToList();

        public static List<object> PriorityBelowZero(this List<Tuple<object, int>> instances) =>
            instances.Where(x => x.Item2 < 0).OrderBy(x => x.Item2).Map(x => x.Item1);

        public static List<object> PriorityZeroOrAbove(this List<Tuple<object, int>> instances) =>
            instances.Where(x => x.Item2 >= 0).OrderBy(x => x.Item2).Map(x => x.Item1);
    }
}

