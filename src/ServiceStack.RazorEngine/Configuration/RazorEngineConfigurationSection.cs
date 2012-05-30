using System.Configuration;

namespace ServiceStack.RazorEngine.Configuration
{
    /// <summary>
    /// Defines the main configuration section for the RazorEngine.
    /// </summary>
    public class RazorEngineConfigurationSection : ConfigurationSection
    {
        #region Fields
        private const string ActivatorAttribute = "activator";
        private const string FactoryAttribute = "factory";
        private const string NamespacesElement = "namespaces";
        private const string SectionPath = "razorEngine";
        private const string TemplateServicesElement = "templateServices";
        #endregion

        #region Properties
        /// <summary>
        /// Gets the activator used for this template service.
        /// </summary>
        [ConfigurationProperty(ActivatorAttribute, IsRequired = false)]
        public string Activator
        {
            get { return (string)this[ActivatorAttribute]; }
            set { this[ActivatorAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the factory used to create compiler service instances.
        /// </summary>
        [ConfigurationProperty(FactoryAttribute)]
        public string Factory
        {
            get { return (string)this[FactoryAttribute]; }
            set { this[FactoryAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the collection of namespaces.
        /// </summary>
        [ConfigurationProperty(NamespacesElement)]
        public NamespaceConfigurationElementCollection Namespaces
        {
            get { return (NamespaceConfigurationElementCollection)this[NamespacesElement]; }
            set { this[NamespacesElement] = value; }
        }

        /// <summary>
        /// Gets or sets the collection of template service configurations.
        /// </summary>
        [ConfigurationProperty(TemplateServicesElement)]
        public TemplateServiceConfigurationElementConfiguration TemplateServices
        {
            get { return (TemplateServiceConfigurationElementConfiguration)this[TemplateServicesElement]; }
            set { this[TemplateServicesElement] = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets an instance of <see cref="RazorEngineConfigurationSection"/> that represents the current configuration.
        /// </summary>
        /// <returns>An instance of <see cref="RazorEngineConfigurationSection"/>, or null if no configuration is specified.</returns>
        public static RazorEngineConfigurationSection GetConfiguration()
        {
            return ConfigurationManager.GetSection(SectionPath) as RazorEngineConfigurationSection;
        }
        #endregion
    }
}