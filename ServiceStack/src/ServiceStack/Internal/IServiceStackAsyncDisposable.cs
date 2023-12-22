using System;
using System.Threading.Tasks;

namespace ServiceStack.Internal;

// intent: like IAsyncDisposable, but usable on net45 TFM
internal interface IServiceStackAsyncDisposable
{
    Task DisposeAsync();
}

internal static class DisposableExtensions
{
    internal static Task DisposeAsync(this IDisposable disposable)
    {
        // note: if a type clearly implements IServiceStackAsyncDisposable, overload
        // resolution will prefer IServiceStackAsyncDisposable over an extension method,
        // so we lose nothing by doing this
        if (disposable is IServiceStackAsyncDisposable ssAsyncDisposable)
            return ssAsyncDisposable.DisposeAsync();
        if (disposable is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync().AsTask();
        disposable?.Dispose();
        return TypeConstants.EmptyTask;
    }
}