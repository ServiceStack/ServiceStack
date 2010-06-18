using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteTransactionTests 
		: OrmLiteTestBase
	{
		[Test]
		public void Transaction_commit_persists_data_to_the_db()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);
				dbCmd.Insert(new ModelWithIdAndName(1));

				using (var dbTrans = dbCmd.BeginTransaction())
				{
					dbCmd.Insert(new ModelWithIdAndName(2));
					dbCmd.Insert(new ModelWithIdAndName(3));

					var rowsInTrans = dbCmd.Select<ModelWithIdAndName>();
					Assert.That(rowsInTrans, Has.Count.EqualTo(3));

					dbTrans.Commit();
				}

				var rows = dbCmd.Select<ModelWithIdAndName>();
				Assert.That(rows, Has.Count.EqualTo(3));
			}
		}

		[Test]
		public void Transaction_rollsback_if_not_committed()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);
				dbCmd.Insert(new ModelWithIdAndName(1));

				using (var dbTrans = dbCmd.BeginTransaction())
				{
					dbCmd.Insert(new ModelWithIdAndName(2));
					dbCmd.Insert(new ModelWithIdAndName(3));

					var rowsInTrans = dbCmd.Select<ModelWithIdAndName>();
					Assert.That(rowsInTrans, Has.Count.EqualTo(3));
				}

				var rows = dbCmd.Select<ModelWithIdAndName>();
				Assert.That(rows, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void Transaction_rollsback_transactions_to_different_tables()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				dbCmd.Insert(new ModelWithIdAndName(1));

				using (var dbTrans = dbCmd.BeginTransaction())
				{
					dbCmd.Insert(new ModelWithIdAndName(2));
					dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(3));
					dbCmd.Insert(ModelWithOnlyStringFields.Create("id3"));

					Assert.That(dbCmd.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
					Assert.That(dbCmd.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
					Assert.That(dbCmd.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));
				}

				Assert.That(dbCmd.Select<ModelWithIdAndName>(), Has.Count.EqualTo(1));
				Assert.That(dbCmd.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(0));
				Assert.That(dbCmd.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(0));
			}
		}

		[Test]
		public void Transaction_commits_inserts_to_different_tables()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				dbCmd.Insert(new ModelWithIdAndName(1));

				using (var dbTrans = dbCmd.BeginTransaction())
				{
					dbCmd.Insert(new ModelWithIdAndName(2));
					dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(3));
					dbCmd.Insert(ModelWithOnlyStringFields.Create("id3"));

					Assert.That(dbCmd.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
					Assert.That(dbCmd.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
					Assert.That(dbCmd.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));

					dbTrans.Commit();
				}

				Assert.That(dbCmd.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
				Assert.That(dbCmd.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
				Assert.That(dbCmd.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));
			}
		}


	}
}