#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;

namespace ServiceStack
{
#if NETCORE

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
        public static ModularStartup? Instance { get; protected set;  } 
        /// <summary>
        /// Which Startup Types not to load 
        /// </summary>
        public List<Type> IgnoreTypes { get; set; } = new();
        
        public List<Assembly>? ScanAssemblies { get; }

        public IConfiguration? Configuration { get; set; }
        
        public Func<IEnumerable<Type>> TypeResolver { get; }
        
        public List<object> LoadedConfigurations { get; set; } = new();

        protected ModularStartup()
        {
            Instance = this;
            ScanAssemblies = new List<Assembly> { GetType().Assembly };
            TypeResolver = () => ScanAssemblies.Distinct().SelectMany(x => x.GetTypes());
        }

        /// <summary>
        /// Scan Types in Assemblies for Startup configuration classes
        /// </summary>
        protected ModularStartup(IConfiguration configuration, params Assembly[] assemblies)
        {
            Instance = this;
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
                requiresConfig.Configuration = Configuration!;
            return instance;
        }
        
        /// <summary>
        /// Whether to load the Startup Type or not, allows all Startup Types not in IgnoreTypes by default
        /// </summary>
        public virtual bool LoadType(Type startupType) => !IgnoreTypes.Contains(startupType);

        private List<Tuple<object,int>>? priorityInstances;
        public List<Tuple<object,int>> GetPriorityInstances()
        {
            if (priorityInstances == null)
            {
                var types = TypeResolver().Where(x =>
                    !typeof(ModularStartup).IsAssignableFrom(x) // exclude self
                    && !x.IsAbstract
                    && (
                        x.HasInterface(typeof(IStartup)) ||
                        x.HasInterface(typeof(IConfigureServices)) ||
                        x.HasInterface(typeof(IConfigureApp)))
                    && LoadType(x)
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

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            void RunConfigure(object instance)
            {
                if (instance is IConfigureServices config)
                {
                    config.Configure(services);
                    LoadedConfigurations.Add(instance);
                }
                else if (instance is IStartup startup)
                {
                    startup.ConfigureServices(services);
                    LoadedConfigurations.Add(instance);
                }
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

        public void Configure(IApplicationBuilder app)
        {
            void RunConfigure(object instance)
            {
                if (instance is IConfigureApp config)
                {
                    config.Configure(app);
                    LoadedConfigurations.Add(instance);
                }
                else if (instance is IStartup startup)
                {
                    startup.Configure(app);
                    LoadedConfigurations.Add(instance);
                }
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
                        args.Add(Configuration!);
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

        public static Type Create<TStartup>()
        {
            if (!typeof(ModularStartup).IsAssignableFrom(typeof(TStartup)))
                throw new NotSupportedException($"{typeof(TStartup).Name} does not inherit ModularStartup");
            
            ModularStartupActivator.StartupType = typeof(TStartup);
            return typeof(ModularStartupActivator);
        }
    }

    /// <summary>
    /// Used to load ModularStartup classes in .NET 6+ top-level WebApplicationBuilder builder 
    /// </summary>
    public class TopLevelAppModularStartup : ModularStartup
    {
        public new static ModularStartup? Instance { get; private set; } //needs to be base concrete type 
        public Type AppHostType { get; set; }
        public AppHostBase StartupInstance { get; set; }

        public static ModularStartup Create<THost>(THost instance,
            IConfiguration configuration, Func<IEnumerable<Type>> typesResolver)
            where THost : AppHostBase
        {
            instance.Configuration = configuration;
            return Instance = new TopLevelAppModularStartup(typeof(THost), instance, configuration, typesResolver);
        }

        protected TopLevelAppModularStartup(Type hostType, AppHostBase hostInstance, 
            IConfiguration configuration, Func<IEnumerable<Type>> typesResolver)
            : base(configuration, typesResolver)
        {
            AppHostType = hostType;
            StartupInstance = hostInstance;
        }

        public new void ConfigureServices(IServiceCollection services)
        {
            //implementation needs to exist to stop base method recursion
            //nameof(TopLevelStatementsStartup).Print();
        }

        public new void Configure(IApplicationBuilder app)
        {
            //nameof(TopLevelAppModularStartup).Print();
        }
    }
    
#endif

    public static class ModularExtensions
    {
        public static List<Tuple<object, int>> WithPriority(this IEnumerable<object> instances) => 
            instances.Select(o => new Tuple<object, int>(o, o.GetType().FirstAttribute<PriorityAttribute>()?.Value ?? 0)).ToList();

        public static List<object> PriorityOrdered(this List<Tuple<object, int>> instances) =>
            instances.OrderBy(x => x.Item2).Map(x => x.Item1);

        public static List<object> PriorityBelowZero(this List<Tuple<object, int>> instances) =>
            instances.Where(x => x.Item2 < 0).OrderBy(x => x.Item2).Map(x => x.Item1);

        public static List<object> PriorityZeroOrAbove(this List<Tuple<object, int>> instances) =>
            instances.Where(x => x.Item2 >= 0).OrderBy(x => x.Item2).Map(x => x.Item1);

#if NETCORE

        /// <summary>
        /// Used to load ModularStartup classes in .NET 6+ top-level WebApplicationBuilder builder
        /// </summary>
        public static IServiceCollection AddModularStartup<THost>(this IServiceCollection services, IConfiguration configuration)
            where THost : AppHostBase
        {
            if (TopLevelAppModularStartup.Instance != null)
                throw new NotSupportedException($"{nameof(AddModularStartup)} has already been called");
            
            var ci = typeof(THost).GetConstructor(Type.EmptyTypes);
            if (ci == null)
                throw new NotSupportedException($"{typeof(THost).Name} requires a parameterless constructor");
            
            var host = (THost)ci.Invoke(TypeConstants.EmptyObjectArray);

            var dlls = new List<Assembly>(host.ServiceAssemblies) { typeof(THost).Assembly };
            var types = dlls.Distinct().SelectMany(x => x.GetTypes())
                .Where(x => x != typeof(TopLevelAppModularStartup)).ToList();
            var startup = TopLevelAppModularStartup.Create(host, configuration, () => types);
            startup.ConfigureServices(services);
            return services;
        }
        
        /// <summary>
        /// .NET Core 3.0 disables IStartup and multiple Configure* entry points on Startup class requiring the use of a
        /// clean ModularStartupActivator adapter class for implementing https://docs.servicestack.net/modular-startup
        /// </summary>
        public static IWebHostBuilder UseModularStartup<TStartup>(this IWebHostBuilder hostBuilder)
            where TStartup : class
        {
            return hostBuilder
                // UserSecrets not loaded when using surrogate startup class, load explicitly from TStartup.Assembly
                .ConfigureAppConfiguration((ctx, config) => 
                    config.AddUserSecrets(typeof(TStartup).GetTypeInfo().Assembly, optional:true))                    
                .UseStartup(ModularStartup.Create<TStartup>());
        }
        
        /// <summary>
        /// ASP.NET Core MVC has a built-in limitation/heuristic requiring the Startup class to be defined in the Host assembly,
        /// which can be done by registering a custom ModularStartupActivator sub class.
        /// </summary>
        public static IWebHostBuilder UseModularStartup<TStartup, TStartupActivator>(this IWebHostBuilder hostBuilder)
            where TStartup : class
        {
            if (!typeof(ModularStartup).IsAssignableFrom(typeof(TStartup)))
                throw new NotSupportedException($"{typeof(TStartup).Name} does not inherit ModularStartup");
            
            if (!typeof(ModularStartupActivator).IsAssignableFrom(typeof(TStartupActivator)))
                throw new NotSupportedException($"{typeof(TStartupActivator).Name} does not inherit ModularStartupActivator");
            
            ModularStartupActivator.StartupType = typeof(TStartup);
            return hostBuilder.UseStartup(typeof(TStartupActivator));
        }
#endif
    }
    
#if NETCORE
    /// <summary>
    /// .NET Core 3.0 disables IStartup and multiple Configure* entry points on Startup class requiring the use of a
    /// clean ModularStartupActivator adapter class for implementing https://docs.servicestack.net/modular-startup
    ///
    /// ASP.NET Core MVC has a built-in limitation/heuristic requiring the Startup class to be defined in the Host assembly,
    /// which can be done by registering a custom ModularStartupActivator sub class.
    /// </summary>
    public class ModularStartupActivator
    {
        public static Type? StartupType { get; set; }
        protected IConfiguration Configuration { get; }

        protected readonly ModularStartup Instance;
        public ModularStartupActivator(IConfiguration configuration)
        {
            Configuration = configuration;
            var ci = StartupType!.GetConstructor(new[] { typeof(IConfiguration) });
            if (ci != null)
            {
                Instance = (ModularStartup) ci.Invoke(new[]{ Configuration });
            }
            else
            {
                ci = StartupType.GetConstructor(Type.EmptyTypes);
                if (ci != null)
                {
                    Instance = (ModularStartup) ci.Invoke(TypeConstants.EmptyObjectArray);
                    Instance.Configuration = configuration;
                }
                else
                    throw new NotSupportedException($"{StartupType.Name} does not have a {StartupType.Name}(IConfiguration) constructor");
            }
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            Instance.ConfigureServices(services);
        }

        public virtual void Configure(IApplicationBuilder app)
        {
            Instance.Configure(app);
        }
    }
#endif
    
}

