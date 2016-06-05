using System;
using ServiceStack.Logging.EventLog;
using ServiceStack.Logging.Log4Net;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class EventLoggerTests
    {
        [Test]
        public void EventLoggerTest()
        {
            ILog log = new EventLogger("ServiceStack.Logging.Tests", "Application");
            Assert.IsNotNull(log);
        }

        [Test]
        public void EventLogger_NullLogNameTest()
        {
            Assert.Throws<ArgumentNullException>(() => {
                ILog log = new EventLogger(null, "Application");
            });
        }

        [Test]
        public void EventLogger_NullSourceNameTest()
        {
            Assert.Throws<ArgumentNullException>(() => {
                ILog log = new EventLogger("ServiceStack.Logging.Tests", null);
            });
        }

        [Test]
        public void EventLogger_LoggingTest()
        {
            string message = "Error Message";
            Exception ex = new Exception("Exception");
            string messageFormat = "Message Format: message: {0}, exception: {1}";

            ILog log = new EventLogger("ServiceStack.Logging.Tests", "Application");
            Assert.IsNotNull(log);

            log.Debug(message);
            log.Debug(message, ex);
            log.DebugFormat(messageFormat, message, ex.Message);

            log.Error(message);
            log.Error(message, ex);
            log.ErrorFormat(messageFormat, message, ex.Message);

            log.Fatal(message);
            log.Fatal(message, ex);
            log.FatalFormat(messageFormat, message, ex.Message);

            log.Info(message);
            log.Info(message, ex);
            log.InfoFormat(messageFormat, message, ex.Message);

            log.Warn(message);
            log.Warn(message, ex);
            log.WarnFormat(messageFormat, message, ex.Message);
        }
    }
}