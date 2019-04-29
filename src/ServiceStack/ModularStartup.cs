#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack
{
    /// <summary>
    ///  Configure Startup Type and which Assemblies to Scan to find "no touch" Startup configuration classes executed in the following order:
    /// 
    ///  Configure Services:
    ///  - IPreConfigureServices.Configure(services)
    ///  - IStartup.ConfigureServices(services)
    ///  - StartupType.ConfigureServices(services)
    ///  - IPostConfigureServices.Configure(services)
    ///
    ///  Configure App:
    ///  - IPreConfigureApp.Configure(app)
    ///  - IStartup.Configure(app)
    ///  - StartupType.Configure(app)
    ///  - IPostConfigureApp.Configure(app)
    /// </summary>
    public static class ModularStartupConfig
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
    }
    

    public interface IPreConfigureServices 
    {
        void Configure(IServiceCollection services);
    }
    public interface IPostConfigureServices
    {
        void Configure(IServiceCollection services);
    }
    
    public interface IPreConfigureApp
    {
        void Configure(IApplicationBuilder app);
    }
    public interface IPostConfigureApp
    {
        void Configure(IApplicationBuilder app);
    }


    /// <summary>
    /// Execute "no touch" Startup configuration classes.
    /// </summary>
    public class ModularStartup : IStartup
    {
        private readonly IConfiguration configuration;
        public ModularStartup(IConfiguration configuration) => this.configuration = configuration;
        public ModularStartup(){}

        private object startupInstance;
        private List<object> instances;

        public object CreateStartupInstance(Type type)
        {
            var ctorConfiguration = type.GetConstructor(new[] { typeof(IConfiguration) });
            if (ctorConfiguration == null) 
                return type.CreateInstance();
            
            var activator = ctorConfiguration.GetActivator();
            return activator.Invoke(configuration);
        }

        public List<object> GetInstances()
        {
            if (instances == null)
            {
                var types = ModularStartupConfig.ScanAssemblies().SelectMany(x => x.GetTypes()).Where(x =>
                    x != typeof(ModularStartup) && (
                        x.HasInterface(typeof(IStartup)) ||
                        x.HasInterface(typeof(IPreConfigureServices)) ||
                        x.HasInterface(typeof(IPostConfigureServices)) ||
                        x.HasInterface(typeof(IPreConfigureApp)) ||
                        x.HasInterface(typeof(IPostConfigureApp))
                    ));

                if (ModularStartupConfig.StartupType != null)
                {
                    startupInstance = CreateStartupInstance(ModularStartupConfig.StartupType);                    
                }
                
                instances = new List<object>();

                foreach (var type in types)
                {
                    var instance = CreateStartupInstance(type);
                    instances.Add(instance);
                }
                
            }
            return instances;
        }

        public List<object> OrderedList(IEnumerable<object> instances)
        {
            var list = instances.ToList();
            if (!list.Any(x => x.GetType().HasAttribute<PriorityAttribute>()))
                return list;
                
            var priorityMap = new Dictionary<object, int>();
            foreach (var o in list)
            {
                priorityMap[o] = o.GetType().FirstAttribute<PriorityAttribute>()?.Value ?? 0;
            }
            
            list.Sort((x,y) => priorityMap[x].CompareTo(priorityMap[y]));
            return list;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var preServices = OrderedList(GetInstances().Where(x => x is IPreConfigureServices));
            foreach (IPreConfigureServices startup in preServices)
            {
                startup.Configure(services);
            }
            
            var startupServices = OrderedList(GetInstances().Where(x => x is IStartup));
            foreach (IStartup startup in startupServices)
            {
                startup.ConfigureServices(services);
            }

            if (startupInstance != null)
            {
                // Execute TStartup Instance ConfigureServices(services)
                var mi = startupInstance.GetType().GetMethod(nameof(ConfigureServices));
                if (mi != null)
                {
                    mi.Invoke(startupInstance, new object[] { services });
                }
            }

            var postServices = OrderedList(GetInstances().Where(x => x is IPostConfigureServices));
            foreach (IPostConfigureServices startup in postServices)
            {
                startup.Configure(services);
            }

            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            var preServices = OrderedList(GetInstances().Where(x => x is IPreConfigureApp));
            foreach (IPreConfigureApp startup in preServices)
            {
                startup.Configure(app);
            }
            
            var startupServices = OrderedList(GetInstances().Where(x => x is IStartup));
            foreach (IStartup startup in startupServices)
            {
                startup.Configure(app);
            }

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
            
            var postServices = OrderedList(GetInstances().Where(x => x is IPostConfigureApp));
            foreach (IPostConfigureApp startup in postServices)
            {
                startup.Configure(app);
            }
        }
    }
}

#endif
