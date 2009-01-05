using ServiceStack.LogicFacade;
using ServiceStack.ServiceModel;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlRequestDto : IXmlRequest
	{
		public XmlRequestDto(string xml, IServiceModelFinder serviceModelInfo)
		{
			this.Xml = xml;
			this.ServiceModelFinder = serviceModelInfo;
		}

		public string Xml { get; set; }

		public IServiceModelFinder ServiceModelFinder { get; set; }
	}
}