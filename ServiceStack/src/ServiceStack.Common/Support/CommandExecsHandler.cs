using System.Threading;
using ServiceStack.Commands;

namespace ServiceStack.Support
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

        public bool Execute()
        {
            command.Execute();
            waitHandle.Set();
            return true;
        }
    }
}