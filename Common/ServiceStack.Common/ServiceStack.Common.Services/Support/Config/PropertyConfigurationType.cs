using System.Xml;

namespace ServiceStack.Common.Services.Support.Config
{
    /// <summary>
    /// Contains the object model that can hold the following Xml Defintion
    /// <![CDATA[
    ///     <property name="UseBasicHttp" value="true"/>
    /// ]]>
    /// </summary>
    public class PropertyConfigurationType
    {
        private const string NAME_ATTR = "name";
        private const string VALUE_ATTR = "value";
        private const string REF_ATTR = "ref";
        private const string TYPE_ATTR = "type";

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyConfigurationType"/> class.
        /// </summary>
        /// <param name="el">The el.</param>
        public  PropertyConfigurationType(XmlElement el)
        {
            Name = el.GetAttribute(NAME_ATTR);
            Value = el.GetAttribute(VALUE_ATTR);
            Ref = el.GetAttribute(REF_ATTR);
            Type = el.GetAttribute(TYPE_ATTR);
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ref.
        /// </summary>
        /// <value>The ref.</value>
        public string Ref { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }
    }
}