using ServiceStack.Logging.Log4Net;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UsingLog4Net
    {
        [Test]
        public void Log4NetUseCase()
        {
            LogManager.LogFactory = new Log4NetFactory();
            ILog log = LogManager.GetLogger(GetType());

            log.Debug("Debug Event Log Entry.");
            log.Warn("Warning Event Log Entry.");
        }

        [Test]
        public void Log4NetUseCasePushProperty()
        {
            LogManager.LogFactory = new Log4NetFactory();
            ILog log = LogManager.GetLogger(GetType());

            using (log.PushProperty("Hello", "World"))
            {
                log.Warn("Warning Event Log Entry.");
            }
        }
    }
}