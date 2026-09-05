using System;
using System.Threading;
using ServiceStack.Commands;

namespace ServiceStack.Support
{
    public class ActionExecHandler : ICommandExec
    {
        private readonly Action action;
        private readonly AutoResetEvent waitHandle;

        public ActionExecHandler(Action action, AutoResetEvent waitHandle)
        {
            this.action = action;
            this.waitHandle = waitHandle;
        }

        public bool Execute()
        {
            try
            {
                action();
            }
            finally
            {
                waitHandle.Set();
            }
            return true;
        }
    }
}