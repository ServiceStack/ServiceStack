// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Threading.Tasks;

namespace ServiceStack
{
    public static class TaskResult
    {
        public static Task<int> Zero;
        public static Task<int> One;
        public static readonly Task<bool> True;
        public static readonly Task<bool> False;
        public static readonly Task Finished;
        public static readonly Task Canceled;

        static TaskResult()
        {
            Finished = ((object)null).InTask();
            True = true.InTask();
            False = false.InTask();
            Zero = 0.InTask();
            One = 1.InTask();

            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            Canceled = tcs.Task;
        }         
    }

    internal class TaskResult<T>
    {
        public static readonly Task<T> Canceled;
        public static readonly Task<T> Default;

        static TaskResult()
        {
            Default = ((T)typeof(T).GetDefaultValue()).InTask();

            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            Canceled = tcs.Task;
        }
    }
}