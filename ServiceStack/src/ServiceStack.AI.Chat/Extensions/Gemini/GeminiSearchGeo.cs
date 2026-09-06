using System.Net;

namespace ServiceStack.AI;

/// <summary>
/// Resolves an analytics visitor's request IP into geographic metadata. Register an implementation
/// in the host IOC, or assign <see cref="GeminiExtension.SearchGeoResolver"/> directly.
/// Resolver failures do not prevent the page view from being recorded.
/// </summary>
public interface IGeminiSearchGeoResolver
{
    ValueTask<GeminiSearchGeo?> ResolveAsync(string ipAddress, CancellationToken token = default);
}

/// <summary>Normalized geographic metadata persisted with a Search widget page view.</summary>
public class GeminiSearchGeo
{
    public long? Asn { get; set; }
    public string? Organization { get; set; }
    public string? ContinentCode { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? RegionCode { get; set; }
    public string? RegionName { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? TimeZone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Returns a canonical IPv4/IPv6 address, or null for an invalid value.</summary>
    public static string? NormalizeIpAddress(string? value)
    {
        if (!IPAddress.TryParse(value?.Trim(), out var address)) return null;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        return address.ToString();
    }
}
