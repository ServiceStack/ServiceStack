using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
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