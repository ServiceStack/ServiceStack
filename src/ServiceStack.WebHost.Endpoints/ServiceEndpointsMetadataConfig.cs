using ServiceStack.Common.Web;

namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceEndpointsMetadataConfig
	{
		/// <summary>
		/// Changes the links for the servicestack/metadata page
		/// </summary>
		/// <param name="servicestackHandlerPrefix"></param>
		/// <returns></returns>
		public static ServiceEndpointsMetadataConfig CreateFor(string servicestackHandlerPrefix)
		{
			var prefix = servicestackHandlerPrefix;
			return new ServiceEndpointsMetadataConfig
			{
				DefaultMetadataUri = prefix + "/metadata",
				Soap11 = new SoapMetadataConfig(prefix + "/soap11/syncreply.svc", prefix + "/soap11/asynconeway.svc", prefix + "/soap11/metadata", "soap11"),
				Soap12 = new SoapMetadataConfig(prefix + "/soap12/syncreply.svc", prefix + "/soap12/asynconeway.svc", prefix + "/soap12/metadata", "soap12"),
				Xml = new MetadataConfig(prefix + "/xml/syncreply", prefix + "/xml/asynconeway", prefix + "/xml/metadata"),
				Json = new MetadataConfig(prefix + "/json/syncreply", prefix + "/json/asynconeway", prefix + "/json/metadata"),
				Jsv = new MetadataConfig(prefix + "/jsv/syncreply", prefix + "/jsv/asynconeway", prefix + "/jsv/metadata"),
				Custom = new MetadataConfig(prefix + "/{0}/syncreply", prefix + "/{0}/asynconeway", prefix + "/{0}/metadata")
			};
		}

		public static ServiceEndpointsMetadataConfig GetDefault()
		{
			return CreateFor(SupportedHandlerMappings.ServiceStackAshxForIis6);
		}

		public static ServiceEndpointsMetadataConfig GetForIis6ServiceStackAshx()
		{
			return CreateFor(SupportedHandlerMappings.ServiceStackAshxForIis6);
		}

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