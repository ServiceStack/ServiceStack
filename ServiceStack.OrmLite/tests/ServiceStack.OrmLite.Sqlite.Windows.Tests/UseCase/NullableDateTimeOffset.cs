using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    [TestFixture]
    public class NullableDateTimeOffset
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            //Inject your database provider here
            OrmLiteConfig.DialectProvider = new SqliteWindowsOrmLiteDialectProvider();
        }

        public class Record
        {
            [AutoIncrement]
            public long Id { get; set; }

            public DateTimeOffset CreatedDate { get; set; }

            public DateTimeOffset? ModifiedDate { get; set; }
        }

        [Test]
        public void Can_Insert_RecordWithNullable_DateTimeOffset()
        {
            var path = SqliteDb.FileConnection;
            if (File.Exists(path))
                File.Delete(path);
            
            using (IDbConnection db = path.OpenDbConnection())
            {
                db.CreateTable<OrmLite.Tests.UseCase.NullableDateTimeOffset.Record>(true);

                Assert.DoesNotThrow(() => db.Insert(new OrmLite.Tests.UseCase.NullableDateTimeOffset.Record() { Id = 1, CreatedDate = DateTime.Now }));
            }

            File.Delete(path);
        }

        [Test]
        public void Can_Update_RecordWithNullable_DateTimeOffset()
        {
            var path = SqliteDb.FileConnection;
            if (File.Exists(path))
                File.Delete(path);

            using (IDbConnection db = path.OpenDbConnection())
            {
                db.CreateTable<OrmLite.Tests.UseCase.NullableDateTimeOffset.Record>(true);

                db.Insert(new OrmLite.Tests.UseCase.NullableDateTimeOffset.Record() {Id = 1, CreatedDate = DateTime.Now });

                var record = db.LoadSingleById<OrmLite.Tests.UseCase.NullableDateTimeOffset.Record>(1);
                record.ModifiedDate = DateTimeOffset.Now;

                Assert.DoesNotThrow(() => db.Update(record));
            }

            File.Delete(path);
        }

        [Test]
        public void Can_UpdateWithNull_RecordWithNullable_DateTimeOffset()
        {
            var path = SqliteDb.FileConnection;
            if (File.Exists(path))
                File.Delete(path);

            using (IDbConnection db = path.OpenDbConnection())
            {
                db.CreateTable<OrmLite.Tests.UseCase.NullableDateTimeOffset.Record>(true);

                db.Insert(new OrmLite.Tests.UseCase.NullableDateTimeOffset.Record() { Id = 1, CreatedDate = DateTime.Now, ModifiedDate = DateTimeOffset.Now });

                var record = db.LoadSingleById<OrmLite.Tests.UseCase.NullableDateTimeOffset.Record>(1);
                record.ModifiedDate = null;

                Assert.DoesNotThrow(() => db.Update(record));
            }

            File.Delete(path);
        }
    }
}
