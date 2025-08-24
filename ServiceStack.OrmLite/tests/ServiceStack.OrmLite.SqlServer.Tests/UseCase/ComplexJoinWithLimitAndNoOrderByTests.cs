using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    public class Bar : IHasGuidId
    {
        [PrimaryKey]
        [Alias("BarId")]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }
    }

    public class Foo : IHasIntId
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Alias("FKBarId")]
        [ForeignKey(typeof(Bar), ForeignKeyName = "fk_Foo_Bar")]
        public Guid BarId { get; set; }
    }

    internal class FooBarJoin
    {
        [BelongTo(typeof(Foo))]
        public int Id { get; set; }

        [BelongTo(typeof(Bar))]
        public Guid BarId { get; set; }

        [BelongTo(typeof(Bar))]
        public string Name { get; set; }
    }

    [TestFixture]
    public class ComplexJoinWithLimitAndNoOrderByTests : OrmLiteTestBase
    {
        private static int _foo1Id;
        private static int _foo2Id;
        private static int _foo3Id;
        private static Guid _bar1Id;
        private static Guid _bar2Id;
        private static Guid _bar3Id;

        private static void InitTables(IDbConnection db)
        {
            db.DropTable<Foo>();
            db.DropTable<Bar>();

            db.CreateTable<Bar>();
            db.CreateTable<Foo>();

            _bar1Id = new Guid("5bd67b84-bfdb-4057-9799-5e7a72a6eaa9");
            _bar2Id = new Guid("a8061d08-6816-4e1e-b3d7-1178abcefa0d");
            _bar3Id = new Guid("84BF769D-5BA9-4506-A7D2-5030E5595EDC");

            db.Insert(new Bar { Id = _bar1Id, Name = "Banana", });
            db.Insert(new Bar { Id = _bar2Id, Name = "Orange", });
            db.Insert(new Bar { Id = _bar3Id, Name = "Apple", });

            _foo1Id = (int)db.Insert(new Foo { BarId = _bar1Id, }, true);
            _foo2Id = (int)db.Insert(new Foo { BarId = _bar2Id, }, true);
            _foo3Id = (int)db.Insert(new Foo { BarId = _bar3Id, }, true);
        }

        [Test]
        public void ComplexJoin_with_JoinSqlBuilder_and_limit_and_no_orderby()
        {
            using var db = OpenDbConnection();
            InitTables(db);

            //JoinSqlBuilder is obsolete
            //var jn = new JoinSqlBuilder<FooBarJoin, Foo>()
            //    .Join<Foo, Bar>(dp => dp.BarId, p => p.Id)
            //    //.OrderBy<Foo>(f => f.Id)  // Test fails without an explicity OrderBy because auto-generated OrderBy uses join table (FooBarJoin) name
            //    .Limit(1, 2);

            var jn = db.From<Foo>()
                .Join<Foo, Bar>((f,b) => f.BarId == b.Id)
                .Limit(1, 2);

            var results = db.Select<FooBarJoin>(jn);
            db.GetLastSql().Print();

            results.PrintDump();

            var fooBarJoin = results.FirstOrDefault(x => x.BarId == _bar1Id);
            Assert.IsNull(fooBarJoin);
            fooBarJoin = results.First(x => x.BarId == _bar2Id);
            Assert.That(fooBarJoin.Id, Is.EqualTo(_foo2Id));
            fooBarJoin = results.First(x => x.BarId == _bar3Id);
            Assert.That(fooBarJoin.Id, Is.EqualTo(_foo3Id));
        }
    }
}