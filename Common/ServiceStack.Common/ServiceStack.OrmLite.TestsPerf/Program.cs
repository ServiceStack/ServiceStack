using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ServiceStack.OrmLite.TestsPerf
{
	class Program
	{
		static void Main(string[] args)
		{
		}

		private static decimal Measure(Action action, decimal iterations)
		{
			GC.Collect();
			var begin = Stopwatch.GetTimestamp();

			for (int i = 0; i < iterations; i++)
			{
				action();
			}

			var end = Stopwatch.GetTimestamp();

			return (decimal)(end - begin) / iterations;
		}
	}
}
