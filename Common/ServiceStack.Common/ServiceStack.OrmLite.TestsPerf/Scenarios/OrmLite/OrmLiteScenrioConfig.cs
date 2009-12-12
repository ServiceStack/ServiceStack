using System.Collections.Generic;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class OrmLiteScenrioConfig
	{
		public static bool UseRelection { get; set; }

		private static readonly List<string> ConnectionStrings = new List<string> {
			":memory:",
            //"~/App_Data/perf.sqlite".MapAbsolutePath(),
		};

		private static readonly List<IPropertyInvoker> PropertyInvokers = new List<IPropertyInvoker> {
			ReflectionPropertyInvoker.Instance,
			ExpressionPropertyInvoker.Instance,
		};

		public static IEnumerable<string> ConfigRuns()
		{
			OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();

			foreach (var connectionString in ConnectionStrings)
			{
				foreach (var propertyInvoker in PropertyInvokers)
				{
					propertyInvoker.ConvertValueFn = OrmLiteConfig.DialectProvider.ConvertDbValue;

					OrmLiteConfig.ClearCache();
					OrmLiteConfig.PropertyInvoker = propertyInvoker;
					DatabaseScenarioBase.ConnectionString = connectionString;

					yield return string.Format("{0} => {1}",
						propertyInvoker.GetType().Name, connectionString);
				}
			}
		}

	}

}