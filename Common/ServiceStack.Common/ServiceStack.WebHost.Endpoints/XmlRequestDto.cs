using ServiceStack.LogicFacade;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlRequestDto : IXmlRequest
	{
		public XmlRequestDto(string xml)
		{
			this.Xml = xml;
		}

		public string Xml { get; set; }
	}
}