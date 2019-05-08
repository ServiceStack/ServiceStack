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

    public interface IConfigureServices 
    {
        void Configure(IServiceCollection services);
    }
    
    public interface IConfigureApp
    {
        void Configure(IApplicationBuilder app);
    }


    /// <summary>
    /// Execute "no touch" Startup configuration classes.
    /// 
    /// Use Init() to configure Startup Type and which Assemblies to Scan to find "no touch" Startup configuration classes executed in the following order:
    /// 
    ///  Configure Services:
    ///  Priority &lt; 0:
    ///  - IConfigureServices.Configure(services), IStartup.ConfigureServices(services)
    /// 
    ///  - StartupType.ConfigureServices(services)
    /// 
    ///  Priority &gt;= 0: (no [Priority] == 0)
    ///  - IConfigureServices.Configure(services), IStartup.ConfigureServices(services)
    ///
    ///  Configure App:
    ///  Priority &lt; 0:
    ///  - IConfigureApp.Configure(app), IStartup.Configure(app)
    /// 
    ///  - StartupType.Configure(app)
    /// 
    ///  Priority &gt;= 0: (no [Priority] == 0)
    ///  - IConfigureApp.Configure(app), IStartup.Configure(app)
    /// </summary>
    public class ModularStartup : IStartup
    {
        /// <summary>
        /// No Startup Class and scan for Modular Startup classes in current Executing Assembly. 
        /// </summary>
        public static void Init() => Init(null, Assembly.GetExecutingAssembly());
        
        /// <summary>
        /// Use StartupType and scan for Modular Startup classes in StartupType Assembly.
        /// </summary>
        /// <param name="startupType"></param>
        public static void Init(Type startupType) => Init(startupType, new[] { startupType.Assembly });
        /// <summary>
        /// Use StartupType and scan for Modular Startup classes in specified Assembly.
        /// </summary>
        /// <param name="startupType"></param>
        public static void Init(Type startupType, Assembly assembly) => Init(startupType, new[] { assembly });
        /// <summary>
        /// Use StartupType and scan for Modular Startup classes in specified Assemblies.
        /// </summary>
        /// <param name="startupType"></param>
        public static void Init(Type startupType, Assembly[] assemblies)
        {
            StartupType = startupType;
            ScanAssemblies = () => assemblies;
        }
        
        internal static Type StartupType { get; set; }
        
        public static Func<Assembly[]> ScanAssemblies { get; set; }

        
        
        private readonly IConfiguration configuration;
        public ModularStartup(IConfiguration configuration) => this.configuration = configuration;
        public ModularStartup(){}

        private object startupInstance;

        public object CreateStartupInstance(Type type)
        {
            var ctorConfiguration = type.GetConstructor(new[] { typeof(IConfiguration) });
            if (ctorConfiguration == null) 
                return type.CreateInstance();
            
            var activator = ctorConfiguration.GetActivator();
            return activator.Invoke(configuration);
        }

        private List<Tuple<object,int>> priorityInstances;
        public List<Tuple<object,int>> GetPriorityInstances()
        {
            if (priorityInstances == null)
            {
                var types = ScanAssemblies().SelectMany(x => x.GetTypes()).Where(x =>
                    x != typeof(ModularStartup) && (
                        x.HasInterface(typeof(IStartup)) ||
                        x.HasInterface(typeof(IConfigureServices)) ||
                        x.HasInterface(typeof(IConfigureApp)))
                    );

                if (StartupType != null)
                {
                    startupInstance = CreateStartupInstance(StartupType);                    
                }
                
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
                    config.Configure(services);
                else if (instance is IStartup startup)
                    startup.ConfigureServices(services);
            }
            
            var startupConfigs = GetPriorityInstances();

            var preStartupConfigs = startupConfigs.PriorityBelowZero();
            preStartupConfigs.ForEach(RunConfigure);

            if (startupInstance != null)
            {
                // Execute TStartup Instance ConfigureServices(services)
                var mi = startupInstance.GetType().GetMethod(nameof(ConfigureServices));
                if (mi != null)
                {
                    mi.Invoke(startupInstance, new object[] { services });
                }
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
                    config.Configure(app);
                else if (instance is IStartup startup)
                    startup.Configure(app);
            }
            
            var startupConfigs = GetPriorityInstances();

            var preStartupConfigs = startupConfigs.PriorityBelowZero();
            preStartupConfigs.ForEach(RunConfigure);

            if (startupInstance != null)
            {
                // Execute TStartup Instance Configure(app) - can have var args
                var mi = startupInstance.GetType().GetMethods().Where(x => 
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
                            args.Add(configuration);
                        }
                        else
                        {
                            args.Add(app.ApplicationServices.GetRequiredService(pi.ParameterType));
                        }
                    }
                
                    mi.Invoke(startupInstance, args.ToArray());
                }
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

