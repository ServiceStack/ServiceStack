using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Northwind.Benchmarks._Misc
{
	[TestFixture]
	public class StopWatchTests
	{
		[Test]
		public void Test_StopWatch_ElapsedTicks()
		{
			GC.Collect();

			var waitFor = TimeSpan.FromSeconds(10);

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var begin = stopWatch.ElapsedTicks;

			Thread.Sleep(waitFor);

			var end = stopWatch.ElapsedTicks;

			var avgTicks = (end - begin);
			var avgTimeSpan = TimeSpan.FromTicks(avgTicks);

			Console.WriteLine("Avg: {0} ticks / {1} ms / {2} secs / {3} secs",
							  avgTicks,
							  avgTicks / TimeSpan.TicksPerMillisecond,
							  avgTicks / TimeSpan.TicksPerSecond,
							  avgTimeSpan.TotalSeconds
				);
		}

		[Test]
		public void Test_StopWatch_ElapsedMilliseconds()
		{
			GC.Collect();

			var waitFor = TimeSpan.FromSeconds(10);

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var begin = stopWatch.ElapsedMilliseconds;

			Thread.Sleep(waitFor);

			var end = stopWatch.ElapsedMilliseconds;

			var avgMs = (end - begin);

			Console.WriteLine("Avg: {0} ticks / {1} ms / {2} secs",
							  avgMs * TimeSpan.TicksPerMillisecond,
							  avgMs,
							  avgMs / 1000
				);
		}

	}
}