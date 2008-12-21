using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using ServiceStack.Common.Services.Support.Config;

namespace ServiceStack.Common.Services.Config
{
	/// <summary>
	/// Creates a factory instance from an object definition stored in the applications .config file.
	/// <![CDATA[
	///   <configuration>
	///     <configSections>
	///        <section name="objects" type="ServiceStack.Common.Services.Config.ObjectsConfigurationSectionHandler, ServiceStack.Common.Services, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
	///     </configSections>
	/// ...
	///   <objects>
	///     <object name="CustomerServiceClient" type="ServiceStack.Common.Services.Client.WebServiceClient, ServiceStack.Common.Services">
	///        <property name="UseBasicHttp" value="true"/>
	///     </object>
	///   </objects>
	///   </configuration>
	/// ]]>
	/// 
	/// The syntax is compatible with the objects defintion defined in:
	/// http://www.springframework.net/doc/reference/html/springobjectsxsd.html
	/// </summary>
	public class ObjectsConfigurationSectionHandler : IConfigurationSectionHandler
	{
		private const string OBJECT_ELEMENT = "object";

		public object Create(object parent, object configContext, System.Xml.XmlNode section)
		{
			var objectTypes = new Dictionary<string, ObjectConfigurationType>();
			var nodeObjects = section.SelectNodes(OBJECT_ELEMENT);
			foreach (XmlNode nodeObject in nodeObjects)
			{
				var objectType = new ObjectConfigurationType((XmlElement)nodeObject);
				objectTypes[objectType.Name] = objectType;
			}
			return objectTypes;
		}
	}
}
