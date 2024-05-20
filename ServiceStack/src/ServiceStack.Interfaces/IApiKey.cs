#nullable enable
using System;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack;

public interface IApiKey : IMeta
{
    string Key { get; }
    string? Environment { get; }
    DateTime CreatedDate { get; }
    DateTime? ExpiryDate { get; }
    DateTime? CancelledDate { get; }
    int? RefId { get; }
    string RefIdStr { get; }
    bool HasScope(string scope);
}

public interface IApiKeyResolver
{
    string? GetApiKeyToken(IRequest req);
}

public interface IApiKeySource
{
    Task<IApiKey?> GetApiKeyAsync(string key);
}
