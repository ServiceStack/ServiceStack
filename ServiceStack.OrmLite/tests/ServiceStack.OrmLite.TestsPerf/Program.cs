using System;
using System.Collections.Generic;
using System.Diagnostics;
using Northwind.Perf;
using ServiceStack.Logging;
using ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind;
using ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite;

namespace ServiceStack.OrmLite.TestsPerf
{
	class Program
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		const long DefaultIterations = 1;

		static readonly List<long> BatchIterations = new List<long> { 100, 1000, 5000, 20000, /*100000, 250000, 1000000, 5000000*/ };
		//static readonly List<long> BatchIterations = new List<long> { 1, 10, 100 };

		static List<DatabaseScenarioBase> GetUseCases()
		{
			return new List<DatabaseScenarioBase>
			{
				//new InsertModelWithFieldsOfDifferentTypesPerfScenario(),
				//new InsertSampleOrderLineScenario(),
				//new SelectOneModelWithFieldsOfDifferentTypesPerfScenario(),
				//new SelectOneSampleOrderLineScenario(),
				//new SelectManyModelWithFieldsOfDifferentTypesPerfScenario(),
				//new SelectManySampleOrderLineScenario(),
				new InsertNorthwindDataScenario(),
			};
		}

		static void Main(string[] args)
		{
			try
			{
				foreach (var configRun in OrmLiteScenrioConfig.DataProviderConfigRuns())
				{
					Console.WriteLine("\n\nStarting config run {0}...", configRun);
					if (args.Length == 1 && args[0] == "csv")
						RunBatch(configRun);
					else
						RunInteractive(configRun, args);
				}

				Console.ReadKey();
			}
			catch (Exception ex)
			{
				Log.Error("Error running perfs", ex);
				throw;
			}
		}

		private static void RunBatch(OrmLiteConfigRun configRun)
		{
			Console.Write(";");
			var useCases = GetUseCases();

			useCases.ForEach(uc => Console.Write("{0};", uc.GetType().Name));
			Console.WriteLine();
			BatchIterations.ForEach(iterations => {
				Console.Write("{0};", iterations);
				useCases.ForEach(uc => {

					configRun.Init(uc);

					// warmup
					uc.Run();
					GC.Collect();
					Console.Write("{0};", Measure(uc.Run, iterations));
				});
				Console.WriteLine();
			});
		}

		private static void RunInteractive(OrmLiteConfigRun configRun, string[] args)
		{
			long iterations = DefaultIterations;

			if (args.Length != 0)
				iterations = long.Parse(args[0]);

			Console.WriteLine("Running {0} iterations for each use case.", iterations);

			var useCases = GetUseCases();
			useCases.ForEach(uc => {

				configRun.Init(uc);

				// warmup
				uc.Run();
				GC.Collect();
               	
				var avgMs = Measure(uc.Run, iterations);
				Console.WriteLine("{0}: Avg: {1}ms", uc.GetType().Name, avgMs);
			});
		}

		private static decimal Measure(Action action, decimal iterations)
		{
			GC.Collect();
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var begin = stopWatch.ElapsedMilliseconds;

			for (var i = 0; i < iterations; i++)
			{
				action();
			}

			var end = stopWatch.ElapsedMilliseconds;

			return (end - begin) / iterations;
		}
	}
}
