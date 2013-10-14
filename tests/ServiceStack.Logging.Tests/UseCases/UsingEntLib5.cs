using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using NUnit.Framework;
using ServiceStack.Logging.EntLib5;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UsingEntLib5
    {
        [Test]
        public void EntLib5UseCase()
        {
            // construct the Configuration Source to use
            var builder = new ConfigurationSourceBuilder();

            // fluent API configuration
            builder.ConfigureLogging()
                .WithOptions
                    .DoNotRevertImpersonation()
                .LogToCategoryNamed("Simple")
                    .SendTo.FlatFile("Simple Log File")
                     .FormatWith(new FormatterBuilder()
                        .TextFormatterNamed("simpleFormat")
                            .UsingTemplate("{timestamp} : {message}{newline}"))
                     .ToFile("simple.log");

            var configSource = new DictionaryConfigurationSource();
            builder.UpdateConfigurationWithReplace(configSource);
            EnterpriseLibraryContainer.Current
                = EnterpriseLibraryContainer.CreateDefaultContainer(configSource);

            LogManager.LogFactory = new EntLib5Factory(configSource);

            ILog log = LogManager.GetLogger(GetType());

            log.Debug("Debug Event Log Entry.");
            log.Warn("Warning Event Log Entry.");
        }
    }
}