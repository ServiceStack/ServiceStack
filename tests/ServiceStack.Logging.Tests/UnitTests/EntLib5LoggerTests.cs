using System;
using ServiceStack.Logging.EntLib5;
using NUnit.Framework;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class EntLib5LoggerTests
    {
        [Test]
        public void EntLib5LoggerTest()
        {
            ILog log = new EntLib5Logger();
            Assert.IsNotNull(log);
        }

        [Test]
        public void EntLib5Logger_LoggingTest()
        {
            const string message = "Error Message";
            Exception ex = new Exception();
            //string messageFormat = "Message Format: message: {0}, exception: {1}";

            ILog log = new EntLib5Logger();
            Assert.IsNotNull(log);

            log.Debug(message);
            log.Debug(message, ex);
            //log.DebugFormat(messageFormat, message, ex.Message);

            log.Error(message);
            log.Error(message, ex);
            //log.ErrorFormat(messageFormat, message, ex.Message);

            log.Fatal(message);
            log.Fatal(message, ex);
            //log.FatalFormat(messageFormat, message, ex.Message);

            log.Info(message);
            log.Info(message, ex);
            //log.InfoFormat(messageFormat, message, ex.Message);

            log.Warn(message);
            log.Warn(message, ex);
            //log.WarnFormat(messageFormat, message, ex.Message);
        }
    }
}