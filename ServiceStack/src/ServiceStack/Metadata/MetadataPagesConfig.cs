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
        this.ignoredFormats = ignoredFormats;
        this.metadata = metadata;

        metadataConfigMap = new Dictionary<string, MetadataConfig>();
        AvailableFormatConfigs = new List<MetadataConfig>();

        foreach (var format in contentTypeFormats)
        {
            metadataConfigMap[format] = metadataConfig.GetEndpointConfig(format);

            var config = GetMetadataConfig(format);
            if (config != null) 
                AvailableFormatConfigs.Add(config);
        }
    }

    public MetadataConfig GetMetadataConfig(string format)
    {
        if (ignoredFormats.Contains(format)) 
            return null;

        metadataConfigMap.TryGetValue(format, out var conf);
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
        metadata.OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out var operation);
        return operation?.RestrictTo?.VisibilityTo == RequestAttributes.None;
    }
}