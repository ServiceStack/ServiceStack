using NUnit.Framework;
using ServiceStack.Logging.Log4Net;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class Log4NetFactoryTests
    {
        [Test]
        public void Log4NetFactoryTest()
        {
            Log4NetFactory factory = new Log4NetFactory();
            ILog log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as Log4NetLogger);

            factory = new Log4NetFactory(true);
            log = factory.GetLogger(GetType().Name);
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as Log4NetLogger);
        }
    }
}