#nullable enable

namespace ServiceStack;

public interface IHasTraceId
{
    string TraceId { get; }
}
