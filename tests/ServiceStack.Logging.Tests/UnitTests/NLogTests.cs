using System;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class NLogTests
    {
        [Test]
        public void Does_maintain_callsite()
        {
            ServiceStack.Logging.LogManager.LogFactory = new ServiceStack.Logging.NLogger.NLogFactory();

            var log = ServiceStack.Logging.LogManager.LogFactory.GetLogger(GetType());
            log.InfoFormat("Message");
            log.InfoFormat("Message with Args {0}", "Foo");
            log.Info("Message with Exception", new Exception("Foo Exception"));
        }
    }
}