using ServiceStack.Logging.EventLog;
using ServiceStack.Logging.Log4Net;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class EventLogFactoryTests
    {
        [Test]
        public void EventLogFactoryTest()
        {
            EventLogFactory factory = new EventLogFactory("ServiceStack.Logging.Tests", "Application");
            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as EventLogger);

            factory = new EventLogFactory("ServiceStack.Logging.Tests");
            log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as EventLogger);
        }
    }
}