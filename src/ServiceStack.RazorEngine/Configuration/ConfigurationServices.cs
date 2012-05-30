using ServiceStack.RazorEngine.Templating;
using System;

namespace ServiceStack.RazorEngine.Configuration
{
    /// <summary>
    /// Provides configuration service operations.
    /// </summary>
    public static class ConfigurationServices
    {
        #region Methods
        /// <summary>
        /// Adds any configured namespaces to the target template service.
        /// </summary>
        /// <param name="service">The template service.</param>
        /// <param name="config">The namespace configuration element.</param>
        public static void AddNamespaces(TemplateService service, NamespaceConfigurationElementCollection config)
        {
            foreach (NamespaceConfigurationElement @namespace in config)
            {
                service.Namespaces.Add(@namespace.Namespace);
            }
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="typeName">The type name.</param>
        /// <returns>An instance of the specified type.</returns>
        public static T CreateInstance<T>(string typeName) where T : class
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name is required.");

            Type type = Type.GetType(typeName);
            if (type == null)
                throw new ArgumentException("The type '" + typeName + "' could not be loaded.");

            T instance = Activator.CreateInstance(type) as T;
            if (instance == null)
                throw new ArgumentException("The type '" + typeName + "' is not an instance of '" + typeof(T).FullName + "'.");

            return instance;
        }

        /// <summary>
        /// Creates an instance of <see cref="TemplateService"/> from the specified configuration.
        /// </summary>
        /// <param name="config">The template service configuration.</param>
        /// <returns>An instance of <see cref="TemplateService"/>.</returns>
        public static TemplateService CreateTemplateService(TemplateServiceConfigurationElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            var service = TemplateServiceFactory.CreateTemplateService(config);
            AddNamespaces(service, config.Namespaces);

            if (!string.IsNullOrWhiteSpace(config.Activator))
                service.SetActivator(CreateInstance<IActivator>(config.Activator));

            return service;
        }
        #endregion
    }
}