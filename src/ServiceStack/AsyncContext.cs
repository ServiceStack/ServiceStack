using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack
{
    public class AsyncContext
    {
        public virtual Task ContinueWith(Task task, Action<Task> fn)
        {
            return task.ContinueWith(fn);
        }

        public Task<TResult> ContinueWith<TResult>(Task task, Func<Task, TResult> fn)
        {
            return task.ContinueWith(fn);
        }

        public virtual Task ContinueWith(Task task, Action<Task> fn, TaskContinuationOptions continuationOptions)
        {
            return task.ContinueWith(fn, continuationOptions);
        }

        public virtual Task ContinueWith(IRequest req, Task task, Action<Task> fn, CancellationToken token = default(CancellationToken))
        {
            return task.ContinueWith(fn, token);
        }

        public virtual Task ContinueWith(IRequest req, Task task, Action<Task> fn, TaskContinuationOptions continuationOptions)
        {
            return task.ContinueWith(fn, continuationOptions);
        }

        public virtual Task<Task<object>> ContinueWith(IRequest req, Task<object> task, Func<Task<object>, Task<object>> fn)
        {
            return task.ContinueWith(fn);
        }

        public virtual Task<object> ContinueWith(IRequest req, Task task, Func<Task, object> fn)
        {
            return task.ContinueWith(fn);
        }

        public virtual Task<object> ContinueWith<T>(IRequest req, Task<T> task, Func<Task<T>, object> fn)
        {
            return task.ContinueWith(fn);
        }

        public virtual Task<T> ContinueWith<T>(IRequest req, Task<object> task, Func<Task<object>, T> fn)
        {
            return task.ContinueWith(fn);
        }

        public virtual Task<T> ContinueWith<T>(IRequest req, Task<T> task, Func<Task<T>, T> fn, CancellationToken token)
        {
            return task.ContinueWith(fn, token);
        }

        public virtual Task<List<T>> ContinueWith<T>(IRequest req, Task<T[]> task, Func<Task<T[]>, List<T>> fn, CancellationToken token = default(CancellationToken))
        {
            return task.ContinueWith(fn, token);
        }
    }
}