using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using ServiceStack.SpringFactory.Support;

namespace ServiceStack.SpringFactory
{
	/// <summary>
	/// Creates a factory instance from an object definition stored in the applications .config file.
	/// <![CDATA[
	///   <configuration>
	///     <configSections>
	///        <section name="objects" type="ServiceStack.Configuration.ObjectsConfigurationSectionHandler, ServiceStack, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
	///     </configSections>
	/// ...
	///   <objects>
	///     <object name="OrderServiceClient" type="ServiceStack.ServiceClient.Web.XmlServiceClient, ServiceStack.ServiceClient.Web">
	///        <constructor-arg value="http://servicestack.net/Endpoints/Xml/SyncReply.ashx/"/>
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
		private const string ObjectElement = "object";

		public object Create(object parent, object configContext, System.Xml.XmlNode section)
		{
			var objectTypes = new Dictionary<string, ObjectConfigurationType>();
			XmlNodeList nodeObjects = section.SelectNodes(ObjectElement);
			foreach (XmlNode nodeObject in nodeObjects)
			{
				var objectType = new ObjectConfigurationType((XmlElement)nodeObject);
				objectTypes[objectType.Name] = objectType;
			}
			return objectTypes;
		}
	}
}