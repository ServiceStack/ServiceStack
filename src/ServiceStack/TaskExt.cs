using System;
using System.Threading.Tasks;
using ServiceStack.Text.FastMember;

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

                return TypeAccessor.Create(task.GetType())[task, "Result"];
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
    }

}