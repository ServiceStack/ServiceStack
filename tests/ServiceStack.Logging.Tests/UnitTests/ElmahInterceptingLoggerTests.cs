using System;
using System.Web;
using NUnit.Framework;
using Rhino.Mocks;
using ServiceStack.Logging.Elmah;
using ServiceStack.Logging.Log4Net;

namespace ServiceStack.Logging.Tests.UnitTests
{
	[TestFixture]
	public class ElmahInterceptingLoggerTests
	{
		[Test]
		public void ElmahInterceptingLoggerTest()
		{
			var wrappedLogger = MockRepository.GenerateStub<ILog>();
			ILog log = new ElmahInterceptingLogger(wrappedLogger, new HttpApplication());
			Assert.IsNotNull(log);
		}

		[Test]
		public void ElmahInterceptingLogger_LoggingTest()
		{
			string message = "Error Message";
			Exception ex = new Exception();
			string messageFormat = "Message Format: message: {0}, exception: {1}";

			var wrappedLogger = MockRepository.GenerateStub<ILog>();
            ILog log = new ElmahInterceptingLogger(wrappedLogger, new HttpApplication());
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
		
		[Test]
		public void Does_log_Log4Net_errors_to_Elmah()
		{
			var log4NetFactory = new Log4NetFactory(true);
			LogManager.LogFactory = new ElmahLogFactory(log4NetFactory, new HttpApplication());

			var log = LogManager.GetLogger(typeof(ElmahLogFactoryTests));
			log.Error("Error log test");

			LogManager.LogFactory = null;
		}
	}
}
