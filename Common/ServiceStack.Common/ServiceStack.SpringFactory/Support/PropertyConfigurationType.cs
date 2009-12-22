using System.Xml;

namespace ServiceStack.SpringFactory.Support
{
	/// <summary>
	/// Contains the object model that can hold the following Xml Defintion
	/// <![CDATA[
	///     <property name="UseBasicHttp" value="true"/>
	/// ]]>
	/// </summary>
	public class PropertyConfigurationType
	{
		private const string NameAttr = "name";
		private const string ValueAttr = "value";
		private const string RefAttr = "ref";
		private const string TypeAttr = "type";

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyConfigurationType"/> class.
		/// </summary>
		/// <param name="el">The el.</param>
		public  PropertyConfigurationType(XmlElement el)
		{
			Name = el.GetAttribute(NameAttr);
			Value = el.GetAttribute(ValueAttr);
			Ref = el.GetAttribute(RefAttr);
			Type = el.GetAttribute(TypeAttr);
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