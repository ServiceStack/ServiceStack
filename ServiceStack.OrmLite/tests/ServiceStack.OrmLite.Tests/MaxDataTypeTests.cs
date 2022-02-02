using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class MaxDataTypeTests : OrmLiteProvidersTestBase
    {
        public MaxDataTypeTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_and_select_max_values()
        {
            OrmLiteConfig.ThrowOnError = true;
            var isSqlServer = Dialect.AnySqlServer.HasFlag(Dialect);

            var model = new ModelWithFieldsOfDifferentTypes
            {
                Int = int.MaxValue,
                Long = long.MaxValue,
                Double = double.MaxValue,
                Decimal = !isSqlServer && !Dialect.Sqlite.HasFlag(Dialect) && !Dialect.AnyPostgreSql.HasFlag(Dialect) 
                    ? Decimal.MaxValue
                    : long.MaxValue,
                DateTime = !Dialect.AnyMySql.HasFlag(Dialect)  
                    ? DateTime.MaxValue
                    : DateTime.MaxValue.AddYears(-1),
                TimeSpan = TimeSpan.MaxValue,
            };

            using var db = OpenDbConnection();
            db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

            //db.GetLastSql().Print();

            var id = db.Insert(model, selectIdentity: true);

            var fromDb = db.SingleById<ModelWithFieldsOfDifferentTypes>(id);

            Assert.That(fromDb.Int, Is.EqualTo(model.Int));
            Assert.That(fromDb.Long, Is.EqualTo(model.Long));
            Assert.That(fromDb.Double, Is.EqualTo(model.Double));
            Assert.That(fromDb.DateTime, Is.EqualTo(model.DateTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(fromDb.TimeSpan, Is.EqualTo(model.TimeSpan));

            if (Dialect != Dialect.Sqlite)
            {
                Assert.That(fromDb.Decimal, Is.EqualTo(model.Decimal));
            }
            else
            {
                Assert.That(fromDb.Decimal / 10000, Is.EqualTo(model.Decimal / 10000).Within(1));
            }
        }
    }
}