using System.Collections.Generic;
using Northwind.Perf;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class OrmLiteScenrioConfig
	{
		private static readonly Dictionary<IOrmLiteDialectProvider, List<string>> DataProviderAndConnectionStrings 
			= new Dictionary<IOrmLiteDialectProvider, List<string>> {
				{ 
					new SqliteOrmLiteDialectProvider(), 
					new List<string>
                  	{
                  		":memory:",
						"~/App_Data/perf.sqlite".MapAbsolutePath(),
                  	} 
				}
			};

		public static IEnumerable<OrmLiteConfigRun> DataProviderConfigRuns()
		{
			foreach (var providerConnectionString in DataProviderAndConnectionStrings)
			{
				var dialectProvider = providerConnectionString.Key;
				var connectionStrings = providerConnectionString.Value;

				foreach (var connectionString in connectionStrings)
				{
					yield return new OrmLiteConfigRun {
						DialectProvider = dialectProvider,
						ConnectionString = connectionString,
					};
				}
			}
		}
	}

	public class OrmLiteConfigRun
	{
		public IOrmLiteDialectProvider DialectProvider { get; set; }

		public string ConnectionString { get; set; }

		public IPropertyInvoker PropertyInvoker { get; set; }

		public void Init(ScenarioBase scenarioBase)
		{
			var dbScenarioBase = scenarioBase as DatabaseScenarioBase;
			if (dbScenarioBase == null) return;

			OrmLiteConfig.DialectProvider = this.DialectProvider;

			OrmLiteConfig.ClearCache();
			//PropertyInvoker.ConvertValueFn = OrmLiteConfig.DialectProvider.ConvertDbValue;

			dbScenarioBase.ConnectionString = this.ConnectionString;
		}

	}

}