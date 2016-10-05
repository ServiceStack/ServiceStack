using System;
using System.Threading.Tasks;
using ServiceStack.Reflection;

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
                task.Wait();

                var taskType = task.GetType();
                if (!taskType.IsGenericType() || taskType.FullName.Contains("VoidTaskResult"))
                    return null;

                var fn = taskType.GetFastGetter("Result");
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

}