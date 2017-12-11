//Requires ServiceStack deps
#if FALSE 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging.Slack;

namespace ServiceStack.Logging.Tests.UnitTests
{
    public class TestAppHost : AppSelfHostBase
    {
        public static Action<SlackMessage> AssertCallback = (req) => { };

        public TestAppHost()
            : base("TestSlackHost", typeof(TestAppHost).Assembly)
        {

        }

        public class SlackLoggingData
        {
            public string Channel { get; set; }
            public string Text { get; set; }
            public string Username { get; set; }
            public string IconEmoji { get; set; }
        }

        public override void Configure(Container container)
        {

        }

        [Route("/testing")]
        public class SlackMessage : SlackLoggingData { }

        public class SlackTestService : Service
        {
            public void Any(SlackMessage request)
            {
                TestAppHost.AssertCallback(request);
            }
        }
    }

    [TestFixture]
    public class SlackLogFactoryTests
    {
        readonly AppSelfHostBase testAppHost = new TestAppHost();

        [OneTimeSetUp]
        public void SetUp()
        {
            testAppHost.Init().Start("http://localhost:22334/");
        }

        [Test]
        public void CanLogWithoutChannel()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing");
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo(null));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Error("This is a test");
            // Log is async HTTP request
            Thread.Sleep(10);
        }

        [Test]
        public void CanLogWithDefaultChannel()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing")
            {
                DefaultChannel = "Testing"
            };
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("Testing"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Error("This is a test");
            // Log is async HTTP request
            Thread.Sleep(10);
        }

        [Test]
        public void CanLogWithTypeSpecificChannels()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing", true)
            {
                DefaultChannel = "Testing",
                ErrorChannel = "ERROR",
                InfoChannel = "INFO",
                WarnChannel = "WARN",
                DebugChannel = "DEBUG",
                FatalChannel = "FATAL"
            };
            //ERROR
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("ERROR"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Error("This is a test");

            //FATAL
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("FATAL"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Fatal("This is a test");

            //WARN
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("WARN"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Warn("This is a test");

            //INFO
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("INFO"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Info("This is a test");

            //DEBUG
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Channel, Is.EqualTo("DEBUG"));
                Assert.That(message.Text, Is.EqualTo("This is a test"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Debug("This is a test");
            // Log is async HTTP request
            Thread.Sleep(10);
        }

        [Test]
        public void DebugNotUsedByDefault()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing");
            bool assertNeverFired = true;
            TestAppHost.AssertCallback = message => assertNeverFired = false;
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Debug("This is a test.");
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).DebugFormat("This is a test. {0}", 1);
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Debug("This is a test.", new ArgumentException());
            Thread.Sleep(200);
            Assert.That(assertNeverFired, Is.EqualTo(true));
            Assert.That(LogManager.LogFactory.GetLogger(typeof(TestAppHost)).IsDebugEnabled, Is.EqualTo(false));
        }

        [Test]
        public void ExceptionsAreLoggedWhenThrown()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing", true)
            {
                DefaultChannel = "Testing",
                ErrorChannel = "ERROR",
                InfoChannel = "INFO",
                WarnChannel = "WARN",
                DebugChannel = "DEBUG"
            };
            //ERROR
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Text, Is.EqualTo(
                    "This is a test\nMessage: Foo is null\r\nParameter name: Foo\nSource: \nTarget site: \nStack trace: \n"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Error("This is a test", new ArgumentNullException("Foo", "Foo is null"));
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Warn("This is a test", new ArgumentNullException("Foo", "Foo is null"));
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Info("This is a test", new ArgumentNullException("Foo", "Foo is null"));
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Fatal("This is a test", new ArgumentNullException("Foo", "Foo is null"));
            LogManager.LogFactory.GetLogger(typeof(TestAppHost)).Debug("This is a test", new ArgumentNullException("Foo", "Foo is null"));
            Thread.Sleep(10);
        }

        [Test]
        public void FormatLoggingCorrectlyLogs()
        {
            LogManager.LogFactory = new SlackLogFactory("http://localhost:22334/testing", true)
            {
                DefaultChannel = "Testing",
                ErrorChannel = "ERROR",
                InfoChannel = "INFO",
                WarnChannel = "WARN",
                DebugChannel = "DEBUG"
            };
            //ERROR
            TestAppHost.AssertCallback = message =>
            {
                Assert.That(message.Text, Is.EqualTo(
                    "Hello one, two, three, four"));
            };
            LogManager.LogFactory.GetLogger(typeof(TestAppHost))
                .ErrorFormat("Hello {0}, {1}, {2}, {3}", "one", "two", "three", "four");
            LogManager.LogFactory.GetLogger(typeof(TestAppHost))
                .FatalFormat("Hello {0}, {1}, {2}, {3}", "one", "two", "three", "four");
            LogManager.LogFactory.GetLogger(typeof(TestAppHost))
                .WarnFormat("Hello {0}, {1}, {2}, {3}", "one", "two", "three", "four");
            LogManager.LogFactory.GetLogger(typeof(TestAppHost))
                .InfoFormat("Hello {0}, {1}, {2}, {3}", "one", "two", "three", "four");
            LogManager.LogFactory.GetLogger(typeof(TestAppHost))
                .DebugFormat("Hello {0}, {1}, {2}, {3}", "one", "two", "three", "four");
            Thread.Sleep(10);
        }

        [Ignore("Call live slack team")]
        [Test]
        public void LogToSlackTest()
        {
            var url = new AppSettings().GetString("SlackUrl");
            LogManager.LogFactory = new SlackLogFactory(url, true);
            var logger = LogManager.LogFactory.GetLogger(typeof(TestAppHost));
            logger.Debug("Hello slack\nThis is a message from NUint tests.");

            LogManager.LogFactory = new SlackLogFactory(url, true)
            {
                BotUsername = "Log'O'Bot",
                IconEmoji = ":ghost:",
                FatalChannel = "logs-other",
            };

            var logger2 = LogManager.LogFactory.GetLogger(typeof(TestAppHost));
            logger2.Fatal("Hello slack\nThis is a message from NUint tests. 111");
        }
    }
}
#endif