// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class TaskUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FromResult<T>(T result) => Task.FromResult(result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> InTask<T>(this T result) => Task.FromResult(result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> InTask<T>(this Exception ex)
        {
            var taskSource = new TaskCompletionSource<T>();
            taskSource.TrySetException(ex);
            return taskSource.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccess(this Task task) => !task.IsFaulted && task.IsCompleted;

        public static Task<To> Cast<From, To>(this Task<From> task) where To : From => task.Then(x => (To)x);

        public static TaskScheduler SafeTaskScheduler()
        {
            //Unit test runner
            if (SynchronizationContext.Current != null)
                return TaskScheduler.FromCurrentSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            return TaskScheduler.FromCurrentSynchronizationContext();
        }

        public static Task<To> Then<From, To>(this Task<From> task, Func<From, To> fn)
        {
            var tcs = new TaskCompletionSource<To>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(fn(t.Result));
            }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public static Task Then(this Task task, Func<Task, Task> fn)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(fn(t));
            }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        //http://stackoverflow.com/a/13904811/85785
        public static Task EachAsync<T>(this IEnumerable<T> items, Func<T, int, Task> fn)
        {
            var tcs = new TaskCompletionSource<object>();

            var enumerator = items.GetEnumerator();
            var i = 0;

            Action<Task> next = null;
            next = t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    StartNextIteration(tcs, fn, enumerator, ref i, next);
            };

            StartNextIteration(tcs, fn, enumerator, ref i, next);

            tcs.Task.ContinueWith(_ => enumerator.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        static void StartNextIteration<T>(TaskCompletionSource<object> tcs, 
            Func<T, int, Task> fn, 
            IEnumerator<T> enumerator, 
            ref int i, 
            Action<Task> next)
        {
            bool moveNext;
            try
            {
                moveNext = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            if (!moveNext)
            {
                tcs.SetResult(null);
                return;
            }

            Task iterationTask = null;
            try
            {
                iterationTask = fn(enumerator.Current, i);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            i++;

            iterationTask?.ContinueWith(next, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public static void Sleep(int timeMs)
        {
            Thread.Sleep(timeMs);
        }

        public static void Sleep(TimeSpan time)
        {
            Thread.Sleep(time);
        }
    }
}