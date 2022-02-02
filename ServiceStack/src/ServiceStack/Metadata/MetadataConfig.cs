namespace ServiceStack.Metadata
{
    public class MetadataConfig
    {
        public MetadataConfig(string format, string name, string syncReplyUri, string asyncOneWayUri, string defaultMetadataUri)
        {
            Format = format;
            Name = name;
            SyncReplyUri = syncReplyUri;
            AsyncOneWayUri = asyncOneWayUri;
            DefaultMetadataUri = defaultMetadataUri;
        }

        public string Format { get; set; }
        public string Name { get; set; }
        public string SyncReplyUri { get; set; }
        public string AsyncOneWayUri { get; set; }
        public string DefaultMetadataUri { get; set; }

        public MetadataConfig Create(string format, string name = null)
        {
            return new MetadataConfig
                (
                    format,
                    name ?? format.ToUpper(),
                    string.Format(SyncReplyUri, format),
                    string.Format(AsyncOneWayUri, format),
                    string.Format(DefaultMetadataUri, format)
                );
        }
    }
}