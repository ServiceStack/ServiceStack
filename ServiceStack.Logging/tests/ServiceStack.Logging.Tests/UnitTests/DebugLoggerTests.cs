#if FALSE
using System;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class DebugLoggerTests
    {
        [Test]
        public void Log4NetLoggerTest()
        {
            ILog log = new DebugLogger(GetType());
            Assert.IsNotNull(log);

            log = new DebugLogger(GetType().Name);
            Assert.IsNotNull(log);
        }

        [Test]
        public void DebugLogger_LoggingTest()
        {
            string message = "Error Message";
            Exception ex = new Exception();
            string messageFormat = "Message Format: message: {0}, exception: {1}";

            ILog log = new DebugLogger(GetType());
            Assert.IsNotNull(log);

            log.Debug(message);
            log.Debug(message, ex);
            log.DebugFormat(messageFormat, messageFormat, ex.Message);

            log.Error(message);
            log.Error(message, ex);
            log.ErrorFormat(messageFormat, messageFormat, ex.Message);

            log.Fatal(message);
            log.Fatal(message, ex);
            log.FatalFormat(messageFormat, messageFormat, ex.Message);

            log.Info(message);
            log.Info(message, ex);
            log.InfoFormat(messageFormat, messageFormat, ex.Message);

            log.Warn(message);
            log.Warn(message, ex);
            log.WarnFormat(messageFormat, messageFormat, ex.Message);
        }
    }
}
#endif
