using System;
using System.Diagnostics;
using NUnit.Framework;

namespace RedisWebServices.Tests
{
	[TestFixture]
	public class AdhocTests
	{
		[Test]
		public void Benchmarks()
		{
			var times = 1000000000;
			var noOfEvens = 0;

			var before = Stopwatch.GetTimestamp();
			for (var i = 0; i < times; i++)
			{
				noOfEvens += (i & 1) == 0 ? 1 : 0;
			}
			Console.WriteLine("TimeTaken: {0}", Stopwatch.GetTimestamp() - before);
			Console.WriteLine("No of Evens: " + noOfEvens);

			noOfEvens = 0;
			before = Stopwatch.GetTimestamp();
			for (var i = 0; i < times; i++)
			{
				noOfEvens += i % 2 == 0 ? 1 : 0;
			}
			Console.WriteLine("TimeTaken: {0}", Stopwatch.GetTimestamp() - before);
			Console.WriteLine("No of Evens: " + noOfEvens);

			/* Results:
				TimeTaken: 21310831
				No of Evens: 500000000
				TimeTaken: 21262060
				No of Evens: 500000000
			 */
		}
	}
}