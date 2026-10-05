using System.Text.Json.Serialization;

namespace ServiceStack.AI;

/// <summary>Static project export settings, configured in code or from user/default/share_static/config.json.</summary>
public class StaticPublishConfig
{
    /// <summary>Enable folder publishing as the default project destination.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Export root; omitted defaults to p under the host's web content directory (typically wwwroot/p).
    /// Explicit relative paths resolve against the host's working directory at installation.</summary>
    public string? Directory { get; set; }

    /// <summary>URL mount path used when BaseUrl is null or empty.</summary>
    public string BasePath { get; set; } = "/p/";

    /// <summary>Public HTTP(S) URL including the mount path. Null or empty uses the Chat UI's current domain.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? BaseUrl { get; set; } = StaticProjectPublisher.DefaultBaseUrl;
}
