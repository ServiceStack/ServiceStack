using ServiceStack.ServiceModel;

namespace ServiceStack.ServiceInterface
{
	public class XmlRequestDto
	{
		public XmlRequestDto(string xml, ServiceModelInfo serviceModelInfo)
		{
			this.Xml = xml;
			this.ServiceModelInfo = serviceModelInfo;
		}

		public string Xml { get; set; }
		public ServiceModelInfo ServiceModelInfo { get; set; }
	}
}