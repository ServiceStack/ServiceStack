using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    [Alias("Bar Table")]
    public class BarSpace : IHasGuidId
    {
        [PrimaryKey]
        [Alias("BarSpace Id")]
        public Guid Id { get; set; }

        [Alias("BarSpace Name")]
        [Required]
        public string Name { get; set; }
    }

    [Alias("Foo Table")]
    public class FooSpace : IHasIntId
    {
        [AutoIncrement]
        [PrimaryKey]
        [Alias("FooSpace Id")]
        public int Id { get; set; }

        [Alias("FK BarId")]
        [ForeignKey(typeof(BarSpace), ForeignKeyName = "fk_FooSpace_BarSpace")]
        public Guid BarId { get; set; }
    }

    internal class FooSpaceBarSpaceJoin
    {
        [BelongTo(typeof(FooSpace))]
        [Alias("FooSpace Id")]
        public int Id { get; set; }

        [BelongTo(typeof(BarSpace))]
        [Alias("BarSpace Id")]
        public Guid BarId { get; set; }

        [BelongTo(typeof(BarSpace))]
        [Alias("BarSpace Name")]
        public string BarName { get; set; }
    }

    [TestFixture]
    public class ComplexJoinWithLimitAndSpacesInAliasesTests : OrmLiteTestBase
    {
        private static int _foo1Id;
        private static int _foo2Id;
        private static int _foo3Id;
        private static Guid _bar1Id;
        private static Guid _bar2Id;
        private static Guid _bar3Id;

        private static void InitTables(IDbConnection db)
        {
            db.DropTable<FooSpace>();
            db.DropTable<BarSpace>();

            db.CreateTable<BarSpace>();
            db.CreateTable<FooSpace>();

            _bar1Id = new Guid("5bd67b84-bfdb-4057-9799-5e7a72a6eaa9");
            _bar2Id = new Guid("a8061d08-6816-4e1e-b3d7-1178abcefa0d");
            _bar3Id = new Guid("84BF769D-5BA9-4506-A7D2-5030E5595EDC");

            db.Insert(new BarSpace { Id = _bar1Id, Name = "Banana", });
            db.Insert(new BarSpace { Id = _bar2Id, Name = "Orange", });
            db.Insert(new BarSpace { Id = _bar3Id, Name = "Apple", });

            _foo1Id = (int)db.Insert(new FooSpace { BarId = _bar1Id, }, true);
            _foo2Id = (int)db.Insert(new FooSpace { BarId = _bar2Id, }, true);
            _foo3Id = (int)db.Insert(new FooSpace { BarId = _bar3Id, }, true);
        }

        [Test]
        public void ComplexJoin_with_JoinSqlBuilder_and_limit_with_spaces_in_aliases()
        {
            using (var db = OpenDbConnection())
            {
                InitTables(db);

                //JoinSqlBuilder is obsolete
                //var jn = new JoinSqlBuilder<FooSpaceBarSpaceJoin, FooSpace>()
                //    .Join<FooSpace, BarSpace>(dp => dp.BarId, p => p.Id)
                //    .OrderBy<FooSpace>(f => f.Id)
                //    .Limit(1, 2);

                var jn = db.From<FooSpace>()
                    .Join<FooSpace, BarSpace>((f,b) => f.BarId == b.Id)
                    .OrderBy<FooSpace>(f => f.Id)
                    .Limit(1, 2);

                var results = db.Select<FooSpaceBarSpaceJoin>(jn);
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
}