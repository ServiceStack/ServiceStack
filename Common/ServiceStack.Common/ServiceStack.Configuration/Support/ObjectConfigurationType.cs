using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Xml;

namespace ServiceStack.Configuration.Support
{
	/// <summary>
	/// Contains the object model that can hold the following Xml Defintion
	/// <![CDATA[
	///     <object name="CustomerServiceClient" type="ServiceStack.Common.Services.Client.WebServiceClient, ServiceStack.Common.Services">
	///        <property name="UseBasicHttp" value="true"/>
	///     </object>
	/// ]]>
	/// </summary>
	public class ObjectConfigurationType
	{
		private const string NAME_ATTR = "name";
		private const string TYPE_ATTR = "type";
		private const string INDEX_ATTR = "index";
		private const string PROPERTY_ELEMENT = "property";
		private const string CONSTRUCTOR_ARG_ELEMENT = "constructor-arg";

		private const char TYPE_SEPERATOR = ',';
		private const int TYPE_INDEX = 0;
		private const int ASSEMBLY_INDEX = 1;
        
		private readonly string name;
		private readonly string type;

		private readonly List<PropertyConfigurationType> constructorArgs;
		private readonly List<PropertyConfigurationType> properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectConfigurationType"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		public ObjectConfigurationType(XmlElement node)
		{
			properties = new List<PropertyConfigurationType>();
			constructorArgs = new List<PropertyConfigurationType>();

			name = node.GetAttribute(NAME_ATTR);
			type = node.GetAttribute(TYPE_ATTR);
            
			XmlNodeList nodeProperties = node.SelectNodes(PROPERTY_ELEMENT);
			foreach (XmlNode nodeProperty in nodeProperties)
			{
				properties.Add(new PropertyConfigurationType((XmlElement)nodeProperty));
			}

			var nodeConstructorArgs = node.SelectNodes(CONSTRUCTOR_ARG_ELEMENT);
			const int FIRST_INDEX = 0;
			var index = FIRST_INDEX;
			var argMap = new SortedDictionary<int, PropertyConfigurationType>();
			foreach (XmlNode nodeConstructorArg in nodeConstructorArgs)
			{
				var el = (XmlElement)nodeConstructorArg;
				if (!string.IsNullOrEmpty(el.GetAttribute(INDEX_ATTR)))
				{
					index = Convert.ToInt32(el.GetAttribute(INDEX_ATTR));
				}
				argMap[index++] = new PropertyConfigurationType(el);
			}
			if (nodeConstructorArgs.Count > 0 && !argMap.ContainsKey(FIRST_INDEX))
			{
				throw new ConfigurationErrorsException("constructor-arg index must start at: " + FIRST_INDEX);
			}
			constructorArgs.AddRange(argMap.Values);
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get { return name; }
		}

		/// <summary>
		/// Gets the type.
		/// </summary>
		/// <value>The type.</value>
		public string Type
		{
			get { return type; }
		}

		/// <summary>
		/// Gets the constructor args.
		/// </summary>
		/// <value>The constructor args.</value>
		public List<PropertyConfigurationType> ConstructorArgs
		{
			get { return constructorArgs; }
		}

		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		public List<PropertyConfigurationType> Properties
		{
			get { return properties; }
		}

	}
}