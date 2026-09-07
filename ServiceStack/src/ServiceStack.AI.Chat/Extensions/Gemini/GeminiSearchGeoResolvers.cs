using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>Common HTTP, validation and caching behavior for the built-in IP geo resolvers.</summary>
public abstract class GeminiSearchGeoResolverBase(IHttpClientFactory httpClientFactory) : IGeminiSearchGeoResolver
{
    readonly ConcurrentDictionary<string, CacheEntry> cache = new();

    /// <summary>How long successful lookups are reused. IP geography rarely changes.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Maximum time analytics waits for the external resolver.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public async ValueTask<GeminiSearchGeo?> ResolveAsync(string ipAddress, CancellationToken token = default)
    {
        var normalized = GeminiSearchGeo.NormalizeIpAddress(ipAddress);
        if (normalized == null || !IsPublicAddress(IPAddress.Parse(normalized))) return null;
        if (cache.TryGetValue(normalized, out var found) && found.ExpiresAt > DateTime.UtcNow)
            return found.Geo;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(RequestTimeout);
        using var http = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetRequestUrl(normalized));
        request.Headers.UserAgent.ParseAdd("ServiceStack.AI.Chat/1.0");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigAwait();
        response.EnsureSuccessStatusCode();
        var json = ChatJson.TryParseObject(await response.Content.ReadAsStringAsync(timeout.Token).ConfigAwait())
            ?? throw new InvalidDataException("IP geo provider returned invalid JSON");
        var geo = ParseResponse(json);
        if (geo != null)
            cache[normalized] = new CacheEntry(geo, DateTime.UtcNow.Add(CacheDuration));
        return geo;
    }

    protected abstract string GetRequestUrl(string ipAddress);
    protected abstract GeminiSearchGeo? ParseResponse(JsonObject json);

    protected static long? ParseAsn(string? value)
    {
        var match = Regex.Match(value ?? "", @"(?i)\bAS\s*(\d+)|^\s*(\d+)\s*$");
        var number = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return long.TryParse(number, out var parsed) ? parsed : null;
    }

    static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.None)
            || address.Equals(IPAddress.IPv6None)) return false;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return !(bytes[0] is 0 or 10 or 127
                || bytes[0] == 169 && bytes[1] == 254
                || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
                || bytes[0] == 192 && bytes[1] == 168
                || bytes[0] >= 224);
        }
        return !(address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal
            || bytes[0] is 0xfc or 0xfd);
    }

    sealed record CacheEntry(GeminiSearchGeo Geo, DateTime ExpiresAt);
}

/// <summary>Resolves public IP addresses with https://freeipapi.com.</summary>
public class FreeIpApiGeminiSearchGeoResolver(IHttpClientFactory httpClientFactory)
    : GeminiSearchGeoResolverBase(httpClientFactory)
{
    protected override string GetRequestUrl(string ipAddress) =>
        $"https://free.freeipapi.com/api/json/{Uri.EscapeDataString(ipAddress)}";

    protected override GeminiSearchGeo ParseResponse(JsonObject json) => new()
    {
        Asn = ParseAsn(json.GetString("asn")),
        Organization = json.GetString("asnOrganization"),
        ContinentCode = json.GetString("continentCode"),
        CountryCode = json.GetString("countryCode"),
        CountryName = json.GetString("countryName"),
        RegionCode = json.GetString("regionCode"),
        RegionName = json.GetString("regionName"),
        City = json.GetString("cityName"),
        PostalCode = json.GetString("zipCode"),
        // Free IP API returns every time zone in the country, not necessarily the visitor's.
        // Store it only when the response is unambiguous rather than recording a wrong zone.
        TimeZone = json.GetArray("timeZones") is { Count: 1 } zones ? zones[0]?.GetValue<string>() : null,
        Latitude = json.GetDouble("latitude"),
        Longitude = json.GetDouble("longitude"),
    };
}

/// <summary>Resolves public IP addresses with the free HTTP endpoint at ip-api.com.</summary>
public class IpApiGeminiSearchGeoResolver(IHttpClientFactory httpClientFactory)
    : GeminiSearchGeoResolverBase(httpClientFactory)
{
    protected override string GetRequestUrl(string ipAddress) =>
        $"http://ip-api.com/json/{Uri.EscapeDataString(ipAddress)}";

    protected override GeminiSearchGeo? ParseResponse(JsonObject json)
    {
        if (json.GetString("status") != "success") return null;
        return new GeminiSearchGeo
        {
            Asn = ParseAsn(json.GetString("as")),
            Organization = json.GetString("org") ?? json.GetString("isp"),
            CountryCode = json.GetString("countryCode"),
            CountryName = json.GetString("country"),
            RegionCode = json.GetString("region"),
            RegionName = json.GetString("regionName"),
            City = json.GetString("city"),
            PostalCode = json.GetString("zip"),
            TimeZone = json.GetString("timezone"),
            Latitude = json.GetDouble("lat"),
            Longitude = json.GetDouble("lon"),
        };
    }
}
