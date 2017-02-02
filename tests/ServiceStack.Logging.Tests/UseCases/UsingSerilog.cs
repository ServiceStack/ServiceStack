using NUnit.Framework;
using ServiceStack.Logging.Serilog;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UsingSerilog
    {
        [Test]
        public void SerilogUseCase()
        {
            LogManager.LogFactory = new SerilogFactory();
            var log = LogManager.GetLogger(GetType());

            log.Debug("Debug Event Log Entry.");
            log.Info("Info Event Log Entry.");
            log.Warn("Warning Event Log Entry.");
            log.Error("Error Event Log Entry.");
            log.Fatal("Fatal Event Log Entry.");
        }
    }
}
