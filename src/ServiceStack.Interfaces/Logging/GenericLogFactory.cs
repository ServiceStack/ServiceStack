using System;

namespace ServiceStack.Logging
{
    public class GenericLogFactory : ILogFactory
    {
        public Action<string> OnMessage;

        public GenericLogFactory(Action<string> onMessage)
        {
            OnMessage = onMessage;
        }

        public ILog GetLogger(Type type)
        {
            return new GenericLogger(type) { OnMessage = OnMessage };
        }

        public ILog GetLogger(string typeName)
        {
            return new GenericLogger(typeName) { OnMessage = OnMessage };
        }
    }
}