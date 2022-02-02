using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.MySql.Tests
{
	[TestFixture]
	public class OrmLiteSaveTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_Save_into_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				db.Save(row);
			}
		}

		[Test]
		public void Can_Save_and_select_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				db.Save(row);

				var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count.EqualTo(1));

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };
				var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

				db.SaveAll(newRows);

				var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count.EqualTo(newRows.Count));
			}
		}

		[Test]
		public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table_with_no_ids()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };
				var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(default(int)));

				db.SaveAll(newRows);

				var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count.EqualTo(newRows.Count));
			}
		}

		[Test]
		public void Can_Save_table_with_null_fields()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithIdAndName>(true);

				var row = ModelWithIdAndName.Create(1);
				row.Name = null;

				db.Save(row);

				var rows = db.Select<ModelWithIdAndName>();

				Assert.That(rows, Has.Count.EqualTo(1));

				ModelWithIdAndName.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_Save_TaskQueue_table()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<TaskQueue>(true);

				var row = TaskQueue.Create(1);

				db.Save(row);

				var rows = db.Select<TaskQueue>();

				Assert.That(rows, Has.Count.EqualTo(1));

				//Update the auto-increment id
				row.Id = rows[0].Id;

				TaskQueue.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_SaveAll_and_select_from_Movie_table()
		{
			using (var db = OpenDbConnection())
			{
				db.CreateTable<Movie>(true);

				var top5Movies = new List<Movie>
				{
					new Movie { Id = "tt0111161", Title = "The Shawshank Redemption", Rating = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, },
					new Movie { Id = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, },
					new Movie { Id = "tt1375666", Title = "Inception", Rating = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, },
					new Movie { Id = "tt0071562", Title = "The Godfather: Part II", Rating = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, },
					new Movie { Id = "tt0060196", Title = "The Good, the Bad and the Ugly", Rating = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, },
				};

				db.SaveAll(top5Movies);

				var rows = db.Select<Movie>();

				Assert.That(rows, Has.Count.EqualTo(top5Movies.Count));
			}
		}

	}
}