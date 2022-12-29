using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class OrmLiteSaveTests : OrmLiteProvidersTestBase
    {
        public OrmLiteSaveTests(DialectContext context) : base(context) {}

        [Test]
        public void Save_populates_AutoIncrementId()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PersonWithAutoId>();

            var row = new PersonWithAutoId
            {
                FirstName = "Jimi",
                LastName = "Hendrix",
                Age = 27
            };

            db.Save(row);

            Assert.That(row.Id, Is.Not.EqualTo(0));
        }

        [Test]
        public void Can_disable_AutoIncrement_field()
        {
            //Can't insert in identity column
            if ((Dialect & Dialect.AnySqlServer) == Dialect)
                return;

            using var db = OpenDbConnection();
            db.DropAndCreateTable<PersonWithAutoId>();

            typeof(PersonWithAutoId)
                .GetModelMetadata()
                .PrimaryKey.AutoIncrement = false;

            var row = new PersonWithAutoId
            {
                Id = 100,
                FirstName = "Jimi",
                LastName = "Hendrix",
                Age = 27
            };

            db.Insert(row);

            row = db.SingleById<PersonWithAutoId>(100);

            Assert.That(row.Id, Is.EqualTo(100));

            typeof(PersonWithAutoId)
                .GetModelMetadata()
                .PrimaryKey.AutoIncrement = true;
        }

        [Test]
        public void SaveAll_populates_AutoIncrementId()
        {
            using var db = OpenDbConnection();
            db.CreateTable<PersonWithAutoId>(overwrite: true);

            var rows = new[] {
                new PersonWithAutoId {
                    FirstName = "Jimi",
                    LastName = "Hendrix",
                    Age = 27
                },
                new PersonWithAutoId {
                    FirstName = "Kurt",
                    LastName = "Cobain",
                    Age = 27
                },
            };

            db.Save(rows);

            Assert.That(rows[0].Id, Is.Not.EqualTo(0));
            Assert.That(rows[1].Id, Is.Not.EqualTo(0));
            Assert.That(rows[0].Id, Is.Not.EqualTo(rows[1].Id));
        }

        [Test]
        public void Save_populates_NullableAutoIncrementId()
        {
            using var db = OpenDbConnection();
            db.CreateTable<PersonWithNullableAutoId>(overwrite: true);

            var row = new PersonWithNullableAutoId
            {
                FirstName = "Jimi",
                LastName = "Hendrix",
                Age = 27
            };

            db.Save(row);

            Assert.That(row.Id, Is.Not.EqualTo(0));
            Assert.That(row.Id, Is.Not.Null);
        }

        [Test]
        public void SaveAll_populates_NullableAutoIncrementId()
        {
            using var db = OpenDbConnection();
            db.CreateTable<PersonWithNullableAutoId>(overwrite: true);

            var rows = new[] {
                new PersonWithNullableAutoId {
                    FirstName = "Jimi",
                    LastName = "Hendrix",
                    Age = 27
                },
                new PersonWithNullableAutoId {
                    FirstName = "Kurt",
                    LastName = "Cobain",
                    Age = 27
                },
            };

            db.Save(rows);

            Assert.That(rows[0].Id, Is.Not.EqualTo(0));
            Assert.That(rows[0].Id, Is.Not.Null);
            Assert.That(rows[1].Id, Is.Not.EqualTo(0));
            Assert.That(rows[1].Id, Is.Not.Null);
            Assert.That(rows[0].Id, Is.Not.EqualTo(rows[1].Id));
        }

        [Test]
        public void Save_works_within_a_transaction()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PersonWithAutoId>();

            using var trans = db.OpenTransaction();
            var rows = new[] {
                new PersonWithAutoId {
                    FirstName = "Jimi",
                    LastName = "Hendrix",
                    Age = 27
                },
                new PersonWithAutoId {
                    FirstName = "Kurt",
                    LastName = "Cobain",
                    Age = 27
                },
            };

            db.Save(rows);

            Assert.That(rows[0].Id, Is.Not.EqualTo(0));
            Assert.That(rows[1].Id, Is.Not.EqualTo(0));
            Assert.That(rows[0].Id, Is.Not.EqualTo(rows[1].Id));

            trans.Commit();
        }


        [Test]
        public void Can_Save_into_ModelWithFieldsOfDifferentTypes_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

            var row = ModelWithFieldsOfDifferentTypes.Create(1);

            db.Save(row);
        }

        [Test]
        public void Can_SaveAll_As_An_Update_Into_Table_Without_Autoincrement_Key()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Rockstar>();
            db.SaveAll(Rockstar.Rockstars);

            var updatedRockstars = new[]
            {
                new Rockstar(6, "Jimi", "Hendrix", 27),
                new Rockstar(5, "Janis", "Joplin", 27),
                new Rockstar(4, "Jim", "Morrisson", 27),
                new Rockstar(3, "Kurt", "Cobain", 27),
                new Rockstar(2, "Elvis", "Presley", 42),
                new Rockstar(1, "Michael", "Jackson", 50),
            };
            db.SaveAll(updatedRockstars);
        }

        public class Rockstar
        {
            public static Rockstar[] Rockstars = {
                new Rockstar(1, "Jimi", "Hendrix", 27), 
                new Rockstar(2, "Janis", "Joplin", 27), 
                new Rockstar(3, "Jim", "Morrisson", 27), 
                new Rockstar(4, "Kurt", "Cobain", 27),              
                new Rockstar(5, "Elvis", "Presley", 42), 
                new Rockstar(6, "Michael", "Jackson", 50), 
            };

            public long RockstarId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }

            public Rockstar() { }
            public Rockstar(int id, string firstName, string lastName, int age)
            {
                RockstarId = id;
                FirstName = firstName;
                LastName = lastName;
                Age = age;
            }
        }

        [Test]
        public void Can_Save_and_select_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

            var row = ModelWithFieldsOfDifferentTypes.Create(1);

            db.Save(row);

            var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

            Assert.That(rows, Has.Count.EqualTo(1));

            ModelWithFieldsOfDifferentTypes.AssertIsEqual(rows[0], row);
        }

        [Test]
        public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

            var rowIds = new List<int> { 1, 2, 3, 4, 5 };
            var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

            db.SaveAll(newRows);

            var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

            Assert.That(rows, Has.Count.EqualTo(newRows.Count));
        }

        [Test]
        public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table_with_no_ids()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

            var rowIds = new List<int> { 1, 2, 3, 4, 5 };
            var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(default(int)));

            db.SaveAll(newRows);

            var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

            Assert.That(rows, Has.Count.EqualTo(newRows.Count));
        }

        [Test]
        public void Can_Save_table_with_null_fields()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdAndName>(true);

            var row = ModelWithIdAndName.Create(1);
            row.Name = null;

            db.Save(row);

            var rows = db.Select<ModelWithIdAndName>();

            Assert.That(rows, Has.Count.EqualTo(1));

            ModelWithIdAndName.AssertIsEqual(rows[0], row);
        }

        [Test]
        public void Can_Save_TaskQueue_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TaskQueue>(true);

            var row = TaskQueue.Create(1);

            db.Save(row);

            var rows = db.Select<TaskQueue>();

            Assert.That(rows, Has.Count.EqualTo(1));

            //Update the auto-increment id
            row.Id = rows[0].Id;

            TaskQueue.AssertIsEqual(rows[0], row);
        }

        [Test]
        public void Can_SaveAll_and_select_from_Movie_table()
        {
            using var db = OpenDbConnection();
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

        [Test]
        public void Can_Save_Update_Into_Table_With_Id_Only()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<ModelWithIdOnly>();

            db.Save(new ModelWithIdOnly(1));

            db.Save(new ModelWithIdOnly(1));

            db.Update(new ModelWithIdOnly(1));
        }
    }
}