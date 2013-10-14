using System;
using ServiceStack.Logging.Log4Net;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class Log4NetLoggerTests
    {
        [Test]
        public void Log4NetLoggerTest()
        {
            ILog log = new Log4NetLogger(GetType());
            Assert.IsNotNull(log);

            log = new Log4NetLogger(GetType().Name);
            Assert.IsNotNull(log);
        }

        [Test]
        public void Log4NetLogger_LoggingTest()
        {
            string message = "Error Message";
            Exception ex = new Exception();
            string messageFormat = "Message Format: message: {0}, exception: {1}";

            ILog log = new Log4NetLogger(GetType());
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