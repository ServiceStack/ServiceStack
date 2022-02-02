using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class TaskExtensions
    {
        public static Task<T> Success<T>(this Task<T> task, Action<T> fn,
            bool onUiThread = true,
            TaskContinuationOptions taskOptions = TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.OnlyOnRanToCompletion)
        {
            if (onUiThread)
            {
                var source = new CancellationToken();
                task.ContinueWith(t => fn(t.Result), source, taskOptions, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                task.ContinueWith(t => fn(t.Result), taskOptions);
            }
            return task;
        }

        public static Task Success(this Task task, Action fn,
            bool onUiThread = true,
            TaskContinuationOptions taskOptions = TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.OnlyOnRanToCompletion)
        {
            if (onUiThread)
            {
                var source = new CancellationToken();
                task.ContinueWith(t => fn(), source, taskOptions, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                task.ContinueWith(t => fn(), taskOptions);
            }
            return task;
        }

        public static Task<T> Error<T>(this Task<T> task, Action<Exception> fn,
            bool onUiThread = true,
            TaskContinuationOptions taskOptions = TaskContinuationOptions.OnlyOnFaulted)
        {
            if (onUiThread)
            {
                var source = new CancellationToken();
                task.ContinueWith(t => fn(t.UnwrapIfSingleException()), source, taskOptions, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                task.ContinueWith(t => fn(t.UnwrapIfSingleException()), taskOptions);
            }
            return task;
        }

        public static Task Error(this Task task, Action<Exception> fn,
            bool onUiThread = true,
            TaskContinuationOptions taskOptions = TaskContinuationOptions.OnlyOnFaulted)
        {
            if (onUiThread)
            {
                var source = new CancellationToken();
                task.ContinueWith(t => fn(t.UnwrapIfSingleException()), source, taskOptions, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                task.ContinueWith(t => fn(t.UnwrapIfSingleException()), taskOptions);
            }
            return task;
        }

        public static Exception UnwrapIfSingleException<T>(this Task<T> task)
        {
            return task.Exception.UnwrapIfSingleException();
        }

        public static Exception UnwrapIfSingleException(this Task task)
        {
            return task.Exception.UnwrapIfSingleException();
        }

        public static Exception UnwrapIfSingleException(this Exception ex)
        {
            var aex = ex as AggregateException;
            if (aex == null)
                return ex;

            if (aex.InnerExceptions != null
                && aex.InnerExceptions.Count == 1)
                return aex.InnerExceptions[0].UnwrapIfSingleException();

            return aex;
        }
    }
}