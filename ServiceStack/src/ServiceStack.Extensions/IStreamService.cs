using System.Collections.Generic;
using System.Threading;

namespace ServiceStack;

public interface IStreamService { }
    
public interface IStreamService<in TRequest, out TResponse> : IStreamService
{
    IAsyncEnumerable<TResponse> Stream(TRequest request, CancellationToken cancel = default);
}