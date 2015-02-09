using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Creates a test Logger, that stores all log messages in a member list
    /// </summary>
	public class TestLogFactory : ILogFactory
    {
        private readonly bool debugEnabled;

        public TestLogFactory(bool debugEnabled = true)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new TestLogger(type) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new TestLogger(typeName) { IsDebugEnabled = debugEnabled };
        }
    }
}
