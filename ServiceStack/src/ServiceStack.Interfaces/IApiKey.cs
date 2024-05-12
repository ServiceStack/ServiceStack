#nullable enable
using System;

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
}
