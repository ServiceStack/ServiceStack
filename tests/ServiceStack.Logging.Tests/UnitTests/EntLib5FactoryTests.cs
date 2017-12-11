using System;
using System.IO;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using NUnit.Framework;
using ServiceStack.Logging.EntLib5;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class EntLib5FactoryTests
    {
        [Test]
        public void EntLib5FactoryTest()
        {
            // initialize the EntLib5 Logger Factory - will use App.Config for settings
            EntLib5Factory factory = new EntLib5Factory();
            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as EntLib5Logger);
        }

        [Test]
        public void EntLib5FactoryTestWithExistingConfigFile()
        {
            // set up a Configuration file and ensure it exists
            const string configFile = "EntLib5.Test.config";

            // R# Tests stopped copying required files
            if (!File.Exists(configFile))
            {
                Console.WriteLine($"{configFile} was not copied to {Environment.CurrentDirectory}");
                return;
            }

            Assert.IsTrue(File.Exists(configFile), "Test setup failure. Required Enterprise Library config file is missing.");

            // initialize the EntLib5 Logger factory with configuration file
            EntLib5Factory factory = new EntLib5Factory(configFile);

            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as EntLib5Logger);
        }

        [Test]
        public void EntLib5FactoryTestWithFluentConfig()
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

            // initialize the EntLib5 Logger factory with configuration file
            EntLib5Factory factory = new EntLib5Factory();

            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as EntLib5Logger);
        }
    }
}