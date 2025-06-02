#nullable enable

namespace ServiceStack;

public interface IUrlFilter
{
    string ToUrl(string absoluteUrl);
}