using System;
using System.Collections.Generic;
using System.Diagnostics;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Tests.Perf
{
	public class PerfTestBase
	{
		protected int DefaultIterations { get; set; }
		protected List<int> MultipleIterations { get; set; }

		public PerfTestBase()
		{
			this.DefaultIterations = 10000;
			this.MultipleIterations = new List<int> { 1000, 10000, 100000, 1000000 };
		}

		protected void CompareMultipleRuns(string run1Name, Action run1Action, string run2Name, Action run2Action)
		{
			WarmUp(run1Action, run2Action);
			foreach (var iteration in this.MultipleIterations)
			{
				Console.WriteLine("\n{0} times:", iteration);
				CompareRuns(iteration, run1Name, run1Action, run2Name, run2Action);
			}
		}

		protected void CompareRuns(string run1Name, Action run1Action, string run2Name, Action run2Action)
		{
			CompareRuns(DefaultIterations, run1Name, run1Action, run2Name, run2Action);
		}

		protected void CompareRuns(int iterations, string run1Name, Action run1Action, string run2Name, Action run2Action)
		{
			var run1 = RunAction(run1Action, DefaultIterations, run1Name);
			var run2 = RunAction(run2Action, DefaultIterations, run2Name);

			var runDiff = run1 - run2;
			var run1IsSlower = runDiff > 0;
			var slowerRun = run1IsSlower ? run1Name : run2Name;
			var fasterRun = run1IsSlower ? run2Name : run1Name;
			var runDiffTime = run1IsSlower ? runDiff : runDiff * -1;
			var runDiffAvg = run1IsSlower ? run1 / run2 : run2 / run1;

			Console.WriteLine("{0} was {1}ms or {2} times slower than {3}",
				slowerRun, runDiffTime, Math.Round(runDiffAvg, 2), fasterRun);
		}

		private static void WarmUp(params Action[] actions)
		{
			foreach (var action in actions)
			{
				action();
				GC.Collect();
			}
		}

		protected static decimal RunAction(Action action, int iterations)
		{
			return RunAction(action, iterations, null);
		}

		protected static decimal RunAction(Action action, int iterations, string actionName)
		{
			actionName = actionName ?? action.GetType().Name;
			var ticksTaken = Measure(action, iterations);
			Console.WriteLine("{0} took {1}ms", actionName, ticksTaken);

			return ticksTaken;
		}

		protected static decimal Measure(Action action, decimal iterations)
		{
			GC.Collect();
			var begin = Stopwatch.GetTimestamp();

			for (var i = 0; i < iterations; i++)
			{
				action();
			}

			var end = Stopwatch.GetTimestamp();

			return (end - begin) / iterations;
		}
	}
}