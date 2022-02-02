using System;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Fluent;

namespace ServiceStack.Logging.EntLib5
{
    public class EntLib5Factory : LogWriterFactory, ILogFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Factory"/> class.
        /// </summary>
        public EntLib5Factory() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Factory"/> class.
        /// </summary>
        /// <param name="EntLib5ConfigurationFile">The enterprise library 5.0 configuration file to load and watch. Supercedes any configuration found in the Config file.</param>
        public EntLib5Factory(string EntLib5ConfigurationFile)
        {
            // verify provided file exists
            var fi = new System.IO.FileInfo(EntLib5ConfigurationFile);
            if (fi.Exists)
            {
                var builder = new ConfigurationSourceBuilder();
                var EntLib5ConfigurationSrc = new FileConfigurationSource(EntLib5ConfigurationFile, true);
                
                builder.UpdateConfigurationWithReplace(EntLib5ConfigurationSrc);
                EnterpriseLibraryContainer.Current = EnterpriseLibraryContainer.CreateDefaultContainer(EntLib5ConfigurationSrc);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Factory"/> class.
        /// </summary>
        /// <param name="EntLib5ConfigurationSrc">The enterprise library 5.0 configuration source to load. Supercedes any configuration found in the Config file.</param>
        public EntLib5Factory(IConfigurationSource EntLib5ConfigurationSrc)
        {
            // replace any settings from App.Config with the ones in the provided config source
            var builder = new ConfigurationSourceBuilder();
            builder.UpdateConfigurationWithReplace(EntLib5ConfigurationSrc);
            EnterpriseLibraryContainer.Current = EnterpriseLibraryContainer.CreateDefaultContainer(EntLib5ConfigurationSrc);
        }


        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return new EntLib5Logger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return new EntLib5Logger(typeName);
        }
    }
}
