using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase;

[TestFixtureOrmLite]
public class AliasedFieldUseCase(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Foo
    {
        [Alias("SOME_COLUMN_NAME")]
        public string Bar { get; set; }
    }

    public class Bar
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Baz { get; set; }
    }

    public class User
    {
        [AutoIncrement]
        [Alias("User ID")]
        public int Id { get; set; }

        [StringLength(100)]
        public string UserName { get; set; }
    }

    [Test]
    public void CanResolveAliasedFieldNameInAnonymousType()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<Foo>(true);

            db.Insert(new Foo { Bar = "some_value" });
            db.Insert(new Foo { Bar = "a totally different value" });
            db.Insert(new Foo { Bar = "whatever" });

            // the original classes property name is used to create the anonymous type
            List<Foo> foos = db.Where<Foo>(new { Bar = "some_value" });

            Assert.That(foos, Has.Count.EqualTo(1));

            // the aliased column name is used to create the anonymous type
            foos = db.Where<Foo>(new { SOME_COLUMN_NAME = "some_value" });

            Assert.That(foos, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void CanResolveAliasedFieldNameInJoinedTable()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Bar>();
            db.DropAndCreateTable<User>();

            db.Insert(new User { UserName = "Peter" });
            db.Insert(new Bar { Baz = "Peter" });

            var ev = db.From<Bar>()
                .Join<User>((x, y) => x.Id == y.Id);

            var foos = db.Select<Foo>(ev);

            ev = db.From<Bar>()
                .Join<User>((x, y) => x.Baz == y.UserName)
                .Where<User>(x => x.Id > 0);

            foos = db.Select<Foo>(ev);

            Assert.That(foos, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Can_Resolve_Aliased_FieldName_in_Joined_Table()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Foo>();
            db.DropAndCreateTable<User>();

            db.Insert(new User { UserName = "Peter" });
            db.Insert(new Foo { Bar = "Peter" });

            var q = db.From<Foo>()
                .Join<User>((x, y) => x.Bar == y.UserName)
                .Where<User>(x => x.Id > 0);

            var foo = db.Single(q);

            Assert.That(foo, Is.Not.Null);
            Assert.That(foo.Bar, Is.EqualTo("Peter"));
        }
    }
}