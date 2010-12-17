using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlAsyncOneWayHandler : GenericHandler
	{
		public XmlAsyncOneWayHandler()
			: base(ContentType.Xml, EndpointAttributes.AsyncOneWay | EndpointAttributes.Xml)
		{
		}
	}

	public class XmlSyncReplyHandler : GenericHandler
	{
		public XmlSyncReplyHandler()
			: base(ContentType.Xml, EndpointAttributes.SyncReply | EndpointAttributes.Xml)
		{
		}
	}
}