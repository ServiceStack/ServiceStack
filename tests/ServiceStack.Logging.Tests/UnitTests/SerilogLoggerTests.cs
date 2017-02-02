using System;
using NUnit.Framework;
using ServiceStack.Logging.Serilog;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class SerilogLoggerTests
    {
        [Test]
        public void Instantiation()
        {
            var log = new SerilogLogger(GetType());
            Assert.IsNotNull(log);
        }

        [Test]
        public void Logging()
        {
            const string message = "Error Message";
            const string messageFormat = "Message Format: message: {0}, exception: {1}";
            var ex = new Exception();

            var log = new SerilogLogger(GetType());
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
