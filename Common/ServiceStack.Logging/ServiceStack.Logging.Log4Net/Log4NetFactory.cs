using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Logging.Log4Net
{
    /// <summary>
    /// ILogFactory that creates an Log4Net ILog logger
    /// </summary>
    public class Log4NetFactory : ILogFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetFactory"/> class.
        /// </summary>
        public Log4NetFactory() : this(false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetFactory"/> class.
        /// </summary>
        /// <param name="configureLog4Net">if set to <c>true</c> [will use the xml definition in App.Config to configure log4 net].</param>
        public Log4NetFactory(bool configureLog4Net)
        {
            if (configureLog4Net)
            {
                log4net.Config.XmlConfigurator.Configure();
            }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return new Log4NetLogger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return new Log4NetLogger(typeName);
        }
    }
}
