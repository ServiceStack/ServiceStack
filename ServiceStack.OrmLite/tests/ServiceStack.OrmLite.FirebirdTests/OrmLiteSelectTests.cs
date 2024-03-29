using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class OrmLiteSelectTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

			var rowIds = new List<int>(new[] { 1, 2, 3 });

			rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

			var row = db.SingleById<ModelWithFieldsOfDifferentTypes>(1);

			Assert.That(row.Id, Is.EqualTo(1));
		}

		[Test]
		public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);

			var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

			rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

			var row = db.SingleById<ModelWithOnlyStringFields>("id-1");

			Assert.That(row.Id, Is.EqualTo("id-1"));
		}

		[Test]
		public void Can_GetByIds_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

			var rowIds = new List<int>(new[] { 1, 2, 3 });

			rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

			var rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
			var dbRowIds = rows.ConvertAll(x => x.Id);

			Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
		}

		[Test]
		public void Can_GetByIds_string_from_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);

			var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

			rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

			var rows = db.SelectByIds<ModelWithOnlyStringFields>(rowIds);
			var dbRowIds = rows.ConvertAll(x => x.Id);

			Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
		}

		[Test]
		public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);

			var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

			rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

			var filterRow = ModelWithOnlyStringFields.Create("id-4");
			filterRow.AlbumName = "FilteredName";

			db.Insert(filterRow);

			var rows = db.Select<ModelWithOnlyStringFields>("AlbumName = @AlbumName", new { filterRow.AlbumName });
			var dbRowIds = rows.ConvertAll(x => x.Id);

			Assert.That(dbRowIds, Has.Count.EqualTo(1));
			Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
		}

		[Test]
		public void Can_select_scalar_value()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();

			n.Times(x => db.Insert(ModelWithIdAndName.Create(0)));

			var count = db.Scalar<int>("SELECT COUNT(*) FROM ModelWIN");

			Assert.That(count, Is.EqualTo(n));
		}

		[Test]
		public void Can_loop_each_string_from_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);

			var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

			rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

			var dbRowIds = new List<string>();
			foreach (var row in db.SelectLazy<ModelWithOnlyStringFields>())
			{
				dbRowIds.Add(row.Id);
			}

			Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
		}

		[Test]
		public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);

			var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

			rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

			var filterRow = ModelWithOnlyStringFields.Create("id-4");
			filterRow.AlbumName = "FilteredName";

			db.Insert(filterRow);

			var dbRowIds = new List<string>();
			var rows = db.SelectLazy<ModelWithOnlyStringFields>("AlbumName = @AlbumName", new { filterRow.AlbumName });
			foreach (var row in rows)
			{
				dbRowIds.Add(row.Id);
			}

			Assert.That(dbRowIds, Has.Count.EqualTo(1));
			Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
		}

		[Test]
		public void Can_GetFirstColumn()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();

			n.Times(x => db.Insert(ModelWithIdAndName.Create(0)));

			var ids = db.Column<int>("SELECT Id FROM ModelWIN");

			Assert.That(ids.Count, Is.EqualTo(n));
		}

		[Test]
		public void Can_GetFirstColumnDistinct()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();

			n.Times(x => db.Insert(ModelWithIdAndName.Create(0)));

			var ids = db.ColumnDistinct<int>("SELECT Id FROM ModelWIN");

			Assert.That(ids.Count, Is.EqualTo(n));
		}

		[Test]
		public void Can_GetLookup()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();

			n.Times(x => {
				var row = ModelWithIdAndName.Create(0);
				row.Name = x % 2 == 0 ? "OddGroup" : "EvenGroup";
				db.Insert(row);
			});

			var lookup = db.Lookup<string, int>("SELECT Name, Id FROM ModelWIN");

			Assert.That(lookup, Has.Count.EqualTo(2));
			Assert.That(lookup["OddGroup"], Has.Count.EqualTo(3));
			Assert.That(lookup["EvenGroup"], Has.Count.EqualTo(2));
		}

		[Test]
		public void Can_GetDictionary()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();
			n.Times(x => db.Insert(ModelWithIdAndName.Create(0)));

			var dictionary = db.Dictionary<int, string>("SELECT Id, Name FROM ModelWIN");

			Assert.That(dictionary, Has.Count.EqualTo(5));

			//Console.Write(dictionary.Dump());
		}

		[Test]
		public void Can_Select_subset_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

			var rowIds = new List<int>(new[] { 1, 2, 3 });

			rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

			var rows = db.Select<ModelWithIdAndName>("SELECT Id, Name FROM ModelWFDT");
			var dbRowIds = rows.ConvertAll(x => x.Id);

			Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
		}

		[Test]
		public void Can_Select_Into_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

			var rowIds = new List<int>(new[] { 1, 2, 3 });

			rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

			var rows = db.Select<ModelWithIdAndName>(typeof(ModelWithFieldsOfDifferentTypes));
			var dbRowIds = rows.ConvertAll(x => x.Id);

			Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
		}


		[Test]
		public void Can_Select_In_for_string_value()
		{
			const int n = 5;

			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();
				
			for(int i=1; i<=n; i++){
				ModelWithIdAndName m = new ModelWithIdAndName(){
					Name= 	"Name"+i.ToString()
				};
				db.Insert(m);
			}

			var selectInNames = new[] { "Name1", "Name2" };
			var rows = db.Select<ModelWithIdAndName>("Name IN ({0})".Fmt(selectInNames.SqlInParams()),
				new { values = selectInNames.SqlInValues() });
			Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));

			rows = db.Select<ModelWithIdAndName>("Name IN (@p1, @p2)", new { p1 = "Name1", p2 = "Name2" });
			Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));
		}
		
		//[Alias("RDB$DATABASE")]
		public class PocoFlag
		{
			public string Name { get; set; }
			public bool Flag { get; set; }
		}

		[Test]
		public void Can_populate_PocoFlag()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			var rows = db.Select<PocoFlag>("SELECT 1 as Flag FROM RDB$DATABASE");
			Assert.That(rows[0].Flag);
		}

		public class PocoFlagWithId
		{
			public int Id { get; set; }
			public bool Flag { get; set; }
		}

		[Test]
		public void Can_populate_PocoFlagWithId()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			var rows = db.Select<PocoFlagWithId>("SELECT 1 as Id, 1 as Flag FROM RDB$DATABASE");
			Assert.That(rows[0].Id, Is.EqualTo(1));
			Assert.That(rows[0].Flag);
		}

	}
}