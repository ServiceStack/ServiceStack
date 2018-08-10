using System;
using NUnit.Framework;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using ServiceStack.Logging.Serilog;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UsingSerilog
    {
        private void TestLog()
        {
            var log = LogManager.GetLogger(GetType());

            log.Debug("Debug Event Log Entry.");
            log.Info("Info Event Log Entry.");
            log.Warn("Warning Event Log Entry.");
            log.Error("Error Event Log Entry.");
            log.Fatal("Fatal Event Log Entry.");
        }

        [Test]
        public void Use_default_SerilogFactory()
        {
            LogManager.LogFactory = new SerilogFactory();
            TestLog();
        }



        [Test]
        public void Use_Serilog_with_custom_configuration_and_sink()
        {
            LogManager.LogFactory = new SerilogFactory(new LoggerConfiguration()
                .WriteTo.MySink()
                .CreateLogger());

            TestLog();
        }
    }

    public class MySink : ILogEventSink
    {
        private readonly IFormatProvider formatProvider;

        public MySink(IFormatProvider formatProvider)
        {
            this.formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(formatProvider);
            Console.WriteLine(DateTimeOffset.Now + " " + message);
        }
    }

    public static class MySinkExtensions
    {
        public static LoggerConfiguration MySink(
            this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new MySink(formatProvider));
        }
    }
}
