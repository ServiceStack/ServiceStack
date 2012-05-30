using System.Configuration;

namespace ServiceStack.RazorEngine.Configuration
{
    /// <summary>
    /// Represents a collection of <see cref="NamespaceConfigurationElement"/> items.
    /// </summary>
    [ConfigurationCollection(typeof(NamespaceConfigurationElement))]
    public class NamespaceConfigurationElementCollection : ConfigurationElementCollection
    {
        #region Methods
        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new NamespaceConfigurationElement();
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
            return ((NamespaceConfigurationElement)element).Namespace;
        }
        #endregion
    }
}
