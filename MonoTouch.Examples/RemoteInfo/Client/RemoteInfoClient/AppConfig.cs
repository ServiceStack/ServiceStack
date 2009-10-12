
using System;
using ServiceStack.Client;

namespace RemoteInfoClient
{
	public static class AppConfig
	{
		public static IServiceClient ServiceClient 
		{ 
			get
			{
				return new XmlServiceClient ("http://localhost:8080/Public/Xml/SyncReply"); 		//xsp host (XML)
//				return new JsonServiceClient ("http://localhost:8080/Public/Json/SyncReply"); 	//xsp host (JSON)
//				return new XmlServiceClient ("http://localhost:81"); 							//Console host (XML)
			}
		}
	}
}
