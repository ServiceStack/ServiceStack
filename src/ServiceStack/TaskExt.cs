using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class TaskExt
    {
        public static Task<object> AsTaskException(this Exception ex)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(ex);
            return tcs.Task;
        }

        public static Task<T> AsTaskException<T>(this Exception ex)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(ex);
            return tcs.Task;
        }

        public static Task<T> AsTaskResult<T>(this T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static object GetResult(this Task task)
        {
            try
            {
                if (!task.IsCompleted)
                    task.Wait();

                var taskType = task.GetType();
                if (!taskType.IsGenericType || taskType.FullName.Contains("VoidTaskResult"))
                    return null;

                var props = TypeProperties.Get(taskType);
                var fn = props.GetPublicGetter("Result");
                return fn?.Invoke(task);
            }
            catch (TypeAccessException)
            {
                return null; //return null for void Task's
            }
            catch (Exception ex)
            {
                throw ex.UnwrapIfSingleException();
            }
        }

        public static T GetResult<T>(this Task<T> task)
        {
            return (T)((Task)task).GetResult();
        }
    }

    //https://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx
    internal sealed class AsyncLock
    {
        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> m_releaser;

        public AsyncLock()
        {
            m_releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => (IDisposable)state,
                    m_releaser.Result, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
            public void Dispose() { m_toRelease.m_semaphore.Release(); }
        }
    }
}