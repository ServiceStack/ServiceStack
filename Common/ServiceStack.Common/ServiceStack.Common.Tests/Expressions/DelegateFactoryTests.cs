using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Common.Expressions;

namespace ServiceStack.Common.Tests.Expressions
{
	[TestFixture]
	public class DelegateFactoryTests
	{
		const string TextValue = "Hello, World!";
		private const int Times = 10000;

		[Test]
		public void String_test_with_func_call()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			Func<string> action = TextValue.ToUpper;

			for (var i=0; i < Times; i++)
			{
				var result = action();
			}

			stopWatch.Stop();
			Console.WriteLine("Delegate took: {0}ms", stopWatch.ElapsedMilliseconds);
		}

		[Test]
		public void String_test_with_direct_call()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			for (var i=0; i < Times; i++)
			{
				var result = TextValue.ToUpper();
			}

			stopWatch.Stop();
			Console.WriteLine("Delegate took: {0}ms", stopWatch.ElapsedMilliseconds);
		}

		[Test]
		public void String_test_with_reflection()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var methodInfo = typeof(string).GetMethod("ToUpper", new Type[] { });

			for (var i=0; i < Times; i++)
			{
				var result = methodInfo.Invoke(TextValue, new object[] { });
			}

			stopWatch.Stop();
			Console.WriteLine("Reflection took: {0}ms", stopWatch.ElapsedMilliseconds);
		}

		[Test]
		public void String_test_with_delegate()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var methodInfo = typeof (string).GetMethod("ToUpper", new Type[] {});
			var delMethod = DelegateFactory.Create(methodInfo);

			for (var i=0; i < Times; i++)
			{
				var result = delMethod(TextValue, new object[] { });
			}

			stopWatch.Stop();
			Console.WriteLine("Delegate took: {0}ms", stopWatch.ElapsedMilliseconds);
		}

	}
}