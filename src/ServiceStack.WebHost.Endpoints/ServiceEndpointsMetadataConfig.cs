using ServiceStack.Common.Web;

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
		public MetadataConfig Custom { get; set; }

		public MetadataConfig GetEndpointConfig(string contentType)
		{
			switch (contentType)
			{
				case ContentType.Soap11:
					return this.Soap11;
				case ContentType.Soap12:
					return this.Soap12;
				case ContentType.Xml:
					return this.Xml;
				case ContentType.Json:
					return this.Json;
				case ContentType.Jsv:
					return this.Jsv;
			}

			var format = ContentType.GetContentFormat(contentType);
			return new MetadataConfig
				(
					string.Format(Custom.SyncReplyUri, format),
					string.Format(Custom.AsyncOneWayUri, format),
					string.Format(Custom.DefaultMetadataUri, format)
				);
		}
	}
}