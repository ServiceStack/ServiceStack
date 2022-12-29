using System;

using System.Data;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests.Expressions
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class OrmLiteCountTests : OrmLiteProvidersTestBase
    {
        public OrmLiteCountTests(DialectContext context) : base(context) {}

        [Test]
        public void CanDoCountWithInterface()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<CountTestTable>(true);
                db.DeleteAll<CountTestTable>();

                db.Insert(new CountTestTable { Id = 1, StringValue = "Your string value" });

                var count = db.Scalar<CountTestTable, long>(e => Sql.Count(e.Id));

                Assert.That(count, Is.EqualTo(1));

                count = Count<CountTestTable>(db);

                Assert.That(count, Is.EqualTo(1));

                count = CountByColumn<CountTestTable>(db);

                Assert.That(count, Is.EqualTo(0));

            }
        }

        [Test]
        public void CanDoCountWithInterfaceAndPredicate()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<CountTestTable>(true);
                db.DeleteAll<CountTestTable>();
                db.Insert(new CountTestTable { Id = 1, StringValue = "Your string value" });

                Expression<Func<CountTestTable, bool>> exp = q => q.Id == 2;
                var count = Count(db, exp);
                Assert.That(count, Is.EqualTo(0));


                exp = q => q.Id == 1;
                count = Count(db, exp);
                Assert.That(count, Is.EqualTo(1));

                exp = q => q.CountColumn == null;
                count = Count(db, exp);
                Assert.That(count, Is.EqualTo(1));

                exp = q => q.CountColumn == null;
                count = CountByColumn(db, exp);
                Assert.That(count, Is.EqualTo(0));
            }
        }

        long Count<T>(IDbConnection db) where T : IHasId<int>
        {
            return db.Scalar<T, long>(e => Sql.Count(e.Id));
        }


        long CountByColumn<T>(IDbConnection db) where T : IHasCountColumn
        {
            return db.Scalar<T, long?>(e => Sql.Count(e.CountColumn)).Value;
        }


        int Count<T>(IDbConnection db, Expression<Func<T, bool>> predicate) where T : IHasId<int>
        {
            return db.Scalar<T, int>(e => Sql.Count(e.Id), predicate);
        }

        int CountByColumn<T>(IDbConnection db, Expression<Func<T, bool>> predicate) where T : IHasCountColumn
        {
            return db.Scalar<T, int?>(e => Sql.Count(e.CountColumn), predicate).Value;
        }

    }

    public interface IHasCountColumn
    {
        int? CountColumn { get; set; }
    }


    public class CountTestTable : IHasId<int>, IHasCountColumn
    {
        public CountTestTable() { }
        #region IHasId implementation
        public int Id { get; set; }
        [StringLength(40)]
        public string StringValue { get; set; }
        #endregion

        #region IHasCountColumn implementation
        public int? CountColumn { get; set; }
        #endregion
    }
}
