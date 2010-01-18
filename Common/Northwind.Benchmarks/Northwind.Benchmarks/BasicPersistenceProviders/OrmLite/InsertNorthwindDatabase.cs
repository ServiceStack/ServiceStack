using Northwind.Common.DataModel;
using ServiceStack.DataAccess;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace Northwind.Benchmarks.BasicPersistenceProviders.OrmLite
{
	public class InsertNorthwindDatabase
		: BasicPersistenceProviderScenarioBase
	{
		public override void OnBeforeRun(IBasicPersistenceProvider provider)
		{
			var ormLiteProvider = (OrmLitePersistenceProvider) provider;
			using (var dbCmd = ormLiteProvider.Connection.CreateCommand())
			{
				dbCmd.CreateTables(true, NorthwindFactory.ModelTypes.ToArray());
			}
		}

		protected override IBasicPersistenceProvider CreateProvider()
		{
			OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;
			return new OrmLitePersistenceProvider(":memory:");
		}
	}
}