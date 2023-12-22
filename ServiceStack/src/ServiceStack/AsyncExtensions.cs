using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

internal static class AsyncExtensions
{
    //http://bradwilson.typepad.com/blog/2012/04/tpl-and-servers-pt3.html
    public static Task<TOut> Continue<TOut>(
        this Task task,
        Func<Task, TOut> next)
    {
        if (task.IsCompleted)
        {
            var tcs = new TaskCompletionSource<TOut>();

            try
            {
                var res = next(task);
                tcs.TrySetResult(res);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        return ContinueClosure(task, next);
    }

    static Task<TOut> ContinueClosure<TOut>(
        Task task,
        Func<Task, TOut> next)
    {
        var ctxt = SynchronizationContext.Current;
        return task.ContinueWith(innerTask =>
        {
            var tcs = new TaskCompletionSource<TOut>();

            try
            {
                if (ctxt != null)
                {
                    ctxt.Post(state =>
                    {
                        try
                        {
                            var res = next(innerTask);
                            tcs.TrySetResult(res);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, state: null);
                }
                else
                {
                    var res = next(innerTask);
                    if (res is Task t && t.IsFaulted)
                    {
                        tcs.TrySetException(t.Exception);
                    }
                    else
                    {
                        tcs.TrySetResult(res);
                    }
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }).Unwrap();
    }
}