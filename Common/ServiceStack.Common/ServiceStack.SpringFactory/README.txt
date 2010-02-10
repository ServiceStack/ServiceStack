 Creates a factory instance from an object definition stored in the applications .config file.
 The syntax is compatible with the objects defintion defined in:
 http://www.springframework.net/doc/reference/html/springobjectsxsd.html

<configuration>
	 <configSections>
		<section name="objects" type="ServiceStack.Configuration.ObjectsConfigurationSectionHandler, ServiceStack.SpringFactory, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
	 </configSections>
	...
	<objects>
	 <object name="OrderServiceClient" type="ServiceStack.ServiceClient.Web.XmlServiceClient, ServiceStack.ServiceClient.Web">
		<constructor-arg value="http://servicestack.net/Endpoints/Xml/SyncReply.ashx/"/>
	 </object>
	</objects>
</configuration>

Note: it is reflection-based (read: slow) and is really just optimized to create a C# instance from an XML definition file.
I would recommend using it to create a top-level factory then caching in an IOC (Func?) as a singleton.

Removing from core ServiceStack.dll as Service Stack strives for simple, efficient solutions and an XML object definition is neither.