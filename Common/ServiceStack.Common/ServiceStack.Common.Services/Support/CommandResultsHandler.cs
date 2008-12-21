using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common.DesignPatterns.Command;

namespace ServiceStack.Common.Services.Support
{
    public class CommandResultsHandler<T> : ICommandExec
    {
        private readonly List<T> results;
        private readonly ICommandList<T> command;
        private readonly AutoResetEvent waitHandle;

        public CommandResultsHandler(List<T> results, ICommandList<T> command, AutoResetEvent waitHandle)
        {
            this.results = results;
            this.command = command;
            this.waitHandle = waitHandle;
        }

        #region ICommandExec Members

        public bool Execute()
        {
            results.AddRange(command.Execute());
            waitHandle.Set();
            return true;
        }

        #endregion
    }
}