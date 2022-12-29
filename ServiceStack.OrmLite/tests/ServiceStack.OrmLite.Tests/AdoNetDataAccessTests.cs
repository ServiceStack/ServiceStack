using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    [Ignore("Required DB with MigrateSqlServerTypes table")]
    public class AdoNetDataAccessTests : OrmLiteProvidersTestBase
    {
        public AdoNetDataAccessTests(DialectContext context) : base(context) {}

        /*
        CREATE TABLE [dbo].[MigrateSqlServerTypes](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [SqlServerTime] [time](7) NULL,
            [OrmLiteTimeSpan] [bigint] NULL,
        )
        */

        public class MigrateSqlServerTypes
        {
            public int Id { get; set; }
            public TimeSpan OrmLiteTimeSpan { get; set; }
        }

        [Test]
        [Ignore("Needs review - MONOREPO")]
        public void Can_read_from_existing_database()
        {
            using var db = OpenDbConnection();
            var map = new Dictionary<int, TimeSpan>();

            using (var dbCmd = db.CreateCommand())
            {
                dbCmd.CommandText = "SELECT * FROM MigrateSqlServerTypes";

                using (var reader = dbCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var sqlTime = (TimeSpan)reader.GetValue(1);
                        map[id] = sqlTime;
                    }
                }
            }

            foreach (var entry in map)
            {
                db.Update(new MigrateSqlServerTypes { Id = entry.Key, OrmLiteTimeSpan = entry.Value });
            }

            db.Select<MigrateSqlServerTypes>().PrintDump();
        }
    }
}