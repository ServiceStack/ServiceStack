using NUnit.Framework;
using ServiceStack.Logging.Serilog;

namespace ServiceStack.Logging.Tests.UnitTests
{
    [TestFixture]
    public class SerilogFactoryTests
    {
        [Test]
        public void Instantiation()
        {
            var factory = new SerilogFactory();
            var log = factory.GetLogger(GetType());
            Assert.IsNotNull(log);
            Assert.IsNotNull(log as SerilogLogger);
        }
    }
}
