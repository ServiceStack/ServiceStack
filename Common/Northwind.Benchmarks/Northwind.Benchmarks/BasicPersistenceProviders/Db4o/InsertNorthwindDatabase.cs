using System.IO;
using Northwind.Common.DataModel;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.OrmLite;

namespace Northwind.Benchmarks.BasicPersistenceProviders.Db4o
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
			var db4ODatabasePath = "~/App_Data/test.db4o".MapAbsolutePath();
			if (File.Exists(db4ODatabasePath))
			{
				File.Delete(db4ODatabasePath);
			}
			var db4OProviderManager = new Db4OFileProviderManager(db4ODatabasePath);
			var provider = db4OProviderManager.GetProvider();
			return provider;
		}
	}

}