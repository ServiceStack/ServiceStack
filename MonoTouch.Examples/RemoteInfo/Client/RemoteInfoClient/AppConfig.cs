
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
//				return new XmlServiceClient ("http://localhost:8080/Public/Xml/SyncReply"); //xsp host
				return new XmlServiceClient ("http://localhost:81"); //command line host
			}
		}
	}
}
