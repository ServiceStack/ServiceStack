using System;
using ServiceStack.Logging;

namespace ServiceStack.Caching.Memcached
{
    public class EnyimLogFactoryWrapper : Enyim.Caching.ILogFactory 
    {
        public Enyim.Caching.ILog GetLogger(string name)
        {
            return new EnyimLoggerWrapper(LogManager.GetLogger(name));
        }

        public Enyim.Caching.ILog GetLogger(Type type)
        {
            return new EnyimLoggerWrapper(LogManager.GetLogger(type));
        }
    }
}
