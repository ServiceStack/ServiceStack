using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost.Tests.Support;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class PerfTests
	{
		private const int Times = 100000;

		[Test]
		public void RunAll()
		{
			With_Native();
			With_Reflection(); //Very slow
			With_Expressions();
			With_custom_func();
			With_TypeFactory();
			With_TypedArguments();
		}


		[Test]
		public void With_Native()
		{
			var request = new BasicRequest();

			Console.WriteLine("Native(): {0}", Measure(() => new BasicService().Execute(request), Times));
		}

		[Test]
		[Ignore("Slow to run")]
		public void With_Reflection()
		{
			var serviceController = new ServiceControllerReflection();

			serviceController.Register(() => new BasicService());
			var request = new BasicRequest();

			Console.WriteLine("With_Reflection(): {0}", Measure(() => serviceController.ExecuteReflection(request), Times));
		}

		[Test]
		public void With_ServiceStackFunq()
		{
			var serviceController = new ServiceController();

			serviceController.Register(() => new BasicService());
			var request = new BasicRequest();

			Console.WriteLine("With_TypedArguments(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		[Test]
		public void With_TypedArguments()
		{
			var serviceController = new ServiceController();

			serviceController.Register(() => new BasicService());
			var request = new BasicRequest();

			Console.WriteLine("With_TypedArguments(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		[Test]
		public void With_Expressions()
		{
			var requestType = typeof(BasicRequest);
			var serviceController = new ServiceController();

			serviceController.Register(requestType, typeof(BasicService));
			var request = new BasicRequest();

			Console.WriteLine("With_Expressions(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		[Test]
		public void With_custom_func()
		{
			var requestType = typeof(BasicRequest);
			var serviceController = new ServiceController();

			serviceController.Register(requestType, typeof(BasicService), type => new BasicService());

			var request = new BasicRequest();

			Console.WriteLine("With_custom_func(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		public class BasicServiceTypeFactory : ITypeFactory
		{
			public object CreateInstance(Type type)
			{
				return new BasicService();
			}
		}

		[Test]
		public void With_TypeFactory()
		{
			var requestType = typeof(BasicRequest);
			var serviceController = new ServiceController();

			serviceController.Register(requestType, typeof(BasicService), new BasicServiceTypeFactory());

			var request = new BasicRequest();

			Console.WriteLine("With_TypeFactory(): {0}", Measure(() => serviceController.Execute(request), Times));
		}


		private static long Measure(Action action, int iterations)
		{
			GC.Collect();
			var watch = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				action();
			}

			return watch.ElapsedTicks;
		}
	}
}
