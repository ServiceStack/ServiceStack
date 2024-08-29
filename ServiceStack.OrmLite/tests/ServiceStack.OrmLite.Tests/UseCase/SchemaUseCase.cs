using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase;

[TestFixtureOrmLite]
public class SchemaUseCase(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        using (var db = OpenDbConnection())
        {
            if(!Dialect.Sqlite.HasFlag(Dialect))
                db.CreateSchema<User>();
        }
    }

    [Alias("Users")]
    [Schema("Security")]
    public class User
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Index]
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    [Test]
    public void Can_Create_Tables_With_Schema()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<User>(true);
                
            Assert.That(db.TableExists<User>());
        }
    }

    [Test]
    public void Can_Perform_CRUD_Operations_On_Table_With_Schema()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<User>(true);

            db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
            db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });

            var user = new User {Id = 3, Name = "B", CreatedDate = DateTime.Now};
            user.Id = (int)db.Insert(user, selectIdentity:true);

            Assert.That(user.Id, Is.GreaterThan(0));

            var rowsB = db.Select<User>("Name = @name", new { name = "B" });
            Assert.That(rowsB, Has.Count.EqualTo(2));

            var rowIds = rowsB.ConvertAll(x => x.Id);
            Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

            rowsB.ForEach(x => db.Delete(x));

            rowsB = db.Select<User>("Name = @name", new { name = "B" });
            Assert.That(rowsB, Has.Count.EqualTo(0));

            var rowsLeft = db.Select<User>();
            Assert.That(rowsLeft, Has.Count.EqualTo(1));

            Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
        }
    }
}