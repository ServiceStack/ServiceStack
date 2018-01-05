using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Logging.Log4Net;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class Log4NetFactoryTests
    {
        [Test]
        public void Log4NetFactoryTest()
        {
            Log4NetFactory factory = new Log4NetFactory();
            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as Log4NetLogger);

            factory = new Log4NetFactory(true);
            log = factory.GetLogger(GetType().Name);
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as Log4NetLogger);
        }

        [Test]
        public void Log4NetFactoryTestWithExistingConfigFile()
        {
            const string configFile = "log4net.Test.config";

            // R# Tests stopped copying required files
            if (!File.Exists(configFile))
            {
                Console.WriteLine($"{configFile} was not copied to {Environment.CurrentDirectory}");
                return;
            }

            Assert.IsTrue(File.Exists(configFile), "Test setup failure. Required log4net config file is missing.");

            Log4NetFactory factory = new Log4NetFactory(configFile);

            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as Log4NetLogger);
        }
    }
}