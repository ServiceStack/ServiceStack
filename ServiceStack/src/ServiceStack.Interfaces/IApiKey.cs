#nullable enable
using System;
using System.Threading.Tasks;

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

public interface IApiKeySource
{
    Task<IApiKey?> GetApiKeyAsync(string key);
}
