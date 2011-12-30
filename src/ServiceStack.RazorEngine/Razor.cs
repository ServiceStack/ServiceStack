using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ServiceStack.RazorEngine.Compilation;
using ServiceStack.RazorEngine.Configuration;
using ServiceStack.RazorEngine.Templating;

namespace ServiceStack.RazorEngine
{
	/// <summary>
    /// Provides quick access to template services.
    /// </summary>
    public static class Razor
    {
        #region Constructor
        /// <summary>
        /// Initialises the <see cref="Razor"/> type.
        /// </summary>
        static Razor()
        {
            Services = new ConcurrentDictionary<string, TemplateService>();
            Configure();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the compiler service factory.
        /// </summary>
        internal static ICompilerServiceFactory CompilerServiceFactory { get; private set; }

        /// <summary>
        /// Gets the default template service.
        /// </summary>
        public static TemplateService DefaultTemplateService { get; private set; }

        /// <summary>
        /// Gets the collection of configured services.
        /// </summary>
        public static IDictionary<string, TemplateService> Services { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a resolver used to resolve named template content.
        /// </summary>
        /// <param name="resolver">The resolver to add.</param>
        public static void AddResolver(ITemplateResolver resolver)
        {
            DefaultTemplateService.AddResolver(resolver);
        }

        /// <summary>
        /// Adds a resolver used to resolve named template content.
        /// </summary>
        /// <param name="resolverDelegate">The resolver delegate to add.</param>
        public static void AddResolver(Func<string, string> resolverDelegate)
        {
            DefaultTemplateService.AddResolver(resolverDelegate);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void Compile(string template, string name)
        {
            DefaultTemplateService.CompileWithAnonymous(template, name);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="modelType">The type of model used in the template.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void Compile(string template, Type modelType, string name)
        {
            DefaultTemplateService.Compile(template, modelType, name);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// This method should be used when an anonymous model is used in the template.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void CompileWithAnonymous(string template, string name)
        {
            DefaultTemplateService.CompileWithAnonymous(template, name);
        }

        /// <summary>
        /// Configures the templating engine.
        /// </summary>
        private static void Configure()
        {
            var config = RazorEngineConfigurationSection.GetConfiguration();
            if (config != null)
            {
                if (!string.IsNullOrWhiteSpace(config.Factory))
                    SetCompilerServiceFactory(config.Factory);
                else
                    CompilerServiceFactory = new DefaultCompilerServiceFactory();

                if (config.TemplateServices.Count > 0)
                {
                    string @default = string.IsNullOrWhiteSpace(config.TemplateServices.Default)
                                          ? null
                                          : config.TemplateServices.Default;

                    foreach (TemplateServiceConfigurationElement serviceConfig in config.TemplateServices)
                    {
                        string name = serviceConfig.Name;
                        var service = ConfigurationServices.CreateTemplateService(serviceConfig);;
                        ConfigurationServices.AddNamespaces(service, config.Namespaces);

                        if (name == @default)
                            DefaultTemplateService = service;

                        Services.Add(name, service);
                    }
                }

                if (DefaultTemplateService == null)
                {
                    DefaultTemplateService = new TemplateService(CompilerServiceFactory.CreateCompilerService());
                    ConfigurationServices.AddNamespaces(DefaultTemplateService, config.Namespaces);
                }

                if (!string.IsNullOrWhiteSpace(config.Activator))
                    DefaultTemplateService.SetActivator(ConfigurationServices.CreateInstance<IActivator>(config.Activator));
            }
            else
            {
                ConfigureDefault();
            }
        }

        /// <summary>
        /// Applies a default configuration.
        /// </summary>
        private static void ConfigureDefault()
        {
            CompilerServiceFactory = new DefaultCompilerServiceFactory();
            var service = CompilerServiceFactory.CreateCompilerService();

            DefaultTemplateService = new TemplateService(service);
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <param name="template">The template to parse.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public static string Parse(string template, string name = null)
        {
            return DefaultTemplateService.Parse(template, name);
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="template">The template to parse.</param>
        /// <param name="model">The model.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public static string Parse<T>(string template, T model, string name = null)
        {
            return DefaultTemplateService.Parse<T>(template, model, name);
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public static string Run(string name)
        {
            return DefaultTemplateService.Run(name);
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public static string Run<T>(T model, string name)
        {
            return DefaultTemplateService.Run<T>(model, name);
        }

        /// <summary>
        /// Sets the activator used to create types.
        /// </summary>
        /// <param name="activator">The activator to use.</param>
        public static void SetActivator(IActivator activator)
        {
            DefaultTemplateService.SetActivator(activator);
        }

        /// <summary>
        /// Sets the activator delegate used to create types.
        /// </summary>
        /// <param name="activator">The activator delegate to use.</param>
        public static void SetActivator(Func<Type, ITemplate> activator)
        {
            DefaultTemplateService.SetActivator(activator);
        }

        /// <summary>
        /// Sets the compiler factory.
        /// </summary>
        /// <param name="typeName">The factory type.</param>
        private static void SetCompilerServiceFactory(string typeName)
        {
            var factory = ConfigurationServices.CreateInstance<ICompilerServiceFactory>(typeName);
            CompilerServiceFactory = factory;
        }

        /// <summary>
        /// Sets the template base type.
        /// </summary>
        /// <param name="type">The template base type.</param>
        public static void SetTemplateBase(Type type)
        {
            DefaultTemplateService.SetTemplateBase(type);
        }
        #endregion
    }
}