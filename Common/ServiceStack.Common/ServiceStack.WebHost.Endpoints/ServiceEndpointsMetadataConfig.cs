namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceEndpointsMetadataConfig
	{
		public string DefaultMetadataUri { get; set; }
		public SoapMetadataConfig Soap11 { get; set; }
		public SoapMetadataConfig Soap12 { get; set; }
		public MetadataConfig Xml { get; set; }
		public MetadataConfig Json { get; set; }
		public MetadataConfig Jsv { get; set; }

		public MetadataConfig GetEndpointConfig(EndpointType endpointType)
		{
			switch (endpointType)
			{
				case EndpointType.Soap11:
					return this.Soap11;
				case EndpointType.Soap12:
					return this.Soap12;
				case EndpointType.Xml:
					return this.Xml;
				case EndpointType.Json:
					return this.Json;
				case EndpointType.Jsv:
					return this.Jsv;
			}
			return null;
		}
	}
}