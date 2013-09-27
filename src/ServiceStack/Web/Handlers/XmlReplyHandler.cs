using ServiceStack.Text;

namespace ServiceStack.Web.Handlers
{
	public class XmlOneWayHandler : GenericHandler
	{
		public XmlOneWayHandler()
            : base(MimeTypes.Xml, EndpointAttributes.OneWay | EndpointAttributes.Xml, Feature.Xml)
		{
		}
	}

	public class XmlReplyHandler : GenericHandler
	{
		public XmlReplyHandler()
            : base(MimeTypes.Xml, EndpointAttributes.Reply | EndpointAttributes.Xml, Feature.Xml)
		{
		}
	}
}