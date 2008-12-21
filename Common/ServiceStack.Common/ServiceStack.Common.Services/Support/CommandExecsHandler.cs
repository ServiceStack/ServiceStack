using System.Threading;
using ServiceStack.Common.DesignPatterns.Command;

namespace ServiceStack.Common.Services.Support
{
    public class CommandExecsHandler : ICommandExec
    {
        private readonly ICommandExec command;
        private readonly AutoResetEvent waitHandle;

        public CommandExecsHandler(ICommandExec command, AutoResetEvent waitHandle)
        {
            this.command = command;
            this.waitHandle = waitHandle;
        }

        #region ICommandExec Members

        public bool Execute()
        {
            command.Execute();
            waitHandle.Set();
            return true;
        }

        #endregion
    }
}