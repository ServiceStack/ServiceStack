using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.WebHost.Endpoints
{

	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{
		public static readonly Dictionary<string, Func<IHttpHandler>> HandlerMap 
			= new Dictionary<string, Func<IHttpHandler>> {
		        {"/Metadata", () => new IndexMetadataHandler()},                                                          		
		        
				{"/Soap11/Wsdl", () => new Soap11WsdlMetadataHandler()},                                                          		
		        {"/Soap11/Metadata", () => new Soap11MetadataHandler()},                                                          		
		        
				{"/Soap12/Wsdl", () => new Soap12WsdlMetadataHandler()},                                                          		
		        {"/Soap12/Metadata", () => new Soap12MetadataHandler()},                                                          		
		        
				{"/Xml/Metadata", () => new XmlMetadataHandler()},                                                          		
		        {"/Xml/AsyncOneWay", () => new XmlAsyncOneWayHandler()},                                                          		
		        {"/Xml/SyncReply", () => new XmlSyncReplyHandler()},                                                          		

				{"/Json/Metadata", () => new JsonMetadataHandler()},                                                          		
		        {"/Json/AsyncOneWay", () => new JsonAsyncOneWayHandler()},                                                          		
		        {"/Json/SyncReply", () => new JsonSyncReplyHandler()},                                                          		

				{"/Jsv/Metadata", () => new JsvMetadataHandler()},                                                          		
		        {"/Jsv/AsyncOneWay", () => new JsvAsyncOneWayHandler()},                                                          		
		        {"/Jsv/SyncReply", () => new JsvSyncReplyHandler()},                                                          		
		};

		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			throw new NotImplementedException();
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
			throw new NotImplementedException();
		}
	}
}