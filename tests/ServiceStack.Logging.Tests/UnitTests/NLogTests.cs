using System;
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
    }
}