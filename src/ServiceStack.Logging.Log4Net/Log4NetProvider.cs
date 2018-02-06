#if NETSTANDARD2_0

using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace ServiceStack.Logging.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        /// <summary>
        /// The Dictionary containing the associated logger implementations of each category
        /// </summary>
        private readonly ConcurrentDictionary<string, Log4NetLogger> loggers =
            new ConcurrentDictionary<string, Log4NetLogger>();

        public Log4NetProvider()
        {
        }

        public Log4NetProvider(string log4NetConfigFile)
        {
            Parselog4NetConfigFile(log4NetConfigFile);
        }

        /// <summary>
        /// Create a new <see cref="ILogger"/> implementation for a category
        /// </summary>
        /// <param name="categoryName">the category name.</param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        /// <summary>
        /// Create a new <see cref="ILogger"/> implementation.
        /// </summary>
        /// <param name="name">the name.</param>
        /// <returns></returns>
        private Log4NetLogger CreateLoggerImplementation(string name)
        {
            return new Log4NetLogger(name);
        }

        /// <summary>
        /// Create a new <see cref="ILogger"/> implementation with a configuration file.
        /// </summary>
        /// <param name="name">the name.</param>
        /// <param name="log4NetConfigFile">the file uri.</param>
        /// <returns></returns>
        private Log4NetLogger CreateLoggerImplementation(string name, string log4NetConfigFile)
        {
            Parselog4NetConfigFile(log4NetConfigFile);
            return new Log4NetLogger(name);
        }

        /// <summary>
        /// Parse a configuration file given his uri
        /// </summary>
        /// <param name="log4NetConfigFile">the file uri.</param>
        private static void Parselog4NetConfigFile(string log4NetConfigFile)
        {
            ILoggerRepository rootRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());

            if (File.Exists(log4NetConfigFile))
                log4net.Config.XmlConfigurator.ConfigureAndWatch(rootRepository, new FileInfo(log4NetConfigFile));
            else
                log4net.Config.XmlConfigurator.Configure(rootRepository);
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }

    public static class Log4netExtensions
    {
        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory, string log4NetConfigFile)
        {
            factory.AddProvider(new Log4NetProvider(log4NetConfigFile));
            return factory;
        }

        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory)
        {
            factory.AddProvider(new Log4NetProvider());
            return factory;
        }
    }
}

#endif