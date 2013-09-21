using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlAsyncOneWayHandler : GenericHandler
	{
		public XmlAsyncOneWayHandler()
            : base(MimeTypes.Xml, EndpointAttributes.OneWay | EndpointAttributes.Xml, Feature.Xml)
		{
		}
	}

	public class XmlSyncReplyHandler : GenericHandler
	{
		public XmlSyncReplyHandler()
            : base(MimeTypes.Xml, EndpointAttributes.Reply | EndpointAttributes.Xml, Feature.Xml)
		{
		}
	}
}