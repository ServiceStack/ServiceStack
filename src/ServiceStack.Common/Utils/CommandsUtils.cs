using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common.Support;
using ServiceStack.DesignPatterns.Command;
#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace ServiceStack.Common.Utils
{
    public class CommandsUtils
    {

        public static List<T> ExecuteAsyncCommandList<T>(TimeSpan timeout, params ICommandList<T>[] commands)
        {
            return ExecuteAsyncCommandList(timeout, commands);
        }

        public static List<T> ExecuteAsyncCommandList<T>(TimeSpan timeout, IEnumerable<ICommandList<T>> commands)
        {
            var results = new List<T>();
            var waitHandles = new List<WaitHandle>();
            foreach (ICommandList<T> command in commands)
            {
                var waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                var commandResultsHandler = new CommandResultsHandler<T>(results, command, waitHandle);

#if NETFX_CORE
                ThreadPool.RunAsync(new WorkItemHandler((IAsyncAction) => ExecuteCommandList(commandResultsHandler)));
#else
                ThreadPool.QueueUserWorkItem(ExecuteCommandList, commandResultsHandler);
#endif
            }
            WaitAll(waitHandles.ToArray(), timeout);
            return results;
        }


        public static void WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            // throws an exception if there are no wait handles
            if (waitHandles != null && waitHandles.Length > 0)
            {
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    // WaitAll for multiple handles on an STA thread is not supported.
                    // CurrentThread is ApartmentState.STA when run under unit tests
                    foreach (WaitHandle waitHandle in waitHandles)
                    {
                        waitHandle.WaitOne(timeout, false);
                    }
                }
                else
                {
                    if (!WaitHandle.WaitAll(waitHandles, timeout, false))
                    {
                        throw new TimeoutException();
                    }
                }
#else
                if (!WaitHandle.WaitAll(waitHandles, timeout))
                {
                    throw new TimeoutException();
                }
#endif
            }
        }

        private static void ExecuteCommandList(object state)
        {
            var handler = (ICommandExec)state;
            handler.Execute();
        }

        private static void ExecuteCommandExec(object state)
        {
            var command = (ICommandExec)state;
            command.Execute();
        }

        public static void ExecuteAsyncCommandExec(TimeSpan timeout, IEnumerable<ICommandExec> commands)
        {
            foreach (ICommandExec command in commands)
            {
#if NETFX_CORE
                ThreadPool.RunAsync(new WorkItemHandler((IAsyncAction) => ExecuteCommandExec(command)));
#else
                ThreadPool.QueueUserWorkItem(ExecuteCommandExec, command);
#endif
            }
        }

        /// <summary>
        /// Provide the an option for the callee to block until all commands are executed
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static List<WaitHandle> ExecuteAsyncCommandExec(IEnumerable<ICommandExec> commands)
        {
            var waitHandles = new List<WaitHandle>();
            foreach (var command in commands)
            {
                var waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                var commandExecsHandler = new CommandExecsHandler(command, waitHandle);
#if NETFX_CORE
                ThreadPool.RunAsync(new WorkItemHandler((IAsyncAction) => ExecuteCommandList(commandExecsHandler)));
#else
                ThreadPool.QueueUserWorkItem(ExecuteCommandList, commandExecsHandler);
#endif
            }
            return waitHandles;
        }
    }
}