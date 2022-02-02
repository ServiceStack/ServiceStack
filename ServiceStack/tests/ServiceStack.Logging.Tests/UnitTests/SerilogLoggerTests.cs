using System;
using NUnit.Framework;
using ServiceStack.Logging.Serilog;
using global::Serilog;
using ServiceStack.Logging.Tests.UseCases;
using ServiceStack.Text;

namespace ServiceStack.Logging.Tests.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Serilog.Core;
    using global::Serilog.Events;

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
            log.Debug(ex, messageFormat, messageFormat, ex);

            log.Error(message);
            log.Error(message, ex);
            log.ErrorFormat(messageFormat, message, ex.Message);
            log.Error(ex, messageFormat, messageFormat, ex);

            log.Fatal(message);
            log.Fatal(message, ex);
            log.FatalFormat(messageFormat, message, ex.Message);
            log.Fatal(ex, messageFormat, messageFormat, ex);

            log.Info(message);
            log.Info(message, ex);
            log.InfoFormat(messageFormat, message, ex.Message);
            log.Info(ex, messageFormat, messageFormat, ex);

            log.Warn(message);
            log.Warn(message, ex);
            log.WarnFormat(messageFormat, message, ex.Message);
            log.Warn(ex, messageFormat, messageFormat, ex);
        }

        [Test]
        public void ForContextAddingPropertiesTests()
        {
            var dummySink = new DummySink();
            var log = new SerilogLogger(new LoggerConfiguration().WriteTo.Sink(dummySink).CreateLogger());

            var messageTemplate = "Testing adding {prop2} props";
            log.ForContext("prop", "value").InfoFormat(messageTemplate, "awesome");
            Log.CloseAndFlush();

            var result = dummySink.Events.SingleOrDefault();

            Assert.NotNull(result);
            Assert.AreEqual(LogEventLevel.Information, result.Level);
            Assert.AreEqual(messageTemplate, result.MessageTemplate.Text);
            Assert.True(result.Properties.ContainsKey("prop"));
            Assert.AreEqual("\"value\"", result.Properties["prop"].ToString());
            Assert.True(result.Properties.ContainsKey("prop2"));
            Assert.AreEqual("\"awesome\"", result.Properties["prop2"].ToString());
        }

        [Test]
        public void PushPropertyTests()
        {
            var dummySink = new DummySink();
            LogManager.LogFactory = new SerilogFactory(new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(dummySink)
                .CreateLogger());

            var log = LogManager.GetLogger(typeof(SerilogLoggerTests));

            using (log.PushProperty("A", "1"))
            using (log.PushProperty("B", "2"))
            {
                log.Info("log entry");
            }

            var result = dummySink.Events[0];
            Assert.That(result.Properties.Any(x => x.Key == "A"));
            Assert.That(result.Properties.Any(x => x.Key == "B"));
        }

    }

    internal class DummySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }

        public List<LogEvent> Events { get; } = new List<LogEvent>();
    }
}
