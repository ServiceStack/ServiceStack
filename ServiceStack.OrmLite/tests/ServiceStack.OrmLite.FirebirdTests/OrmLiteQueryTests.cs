using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class OrmLiteQueryTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int>(new[] { 1, 2, 3 });

				rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var row = db.SingleById<ModelWithFieldsOfDifferentTypes>(1);

				Assert.That(row.Id, Is.EqualTo(1));
			}
		}

		[Test]
		public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var row = db.SingleById<ModelWithOnlyStringFields>("id-1");

				Assert.That(row.Id, Is.EqualTo("id-1"));
			}
		}
		
		[Test]
		public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				db.Insert(filterRow);

				var rows = db.Where<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				var dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

				rows = db.Where<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

				var queryByExample = new ModelWithOnlyStringFields { AlbumName = filterRow.AlbumName };
				rows = db.SelectNonDefaults(queryByExample);
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

				rows = db.Select<ModelWithOnlyStringFields>(
					"SELECT * FROM ModelWithOnlyStringFields WHERE AlbumName = @AlbumName", new { filterRow.AlbumName });
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

		[Test]
		public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				db.Insert(filterRow);

				var dbRowIds = new List<string>();
				var rows = db.WhereLazy<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				foreach (var row in rows)
				{
					dbRowIds.Add(row.Id);
				}

				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

        [Test]
        public void Can_select_from_ModelWithReservedWords_table()
        {
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
            db.CreateTable<ModelWithReservedWords>(true);

            var rowIds = new List<int>(new[] { 1, 2, 3 });

            rowIds.ForEach(x => db.Insert(new ModelWithReservedWords() { Group = 1, User = 1, Name = $"Test{x}" }));

            var list = db.Select<ModelWithReservedWords>();

            Assert.That(list.Count, Is.EqualTo(3));
        }

    }
}