using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class OrmLiteTransactionTests 
		: OrmLiteTestBase
	{
		[Test]
		public void Transaction_commit_persists_data_to_the_db()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithIdAndName>(true);
				db.DeleteAll<ModelWithIdAndName>();
				db.Insert(new ModelWithIdAndName(0));

				using (var dbTrans = db.BeginTransaction())
				{
					db.Insert(new ModelWithIdAndName(0));
					db.Insert(new ModelWithIdAndName(0));

					var rowsInTrans = db.Select<ModelWithIdAndName>();
					Assert.That(rowsInTrans, Has.Count.EqualTo(3));

					dbTrans.Commit();
				}

				var rows = db.Select<ModelWithIdAndName>();
				Assert.That(rows, Has.Count.EqualTo(3));
			}
		}

		[Test]
		public void Transaction_rollsback_if_not_committed()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithIdAndName>(true);
				db.DeleteAll<ModelWithIdAndName>();
				db.Insert(new ModelWithIdAndName(0));

				using (var dbTrans = db.BeginTransaction())
				{
					db.Insert(new ModelWithIdAndName(0));
					db.Insert(new ModelWithIdAndName(0));

					var rowsInTrans = db.Select<ModelWithIdAndName>();
					Assert.That(rowsInTrans, Has.Count.EqualTo(3));
				}

				var rows = db.Select<ModelWithIdAndName>();
				Assert.That(rows, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void Transaction_rollsback_transactions_to_different_tables()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithIdAndName>(true);
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				db.CreateTable<ModelWithOnlyStringFields>(true);
				db.DeleteAll<ModelWithIdAndName>();
				db.Insert(new ModelWithIdAndName(0));

				using (var dbTrans = db.BeginTransaction())
				{
					db.Insert(new ModelWithIdAndName(0));
					db.Insert(ModelWithFieldsOfDifferentTypes.Create(3));
					db.Insert(ModelWithOnlyStringFields.Create("id3"));

					Assert.That(db.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
					Assert.That(db.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
					Assert.That(db.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));
				}

				Assert.That(db.Select<ModelWithIdAndName>(), Has.Count.EqualTo(1));
				Assert.That(db.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(0));
				Assert.That(db.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(0));
			}
		}

		[Test]
		public void Transaction_commits_inserts_to_different_tables()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithIdAndName>(true);
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				db.CreateTable<ModelWithOnlyStringFields>(true);
				
				db.DeleteAll<ModelWithIdAndName>();
				db.Insert(new ModelWithIdAndName(0));

				using (var dbTrans = db.BeginTransaction())
				{
					db.Insert(new ModelWithIdAndName(0));
					db.Insert(ModelWithFieldsOfDifferentTypes.Create(3));
					db.Insert(ModelWithOnlyStringFields.Create("id3"));

					Assert.That(db.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
					Assert.That(db.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
					Assert.That(db.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));

					dbTrans.Commit();
				}

				Assert.That(db.Select<ModelWithIdAndName>(), Has.Count.EqualTo(2));
				Assert.That(db.Select<ModelWithFieldsOfDifferentTypes>(), Has.Count.EqualTo(1));
				Assert.That(db.Select<ModelWithOnlyStringFields>(), Has.Count.EqualTo(1));
			}
		}


	}
}