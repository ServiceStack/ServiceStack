using System.Configuration;

namespace ServiceStack.RazorEngine.Configuration
{
	/// <summary>
    /// Defines the <see cref="ConfigurationElement"/> that represents a template service element.
    /// </summary>
    public class TemplateServiceConfigurationElement : ConfigurationElement
    {
        #region Fields
        private const string ActivatorAttribute = "activator";
        private const string LanguageAttribute = "language";
        private const string MarkupParserAttribute = "markupParser";
        private const string NameAttribute = "name";
        private const string NamespacesAttribute = "namespaces";
        private const string StrictModeAttribute = "strictMode";
        private const string TemplateBaseAttribute = "templateBase";
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
        /// Defines the language supported by the template service.
        /// </summary>
        [ConfigurationProperty(LanguageAttribute, IsRequired = false, DefaultValue = Language.CSharp)]
        public Language Language
        {
            get { return (Language)this[LanguageAttribute]; }
            set { this[LanguageAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the markup parser.
        /// </summary>
        [ConfigurationProperty(MarkupParserAttribute)]
        public string MarkupParser
        {
            get { return (string)this[MarkupParserAttribute]; }
            set { this[MarkupParserAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the name of the template service.
        /// </summary>
        [ConfigurationProperty(NameAttribute, IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this[NameAttribute]; }
            set { this[NameAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the collection of namespaces.
        /// </summary>
        [ConfigurationProperty(NamespacesAttribute)]
        public NamespaceConfigurationElementCollection Namespaces
        {
            get { return (NamespaceConfigurationElementCollection)this[NamespacesAttribute]; }
            set { this[NamespacesAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets whether the template service should be running in strict mode.
        /// </summary>
        [ConfigurationProperty(StrictModeAttribute)]
        public bool StrictMode
        {
            get { return (bool)this[StrictModeAttribute]; }
            set { this[StrictModeAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the template base
        /// </summary>
        [ConfigurationProperty(TemplateBaseAttribute)]
        public string TemplateBase
        {
            get { return (string)this[TemplateBaseAttribute]; }
            set { this[TemplateBaseAttribute] = value; }
        }
        #endregion
    }
}