using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public static class TaskExt
{
    public static Task<object> AsTaskException(this Exception ex)
    {
        return Task.FromException<object>(ex);
    }

    public static Task<T> AsTaskException<T>(this Exception ex)
    {
        return Task.FromException<T>(ex);
    }

    public static Task<T> AsTaskResult<T>(this T result)
    {
        return Task.FromResult(result);
    }

    public static object GetResult(this Task task)
    {
        if (task == null)
            return null;

        try
        {
            if (!task.IsCompleted)
                task.Wait();

            if (task is Task<object> taskObj)
                return taskObj.Result;

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
        if (task == null)
            return default;

        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ex.UnwrapIfSingleException();
        }
    }
        
    private static readonly TaskFactory SyncTaskFactory = new TaskFactory(CancellationToken.None,
        TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
    public static void RunSync(Func<Task> task) => SyncTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
    public static TResult RunSync<TResult>(Func<Task<TResult>> task) => SyncTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
        
    public static ValueTask AsValueTask(this Task task) => new ValueTask(task);
    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new ValueTask<T>(task);
        
#if NET6_0_OR_GREATER
    public static void Wait(this ValueTask task) => task.AsTask().Wait();
#else
        public static Task AsTask(this Task task) => task; //compat src with .NET6+
#endif
}
