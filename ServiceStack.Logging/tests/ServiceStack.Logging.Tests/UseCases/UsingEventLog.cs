using ServiceStack.Logging.EventLog;
using ServiceStack.Logging.Log4Net;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UsingEventLog
    {
        [Test]
        public void EventLogUseCase()
        {
            LogManager.LogFactory = new EventLogFactory("ServiceStack.Logging.Tests", "Application");
            ILog log = LogManager.GetLogger(GetType());

            log.Debug("Start Logging...");
        }
    }
}