using System.Configuration;

namespace ServiceStack.RazorEngine.Configuration
{
	/// <summary>
    /// Defines the <see cref="ConfigurationElement"/> that represents a namespaces element.
    /// </summary>
    public class NamespaceConfigurationElement : ConfigurationElement
    {
        #region Fields
        private const string NamespaceAttribute = "namespace";
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        [ConfigurationProperty(NamespaceAttribute, IsRequired = true, IsKey = true)]
        public string Namespace
        {
            get { return (string)this[NamespaceAttribute]; }
            set { this[NamespaceAttribute] = value; }
        }
        #endregion
    }
}