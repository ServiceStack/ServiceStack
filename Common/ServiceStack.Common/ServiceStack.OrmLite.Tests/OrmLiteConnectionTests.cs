using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteConnectionTests 
		: OrmLiteTestBase
	{
		[Test]
		public void Can_create_connection()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
			}
		}

		[Test]
		public void Can_create_ReadOnly_connection()
		{
			using (var db = ConnectionString.OpenReadOnlyDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
			}
		}

		[Test]
		public void Can_create_table_with_ReadOnly_connection()
		{
			using (var db = ConnectionString.OpenReadOnlyDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				try
				{
					dbCmd.CreateTable<ModelWithIdAndName>(true);
					dbCmd.Insert(new ModelWithIdAndName(1));
				}
				catch (Exception ex)
				{
					Log(ex.Message);
					return;
				}
				Assert.Fail("Should not be able to create a table with a readonly connection");
			}
		}

		[Test]
		public void Can_open_two_ReadOnlyConnections_to_same_database()
		{
			var db = ConnectionString.OpenDbConnection();
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);
				dbCmd.Insert(new ModelWithIdAndName(1));
			}

			var dbReadOnly = "test.sqlite".OpenDbConnection();
			using (var dbReadOnlyCmd = dbReadOnly.CreateCommand())
			{
				dbReadOnlyCmd.Insert(new ModelWithIdAndName(2));
				var rows = dbReadOnlyCmd.Select<ModelWithIdAndName>();
				Assert.That(rows, Has.Count(2));
			}

			dbReadOnly.Dispose();
			db.Dispose();
		}

	}
}