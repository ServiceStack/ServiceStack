using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.TestsPerf.Scenarios;
using ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite;

namespace ServiceStack.OrmLite.TestsPerf
{
	class Program
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		const long DefaultIterations = 1000;

		static readonly List<long> BatchIterations = new List<long> { 100, 1000, 5000, 20000, /*100000, 250000, 1000000, 5000000*/ };

		static List<ScenarioBase> GetUseCases()
		{
			return new List<ScenarioBase>
			{
				new InsertModelWithFieldsOfDifferentTypesScenario(),
				new InsertSampleOrderLineScenario(),
				new SelectOneModelWithFieldsOfDifferentTypesScenario(),
				new SelectOneSampleOrderLineScenario(),
				new SelectManyModelWithFieldsOfDifferentTypesScenario(),
				new SelectManySampleOrderLineScenario(),
			};
		}

		static void Main(string[] args)
		{
			try
			{
				foreach (var configRun in OrmLiteScenrioConfig.ConfigRuns())
				{
					Console.WriteLine("\n\nStarting config run {0}...", configRun);
					if (args.Length == 1 && args[0] == "csv")
						RunBatch();
					else
						RunInteractive(args);
				}

				Console.ReadKey();
			}
			catch (Exception ex)
			{
				Log.Error("Error running perfs", ex);
				throw;
			}
		}

		private static void RunBatch()
		{
			Console.Write(";");
			var useCases = GetUseCases();

			useCases.ForEach(uc => Console.Write("{0};", uc.GetType().Name));
			Console.WriteLine();
			BatchIterations.ForEach(iterations => {
				Console.Write("{0};", iterations);
				useCases.ForEach(uc => {
					// warmup
					uc.Run();
					GC.Collect();
					Console.Write("{0};", Measure(uc.Run, iterations));
				});
				Console.WriteLine();
			});
		}

		private static void RunInteractive(string[] args)
		{
			long iterations = DefaultIterations;

			if (args.Length != 0)
				iterations = long.Parse(args[0]);

			Console.WriteLine("Running {0} iterations for each use case.", iterations);

			var useCases = GetUseCases();
			useCases.ForEach(uc => {
				// warmup
				uc.Run();
				GC.Collect();
				Console.WriteLine("{0}: {1}", uc.GetType().Name, Measure(uc.Run, iterations));
			});
		}

		private static decimal Measure(Action action, decimal iterations)
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
