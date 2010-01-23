using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteConnectionTests 
		: OrmLiteTestBase
	{
		[Test][Ignore]
		public void Can_create_connection_to_blank_database()
		{
			var connString = @"C:\Projects\PoToPe\trunk\website\src\Mflow.Intranet\Mflow.Intranet\App_Data\Exports\2009-10\MonthlySnapshot.mdf";
			using (var db = connString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
			}
		}

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
			var db = "test.sqlite".OpenDbConnection();
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