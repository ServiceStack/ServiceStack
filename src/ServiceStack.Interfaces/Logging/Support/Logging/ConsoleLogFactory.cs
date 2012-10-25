using System;

namespace ServiceStack.Logging.Support.Logging
{
    /// <summary>
    /// Creates a Debug Logger, that logs all messages to: System.Diagnostics.Debug
    /// 
    /// Made public so its testable
    /// </summary>
	public class ConsoleLogFactory : ILogFactory
    {
        public ILog GetLogger(Type type)
        {
            return new ConsoleLogger(type);
        }

        public ILog GetLogger(string typeName)
        {
			return new ConsoleLogger(typeName);
        }
    }
}
