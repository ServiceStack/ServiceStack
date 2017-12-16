using ServiceStack.Logging;
using System;
using System.Web;
using Elmah;

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

        internal class ErrorFilterConsole : ErrorFilterModule
        {
            public void HookFiltering(IExceptionFiltering module)
            {
                module.Filtering += base.OnErrorModuleFiltering;
            }
        }

        private readonly ILogFactory logFactory;
        private readonly HttpApplication application;

        // Filters and modules for HttpApplication-less Elmah logging
        private ErrorFilterConsole errorFilter = new ErrorFilterConsole();
        public ErrorMailModule ErrorEmail = new ErrorMailModule();
        public ErrorLogModule ErrorLog = new ErrorLogModule();
        public ErrorTweetModule ErrorTweet = new ErrorTweetModule();

        /// <summary>	Constructor. </summary>
        /// <remarks>	9/2/2011. </remarks>
        /// <param name="logFactory">	The log factory that provides the original. </param>
        /// <param name="application"> The Http Application to log with. Optional parameter in case of self hosting.</param>
        public ElmahLogFactory(ILogFactory logFactory, HttpApplication application = null)
        {
            if (application == null)
            {
                application = InitNoContext();
            }

            this.logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            this.application = application;
        }

        private HttpApplication InitNoContext()
        {
            var httpApplication = new HttpApplication();
            errorFilter.Init(httpApplication);

            (ErrorEmail as IHttpModule).Init(httpApplication);
            errorFilter.HookFiltering(ErrorEmail);

            (ErrorLog as IHttpModule).Init(httpApplication);
            errorFilter.HookFiltering(ErrorLog);

            (ErrorTweet as IHttpModule).Init(httpApplication);
            errorFilter.HookFiltering(ErrorTweet);
            return httpApplication;

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
