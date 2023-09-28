#nullable enable

namespace ServiceStack.Aws;

public class CloudflareConfig
{
    public string? AccountId { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Bucket { get; set; }
    public string? OpenAiBaseUrl { get; set; }
    public string? ToServiceUrl() => AccountId == null
        ? null
        : $"https://{AccountId}.r2.cloudflarestorage.com";
}
