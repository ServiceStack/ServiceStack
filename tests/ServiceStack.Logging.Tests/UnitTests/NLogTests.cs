using System;
using NLog;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class NLogTests
    {
        [Test]
        public void Does_maintain_callsite()
        {
            Logging.LogManager.LogFactory = new NLogger.NLogFactory();

            var log = ServiceStack.Logging.LogManager.LogFactory.GetLogger(GetType());
            log.InfoFormat("Message");
            log.InfoFormat("Message with Args {0}", "Foo");
            log.Info("Message with Exception", new Exception("Foo Exception"));
        }

        [Test]
        public void PushPropertyTest()
        {
            Logging.LogManager.LogFactory = new NLogger.NLogFactory();

            var log = Logging.LogManager.LogFactory.GetLogger(GetType());
            using (log.PushProperty("Hello", "World"))
            {
                log.InfoFormat("Message");
            }
        }

        [Test]
        public void Can_call_method_using_NLog_concrete_providers()
        {
            Logging.LogManager.LogFactory = new NLogger.NLogFactory();

            var instance = new NLogExample();
            instance.Method();
        }
    }

    public class NLogExample
    {
        public static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void Method()
        {
            logger.Log(LogLevel.Debug, "Method called");
        }
    }
}