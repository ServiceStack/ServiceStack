using ServiceStack.Logging;
using System;
using System.Web;

namespace ServiceStack.Logging.Elmah
{
    /// <summary>
    /// Elmah log factory that wraps another log factory, providing interception facilities on log calls.  For Error or Fatal calls, the
    /// details will be logged to Elmah in addition to the originally intended logger.  For all other log types, only the original logger is
    /// used.
    /// </summary>
    /// <remarks>	9/2/2011. </remarks>
    public class ElmahLogFactory : ILogFactory
    {
        private readonly ILogFactory logFactory;
        private readonly HttpApplication application;

        /// <summary>	Constructor. </summary>
        /// <remarks>	9/2/2011. </remarks>
        /// <param name="logFactory">	The log factory that provides the original . </param>
        /// <param name="application"> The Http Application to log with. </param>
        public ElmahLogFactory(ILogFactory logFactory, HttpApplication application)
        {
            if (null == logFactory) { throw new ArgumentNullException("logFactory"); }
            if (null == application) { throw new ArgumentNullException("application"); }

            this.logFactory = logFactory;
            this.application = application;
        }

        /// <summary>	Gets a logger from the wrapped logFactory. </summary>
        /// <remarks>	9/2/2011. </remarks>
        /// <param name="typeName">	Name of the type. </param>
        /// <returns>	The logger. </returns>
        public ILog GetLogger(string typeName)
        {
            return new ElmahInterceptingLogger(this.logFactory.GetLogger(typeName), application);
        }

        /// <summary>	Gets a logger from the wrapped logFactory. </summary>
        /// <remarks>	9/2/2011. </remarks>
        /// <param name="type">	The type. </param>
        /// <returns>	The logger. </returns>
        public ILog GetLogger(Type type)
        {
            return new ElmahInterceptingLogger(this.logFactory.GetLogger(type), application);
        }
    }
}
