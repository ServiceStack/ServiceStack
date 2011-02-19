namespace ServiceStack.WebHost.Endpoints
{
	public class SoapMetadataConfig : MetadataConfig
	{
		public SoapMetadataConfig(string syncReplyUri, string asyncOneWayUri, string defaultMetadataUri, string wsdlMetadataUri)
			: base(syncReplyUri, asyncOneWayUri, defaultMetadataUri)
		{
			WsdlMetadataUri = wsdlMetadataUri;
		}

		public string WsdlMetadataUri { get; set; }
	}
}