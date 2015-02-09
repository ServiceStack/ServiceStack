using System.Collections.Generic;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class MetadataPagesConfig
    {
        private ServiceMetadata metadata;
        private HashSet<string> ignoredFormats;
        private readonly Dictionary<string, MetadataConfig> metadataConfigMap;
        public List<MetadataConfig> AvailableFormatConfigs { get; private set; }

        public MetadataPagesConfig(
            ServiceMetadata metadata,
            ServiceEndpointsMetadataConfig metadataConfig,
            HashSet<string> ignoredFormats,
            List<string> contentTypeFormats)
        {
            this.ignoredFormats = ignoredFormats;
            this.metadata = metadata;

            metadataConfigMap = new Dictionary<string, MetadataConfig> {
                {"xml", metadataConfig.Xml},
                {"json", metadataConfig.Json},
                {"jsv", metadataConfig.Jsv},
                {"soap11", metadataConfig.Soap11},
                {"soap12", metadataConfig.Soap12},
            };

            AvailableFormatConfigs = new List<MetadataConfig>();

            var config = GetMetadataConfig("xml");
            if (config != null) AvailableFormatConfigs.Add(config);
            config = GetMetadataConfig("json");
            if (config != null) AvailableFormatConfigs.Add(config);
            config = GetMetadataConfig("jsv");
            if (config != null) AvailableFormatConfigs.Add(config);

            foreach (var format in contentTypeFormats)
            {
                metadataConfigMap[format] = metadataConfig.Custom.Create(format);

                config = GetMetadataConfig(format);
                if (config != null) AvailableFormatConfigs.Add(config);
            }

            config = GetMetadataConfig("soap11");
            if (config != null) AvailableFormatConfigs.Add(config);
            config = GetMetadataConfig("soap12");
            if (config != null) AvailableFormatConfigs.Add(config);
        }

        public MetadataConfig GetMetadataConfig(string format)
        {
            if (ignoredFormats.Contains(format)) return null;

            MetadataConfig conf;
            metadataConfigMap.TryGetValue(format, out conf);
            return conf;
        }

        public bool IsVisible(IRequest httpRequest, Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.IsVisible(httpRequest, format, operation);
        }

        public bool CanAccess(IRequest httpRequest, Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.CanAccess(httpRequest, format, operation);
        }

        public bool CanAccess(Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.CanAccess(format, operation);
        }

        public bool AlwaysHideInMetadata(string operationName)
        {
            Operation operation;
            metadata.OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null || operation.RestrictTo == null) return false;

            return operation.RestrictTo.VisibilityTo == RequestAttributes.None;
        }
    }
}