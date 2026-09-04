#if NETFX || NET472
using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Caching.Memcached;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class MemcachedClientCacheUnitTests
    {
        public class TestDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void MemcachedValueWrapper_Serializes_And_Deserializes_Complex_Types()
        {
            var dto = new TestDto { Id = 42, Name = "MemcachedTest" };
            var wrapper = new MemcachedValueWrapper(dto);

            Assert.That(wrapper.ValueType, Is.EqualTo(typeof(TestDto)));
            Assert.That(wrapper.JsonString, Does.Contain("MemcachedTest"));
            Assert.That(wrapper.Value, Is.Not.Null);

            var deserialized = wrapper.Value as TestDto;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(42));
            Assert.That(deserialized.Name, Is.EqualTo("MemcachedTest"));
        }

        [Test]
        public void MemcachedValueWrapper_Handles_Null_Value()
        {
            var wrapper = new MemcachedValueWrapper(null);
            Assert.That(wrapper.Value, Is.Null);
            Assert.That(wrapper.ValueType, Is.Null);
            Assert.That(wrapper.JsonString, Is.Null);
        }

        [Test]
        public void MemcachedValueWrapper_Unwraps_Nested_Wrappers()
        {
            var dto = new TestDto { Id = 100, Name = "Nested" };
            var innerWrapper = new MemcachedValueWrapper(dto);
            var nestedWrapper = new MemcachedValueWrapper(innerWrapper);

            Assert.That(nestedWrapper.ValueType, Is.EqualTo(typeof(TestDto)));
            Assert.That(nestedWrapper.Value, Is.InstanceOf<TestDto>());
            Assert.That(((TestDto)nestedWrapper.Value).Id, Is.EqualTo(100));
        }

        [Test]
        public void MemcachedValueWrapper_Handles_Null_ValueType_Gracefully()
        {
            var wrapper = new MemcachedValueWrapper
            {
                ValueType = null,
                JsonString = "{\"Id\":1,\"Name\":\"Test\"}"
            };

            Assert.DoesNotThrow(() =>
            {
                var val = wrapper.Value;
                Assert.That(val, Is.Not.Null);
            });
        }

        [Test]
        public void MemcachedValueWrapper_Handles_Corrupted_Json_Gracefully()
        {
            var wrapper = new MemcachedValueWrapper
            {
                ValueType = typeof(TestDto),
                JsonString = "Not Valid JSON {{[{"
            };

            Assert.DoesNotThrow(() =>
            {
                var val = wrapper.Value;
                // Graceful fallback returns raw string or null without throwing
                Assert.That(val, Is.Not.Null);
            });
        }

        [Test]
        public void EnyimLoggerWrapper_Handles_Null_Logger_Safely()
        {
            var logger = new EnyimLoggerWrapper(null);

            Assert.DoesNotThrow(() =>
            {
                logger.Debug("debug message");
                logger.Debug("debug", new Exception("ex"));
                logger.DebugFormat("debug format {0}", 1);
                logger.Info("info message");
                logger.Info("info", new Exception("ex"));
                logger.InfoFormat("info format {0}", 1);
                logger.Warn("warn message");
                logger.Warn("warn", new Exception("ex"));
                logger.WarnFormat("warn format {0}", 1);
                logger.Error("error message");
                logger.Error("error", new Exception("ex"));
                logger.ErrorFormat("error format {0}", 1);
                logger.Fatal("fatal message");
                logger.Fatal("fatal", new Exception("ex"));
                logger.FatalFormat("fatal format {0}", 1);
            });
        }

        private class CapturingLogger : ILog
        {
            public List<string> Messages { get; } = new List<string>();

            public void Debug(object message) => Messages.Add($"Debug:{message}");
            public void Debug(object message, Exception exception) => Messages.Add($"Debug:{message}:{exception.Message}");
            public void DebugFormat(string format, params object[] args) => Messages.Add($"DebugFormat:{string.Format(format, args)}");
            public void Info(object message) => Messages.Add($"Info:{message}");
            public void Info(object message, Exception exception) => Messages.Add($"Info:{message}:{exception.Message}");
            public void InfoFormat(string format, params object[] args) => Messages.Add($"InfoFormat:{string.Format(format, args)}");
            public void Warn(object message) => Messages.Add($"Warn:{message}");
            public void Warn(object message, Exception exception) => Messages.Add($"Warn:{message}:{exception.Message}");
            public void WarnFormat(string format, params object[] args) => Messages.Add($"WarnFormat:{string.Format(format, args)}");
            public void Error(object message) => Messages.Add($"Error:{message}");
            public void Error(object message, Exception exception) => Messages.Add($"Error:{message}:{exception.Message}");
            public void ErrorFormat(string format, params object[] args) => Messages.Add($"ErrorFormat:{string.Format(format, args)}");
            public void Fatal(object message) => Messages.Add($"Fatal:{message}");
            public void Fatal(object message, Exception exception) => Messages.Add($"Fatal:{message}:{exception.Message}");
            public void FatalFormat(string format, params object[] args) => Messages.Add($"FatalFormat:{string.Format(format, args)}");
            public bool IsDebugEnabled => true;
        }

        [Test]
        public void EnyimLoggerWrapper_Delegates_To_Underlying_Logger()
        {
            var capturingLog = new CapturingLogger();
            var enyimLogger = new EnyimLoggerWrapper(capturingLog);

            enyimLogger.Info("test info");
            enyimLogger.DebugFormat("num: {0}", 123);
            enyimLogger.Error("fail", new InvalidOperationException("boom"));

            Assert.That(capturingLog.Messages, Does.Contain("Info:test info"));
            Assert.That(capturingLog.Messages, Does.Contain("DebugFormat:num: 123"));
            Assert.That(capturingLog.Messages, Does.Contain("Error:fail:boom"));
        }

        [Test]
        public void MemcachedClientCache_Constructor_Throws_On_Null_Hosts()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new MemcachedClientCache((IEnumerable<string>)null);
            });
        }

        [Test]
        public void MemcachedClientCache_Constructor_Throws_On_Null_IPEndpoints()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new MemcachedClientCache((IEnumerable<IPEndPoint>)null);
            });
        }

        [Test]
        public void MemcachedClientCache_Constructor_Throws_On_Null_Configuration()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new MemcachedClientCache((Enyim.Caching.Configuration.IMemcachedClientConfiguration)null);
            });
        }

        [Test]
        public void MemcachedClientCache_Constructor_Throws_On_Invalid_Host_Or_Port()
        {
            var ex1 = Assert.Throws<ArgumentException>(() =>
            {
                new MemcachedClientCache(new[] { "" });
            });
            Assert.That(ex1.Message, Does.Contain("is not a valid host IP Address"));

            var ex2 = Assert.Throws<ArgumentException>(() =>
            {
                new MemcachedClientCache(new[] { "127.0.0.1:invalidPort" });
            });
            Assert.That(ex2.Message, Does.Contain("contains an invalid port"));

            var ex3 = Assert.Throws<ArgumentException>(() =>
            {
                new MemcachedClientCache(new[] { "127.0.0.1:999999" });
            });
            Assert.That(ex3.Message, Does.Contain("contains an invalid port"));
        }

        [Test]
        public void MemcachedClientCache_Parses_IPv4_And_IPv6_Host_Formats()
        {
            // Valid formats should not throw ArgumentException regarding host format or port parsing
            Assert.DoesNotThrow(() =>
            {
                var client = new MemcachedClientCache(new[] { "127.0.0.1", "127.0.0.1:11211", "[::1]", "[::1]:11211" });
                Assert.That(client, Is.Not.Null);
            });
        }
    }
}
#endif
