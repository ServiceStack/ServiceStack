using System.Configuration;

namespace ServiceStack.Razor.Configuration
{
	/// <summary>
    /// Represents a collection of <see cref="TemplateServiceConfigurationElement"/> items.
    /// </summary>
    [ConfigurationCollection(typeof(TemplateServiceConfigurationElement))]
    public class TemplateServiceConfigurationElementConfiguration : ConfigurationElementCollection
    {
        #region Fields
        private const string DefaultAttribute = "default";
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the default template service.
        /// </summary>
        public string Default { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new TemplateServiceConfigurationElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TemplateServiceConfigurationElement)element).Name;
        }

        /// <summary>
        /// Gets a value indicating whether an unknown attribute is encountered during deserialization.
        /// </summary>
        /// <param name="name">The name of the unrecognized attribute.</param>
        /// <param name="value">The value of the unrecognized attribute.</param>
        /// <returns>
        /// true when an unknown attribute is encountered while deserializing; otherwise, false.
        /// </returns>
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            if (name.Equals("default"))
            {
                Default = value;
                return true;
            }

            return base.OnDeserializeUnrecognizedAttribute(name, value);
        }
        #endregion
    }
}