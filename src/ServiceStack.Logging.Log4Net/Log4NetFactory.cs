using System;
using System.IO;

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
#if NETSTANDARD2_0
                log4net.Config.XmlConfigurator.Configure(RootRepository);
#else
                log4net.Config.XmlConfigurator.Configure();
#endif
            }
        }
        
#if NETSTANDARD2_0
        private log4net.Repository.ILoggerRepository rootRepository;
        public log4net.Repository.ILoggerRepository RootRepository => rootRepository ??
          (rootRepository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()));
#endif
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetFactory"/> class.
        /// </summary>
        /// <param name="log4NetConfigurationFile">The log4 net configuration file to load and watch. If not found configures from App.Config.</param>
        public Log4NetFactory(string log4NetConfigurationFile)
        {
#if NETSTANDARD2_0
            if (File.Exists(log4NetConfigurationFile))
                log4net.Config.XmlConfigurator.ConfigureAndWatch(RootRepository, new FileInfo(log4NetConfigurationFile));
            else
                log4net.Config.XmlConfigurator.Configure(RootRepository);
#else
            //Restart logging if necessary
            log4net.Repository.ILoggerRepository rootRepository = log4net.LogManager.GetRepository();
            if (rootRepository != null)
                rootRepository.Shutdown();

            if (File.Exists(log4NetConfigurationFile))
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfigurationFile));
            else
                log4net.Config.XmlConfigurator.Configure();
#endif
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
