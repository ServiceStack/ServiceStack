namespace ServiceStack.WebHost.Endpoints
{
	public class MetadataConfig
	{
		public MetadataConfig(string syncReplyUri, string asyncOneWayUri, string defaultMetadataUri)
		{
			SyncReplyUri = syncReplyUri;
			AsyncOneWayUri = asyncOneWayUri;
			DefaultMetadataUri = defaultMetadataUri;
		}

		public string SyncReplyUri { get; set; }
		public string AsyncOneWayUri { get; set; }
		public string DefaultMetadataUri { get; set; }
	}
}