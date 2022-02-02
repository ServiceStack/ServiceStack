using System;
using System.Collections.Generic;
using System.IO;
using Northwind.Perf;
using ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite;

namespace ServiceStack.OrmLite.TestsPerf.PerfTests
{
	public class OrmLitePerfTests
		: PerfTestBase
	{
		protected List<string> ConnectionStrings = new List<string>();

		public void WriteLog()
		{
			var fileName = string.Format("~/App_Data/PerfTests/{0:yyyy-MM-dd}.log", DateTime.Now).MapAbsolutePath();
			using (var writer = new StreamWriter(fileName, true))
			{
				writer.Write(SbLog);
			}
		}

		protected void RunMultipleTimes(ScenarioBase scenarioBase)
		{
			foreach (var configRun in OrmLiteScenrioConfig.DataProviderConfigRuns())
			{
				configRun.Init(scenarioBase);

				RunMultipleTimes(scenarioBase.Run, scenarioBase.GetType().Name);
			}
		}

	}

}