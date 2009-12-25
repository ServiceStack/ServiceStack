using System;
using System.Collections.Generic;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class OrmLiteScenrioConfig
	{
		public static bool UseRelection { get; set; }

		private static readonly List<IPropertyInvoker> PropertyInvokers = new List<IPropertyInvoker> {
			ReflectionPropertyInvoker.Instance,
			ExpressionPropertyInvoker.Instance,
		};

		private static readonly Dictionary<IOrmLiteDialectProvider, List<string>> DataProviderAndConnectionStrings 
			= new Dictionary<IOrmLiteDialectProvider, List<string>> {
				{ 
					new SqliteOrmLiteDialectProvider(), 
					new List<string>
                  	{
                  		":memory:",
						//"~/App_Data/perf.sqlite".MapAbsolutePath(),
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
						PropertyInvoker = ExpressionPropertyInvoker.Instance,
					};
				}
			}

		}

		public static IEnumerable<OrmLiteConfigRun> PropertyInvokerConfigRuns()
		{
			foreach (var propertyInvoker in PropertyInvokers)
			{
				yield return new OrmLiteConfigRun {
					ConnectionString = ":memory:",
					DialectProvider = SqliteOrmLiteDialectProvider.Instance,
					PropertyInvoker = propertyInvoker
				};
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
			OrmLiteConfig.PropertyInvoker = this.PropertyInvoker;
			//PropertyInvoker.ConvertValueFn = OrmLiteConfig.DialectProvider.ConvertDbValue;

			dbScenarioBase.ConnectionString = this.ConnectionString;
		}

	}

}