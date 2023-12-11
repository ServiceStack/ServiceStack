#nullable enable

using System;
using Amazon;
using ServiceStack.AI;

namespace ServiceStack.Aws;

public class AwsConfig
{
    public string? AccountId { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Bucket { get; set; }
    public string? Region { get; set; }
    public string? VocabularyName { get; set; }
    
    public AwsSpeechToTextConfig ToSpeechToTextConfig(Action<AwsSpeechToTextConfig>? configure = null)
    {
        var to = new AwsSpeechToTextConfig {
            Bucket = Bucket ?? throw new ArgumentNullException(Bucket),
            VocabularyName = VocabularyName,
        };
        configure?.Invoke(to);
        return to;
    }

    public RegionEndpoint ToRegionEndpoint() => Region != null
        ? RegionEndpoint.GetBySystemName(Region)
        : RegionEndpoint.USEast1;
}
