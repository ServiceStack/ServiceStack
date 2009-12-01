using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteSelectTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int>(new[] { 1, 2, 3 });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var row = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(1);

				Assert.That(row.Id, Is.EqualTo(1));
			}
		}

		[Test]
		public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithOnlyStringFields.Create(x)));

				var row = dbCmd.GetById<ModelWithOnlyStringFields>("id-1");

				Assert.That(row.Id, Is.EqualTo("id-1"));
			}
		}

		[Test]
		public void Can_GetByIds_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int>(new[] { 1, 2, 3 });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var rows = dbCmd.GetByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
			}
		}

		[Test]
		public void Can_GetByIds_string_from_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithOnlyStringFields.Create(x)));

				var rows = dbCmd.GetByIds<ModelWithOnlyStringFields>(rowIds);
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
			}
		}

		[Test]
		public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				dbCmd.Insert(filterRow);

				var rows = dbCmd.Select<ModelWithOnlyStringFields>("AlbumName = {0}", filterRow.AlbumName);
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Has.Count(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

		[Test]
		public void Can_select_scalar_value()
		{
			const int n = 5;

			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);

				n.Times(x => dbCmd.Insert(ModelWithIdAndName.Create(x)));

				var count = dbCmd.GetScalar<int>("SELECT COUNT(*) FROM ModelWithIdAndName");

				Assert.That(count, Is.EqualTo(n));
			}
		}

		[Test]
		public void Can_loop_each_string_from_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithOnlyStringFields.Create(x)));

				var dbRowIds = new List<string>();
				foreach (var row in dbCmd.Each<ModelWithOnlyStringFields>())
				{
					dbRowIds.Add(row.Id);
				}

				Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
			}
		}

		[Test]
		public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				dbCmd.Insert(filterRow);

				var dbRowIds = new List<string>();
				var rows = dbCmd.Each<ModelWithOnlyStringFields>("AlbumName = {0}", filterRow.AlbumName);
				foreach (var row in rows)
				{
					dbRowIds.Add(row.Id);
				}

				Assert.That(dbRowIds, Has.Count(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

		[Test]
		public void Can_GetFirstColumn()
		{
			const int n = 5;

			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);

				n.Times(x => dbCmd.Insert(ModelWithIdAndName.Create(x)));

				var ids = dbCmd.GetFirstColumn<long>("SELECT Id FROM ModelWithIdAndName");

				Assert.That(ids.Count, Is.EqualTo(n));
			}
		}

		[Test]
		public void Can_GetFirstColumnDistinct()
		{
			const int n = 5;

			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);

				n.Times(x => dbCmd.Insert(ModelWithIdAndName.Create(x)));

				var ids = dbCmd.GetFirstColumnDistinct<int>("SELECT Id FROM ModelWithIdAndName");

				Assert.That(ids.Count, Is.EqualTo(n));
			}
		}

		[Test]
		public void Can_GetLookup()
		{
			const int n = 5;

			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(true);

				n.Times(x => {
					var row = ModelWithIdAndName.Create(x);
					row.Name = x % 2 == 0 ? "OddGroup" : "EvenGroup";
					dbCmd.Insert(row);
				});

				var lookup = dbCmd.GetLookup<string, int>("SELECT Name, Id FROM ModelWithIdAndName");

				Assert.That(lookup, Has.Count(2));
				Assert.That(lookup["OddGroup"], Has.Count(3));
				Assert.That(lookup["EvenGroup"], Has.Count(2));
			}
		}

		[Test]
		public void Can_Select_subset_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int>(new[] { 1, 2, 3 });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var rows = dbCmd.Select<ModelWithIdAndName>("SELECT Id, Name FROM ModelWithFieldsOfDifferentTypes");
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
			}
		}

		[Test]
		public void Can_SelectInto_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int>(new[] { 1, 2, 3 });

				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var rows = dbCmd.SelectInto<ModelWithFieldsOfDifferentTypes, ModelWithIdAndName>();
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
			}
		}

	}
}