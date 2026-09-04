using System;
using System.Collections.Generic;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Metadata;

public class MetadataPagesConfig
{
    private readonly ServiceMetadata metadata;
    private readonly HashSet<string> ignoredFormats;
    private readonly Dictionary<string, MetadataConfig> metadataConfigMap;
    public List<MetadataConfig> AvailableFormatConfigs { get; private set; }

    public MetadataPagesConfig(
        ServiceMetadata metadata,
        ServiceEndpointsMetadataConfig metadataConfig,
        HashSet<string> ignoredFormats,
        List<string> contentTypeFormats)
    {
        this.ignoredFormats = ignoredFormats ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        this.metadata = metadata;

        metadataConfigMap = new Dictionary<string, MetadataConfig>(StringComparer.OrdinalIgnoreCase);
        AvailableFormatConfigs = new List<MetadataConfig>();

        if (contentTypeFormats != null && metadataConfig != null)
        {
            foreach (var format in contentTypeFormats)
            {
                if (format == null) continue;
                metadataConfigMap[format] = metadataConfig.GetEndpointConfig(format);

                var config = GetMetadataConfig(format);
                if (config != null) 
                    AvailableFormatConfigs.Add(config);
            }
        }
    }

    public MetadataConfig GetMetadataConfig(string format)
    {
        if (format == null) return null;
        if (ignoredFormats != null && ignoredFormats.Contains(format)) 
            return null;

        metadataConfigMap.TryGetValue(format, out var conf);
        return conf;
    }

    public bool IsVisible(IRequest httpRequest, Format format, string operation)
    {
        if (ignoredFormats != null && ignoredFormats.Contains(format.FromFormat())) return false;
        return metadata?.IsVisible(httpRequest, format, operation) ?? false;
    }

    public bool CanAccess(IRequest httpRequest, Format format, string operation)
    {
        if (ignoredFormats != null && ignoredFormats.Contains(format.FromFormat())) return false;
        return metadata?.CanAccess(httpRequest, format, operation) ?? false;
    }

    public bool CanAccess(Format format, string operation)
    {
        if (ignoredFormats != null && ignoredFormats.Contains(format.FromFormat())) return false;
        return metadata?.CanAccess(format, operation) ?? false;
    }

    public bool AlwaysHideInMetadata(string operationName)
    {
        if (operationName == null || metadata?.OperationNamesMap == null) return false;
        metadata.OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out var operation);
        return operation?.RestrictTo?.VisibilityTo == RequestAttributes.None;
    }
}