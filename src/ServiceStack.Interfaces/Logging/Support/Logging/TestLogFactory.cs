using System;

namespace ServiceStack.Logging.Support.Logging
{
    /// <summary>
    /// Creates a test Logger, that stores all log messages in a member list
    /// </summary>
	public class TestLogFactory : ILogFactory
    {
        public ILog GetLogger(Type type)
        {
            return new TestLogger(type);
        }

        public ILog GetLogger(string typeName)
        {
            return new TestLogger(typeName);
        }
    }
}
