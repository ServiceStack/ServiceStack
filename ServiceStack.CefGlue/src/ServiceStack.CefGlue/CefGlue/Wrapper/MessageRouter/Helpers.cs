namespace Xilium.CefGlue.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class Helpers
    {
        public static void PostTask(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }

        public static void PostTaskUncertainty(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }

        internal sealed class ActionTask : CefTask
        {
            public Action _action;

            public ActionTask(Action action)
            {
                _action = action;
            }

            protected override void Execute()
            {
                _action();
                _action = null;
            }
        }

        internal static void RequireUIThread()
        {
            if (!CefRuntime.CurrentlyOn(CefThreadId.UI))
                throw new InvalidOperationException("This method should be called on CEF UI thread.");
        }

        internal static void RequireRendererThread()
        {
            if (!CefRuntime.CurrentlyOn(CefThreadId.Renderer))
                throw new InvalidOperationException("This method should be called on CEF renderer thread.");
        }



        internal delegate void Action();
        internal delegate void Action<T1, T2>(T1 arg1, T2 arg2);
        internal delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
        internal delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        internal delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        //internal delegate TResult Func<TResult>();

        //internal delegate TResult Func<T, TResult>(T arg);

        //internal delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);

        public static Action Apply<T>(Action<T> action, T arg)
        {
            return () => action(arg);
        }

        public static Action Apply<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return () => action(arg1, arg2);
        }

        public static Action Apply<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return () => action(arg1, arg2, arg3);
        }

        public static Action Apply<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return () => action(arg1, arg2, arg3, arg4);
        }

        public static Action Apply<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return () => action(arg1, arg2, arg3, arg4, arg5);
        }

        public static long Int64Set(int low, int high)
        {
            return (uint)low | (long)high << 32;
        }

        public static int Int64GetLow(long value)
        {
            return (int)value;
        }

        public static int Int64GetHigh(long value)
        {
            return (int)(value >> 32);
        }
    }
}
