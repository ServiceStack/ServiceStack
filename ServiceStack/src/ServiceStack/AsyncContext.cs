using System;
using System.Threading.Tasks;

namespace ServiceStack;

public class AsyncContext
{
    public virtual Task ContinueWith(Task task, Action<Task> fn)
    {
        return task.ContinueWith(fn);
    }

    public virtual Task ContinueWith(Task task, Action<Task> fn, TaskContinuationOptions continuationOptions)
    {
        return task.ContinueWith(fn, continuationOptions);
    }
}