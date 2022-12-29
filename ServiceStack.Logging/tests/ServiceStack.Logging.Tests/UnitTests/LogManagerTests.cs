using NUnit.Framework;
using Rhino.Mocks;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class LogManagerTests : UnitTestBase
    {
        [Test]
        public void LogManager_DefaultTest()
        {
            ILog log = LogManager.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(LogManager.LogFactory as NullLogFactory);
            Assert.IsNotNull(log as NullDebugLogger);

            log = LogManager.GetLogger(GetType().Name);
            Assert.IsNotNull(log);
            Assert.IsNotNull(LogManager.LogFactory as NullLogFactory);
            Assert.IsNotNull(log as NullDebugLogger);
        }

        [Test]
        public void LogManager_InjectionTest()
        {
            ILogFactory factory = Mocks.CreateMock<ILogFactory>();
            Expect.Call(factory.GetLogger(GetType())).Return(Mocks.DynamicMock<ILog>());
            ReplayAll();

            LogManager.LogFactory = factory;
            ILog log = LogManager.GetLogger(GetType());

            Assert.IsNotNull(log);
            VerifyAll();
        }
    }
}