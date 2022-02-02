using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Models;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    [IgnoreDialect(Dialect.MySql, MySqlDb.V5_5, "You cannot set the default for a date column to be the value of a function such as NOW() or CURRENT_DATE. The exception is that you can specify CURRENT_TIMESTAMP as the default for a TIMESTAMP column")]
    public class DefaultValueTests : OrmLiteProvidersTestBase
    {
        public DefaultValueTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_table_with_DefaultValues()
        {
            using var db = OpenDbConnection();
            var row = CreateAndInitialize(db);

            var expectedDate = !Dialect.HasFlag(Dialect.Firebird)
                ? DateTime.UtcNow.Date
                : DateTime.Now.Date; 

            Assert.That(row.CreatedDateUtc, Is.GreaterThan(expectedDate));
            Assert.That(row.NCreatedDateUtc, Is.GreaterThan(expectedDate));
        }

        private DefaultValues CreateAndInitialize(IDbConnection db, int count = 1)
        {
            db.DropAndCreateTable<DefaultValues>();
            db.GetLastSql().Print();

            DefaultValues firstRow = null;
            for (var i = 1; i <= count; i++)
            {
                var defaultValues = new DefaultValues { Id = i };
                db.Insert(defaultValues);

                var row = db.SingleById<DefaultValues>(1);
                row.PrintDump();
                Assert.That(row.DefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultIntNoDefault, Is.EqualTo(0));
                Assert.That(row.NDefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.NDefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.DefaultString, Is.EqualTo("String"));

                if (firstRow == null)
                    firstRow = row;
            }

            return firstRow;
        }

        [Test]
        public void Can_use_ToUpdateStatement_to_generate_inline_SQL()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            var row = db.SingleById<DefaultValues>(1);
            row.DefaultIntNoDefault = 42;

            var sql = db.ToUpdateStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            row = db.SingleById<DefaultValues>(1);

            Assert.That(row.DefaultInt, Is.EqualTo(1));
            Assert.That(row.DefaultIntNoDefault, Is.EqualTo(42));
            Assert.That(row.NDefaultInt, Is.EqualTo(1));
            Assert.That(row.DefaultDouble, Is.EqualTo(1.1).Within(.1d));
            Assert.That(row.NDefaultDouble, Is.EqualTo(1.1).Within(.1d));
            Assert.That(row.DefaultString, Is.EqualTo("String"));
        }

        [Test]
        public void Can_filter_update_method1_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db, 2);

            ResetUpdateDate(db);
            db.Update(cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider),
                new DefaultValues { Id = 1, DefaultInt = 45, CreatedDateUtc = DateTime.Now }, 
                new DefaultValues { Id = 2, DefaultInt = 72, CreatedDateUtc = DateTime.Now });
            VerifyUpdateDate(db);
            VerifyUpdateDate(db, id: 2);
        }

        private static void ResetUpdateDate(IDbConnection db)
        {
            var updateTime = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            db.Update<DefaultValues>(new { UpdatedDateUtc = updateTime }, p => p.Id == 1);
        }

        private void VerifyUpdateDate(IDbConnection db, int id = 1)
        {
            var row = db.SingleById<DefaultValues>(id);
            row.PrintDump();

            if (!Dialect.HasFlag(Dialect.AnyMySql)) //not returning UTC
                Assert.That(row.UpdatedDateUtc, Is.GreaterThan(DateTime.UtcNow - TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void Can_filter_update_method2_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            db.Update(new DefaultValues { Id = 1, DefaultInt = 2342, CreatedDateUtc = DateTime.Now }, p => p.Id == 1,
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_update_method3_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            var row = db.SingleById<DefaultValues>(1);
            row.DefaultInt = 3245;
            row.DefaultDouble = 978.423;
            db.Update(row, cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_update_method4_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            db.Update<DefaultValues>(new { DefaultInt = 765 }, p => p.Id == 1,
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_updateAll_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db, 2);

            ResetUpdateDate(db);
            db.UpdateAll(new [] { new DefaultValues { Id = 1, DefaultInt = 45, CreatedDateUtc = DateTime.Now },
                    new DefaultValues { Id = 2, DefaultInt = 72, CreatedDateUtc = DateTime.Now } },
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
            VerifyUpdateDate(db, id: 2);
        }

        [Test]
        public void Can_filter_updateOnly_method1_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            db.UpdateOnly(() => new DefaultValues {DefaultInt = 345}, p => p.Id == 1,
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_updateOnly_method2_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            db.UpdateOnly(() => new DefaultValues { DefaultInt = 345 }, db.From<DefaultValues>().Where(p => p.Id == 1),
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_updateOnly_method3_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db);

            ResetUpdateDate(db);
            var row = db.SingleById<DefaultValues>(1);
            row.DefaultDouble = 978.423;
            db.UpdateOnlyFields(row, db.From<DefaultValues>().Update(p => p.DefaultDouble),
                cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
            VerifyUpdateDate(db);
        }

        [Test]
        public void Can_filter_updateOnly_method4_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<DefaultValues>(1);
                row.DefaultDouble = 978.423;
                db.UpdateOnlyFields(row, p => p.DefaultDouble, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method5_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<DefaultValues>(1);
                row.DefaultDouble = 978.423;
                db.UpdateOnlyFields(row, new[] { nameof(DefaultValues.DefaultDouble) }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateAdd_expression_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);

                var count = db.UpdateAdd(() => new DefaultValues { DefaultInt = 5, DefaultDouble = 7.2 }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));

                Assert.That(count, Is.EqualTo(1));
                var row = db.SingleById<DefaultValues>(1);
                Assert.That(row.DefaultInt, Is.EqualTo(6));
                Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateAdd_SqlExpression_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);

                var where = db.From<DefaultValues>().Where(p => p.Id == 1);
                var count = db.UpdateAdd(() => new DefaultValues { DefaultInt = 5, DefaultDouble = 7.2 }, where,
                    cmd => cmd.SetUpdateDate<DefaultValues>(nameof(DefaultValues.UpdatedDateUtc), DialectProvider));

                Assert.That(count, Is.EqualTo(1));
                var row = db.SingleById<DefaultValues>(1);
                Assert.That(row.DefaultInt, Is.EqualTo(6));
                Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Does_use_defaults_for_missing_values()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithDefaults>();

                db.Insert(new ModelWithDefaults { DefaultInt = 10 });

                var row = db.Select<ModelWithDefaults>().FirstOrDefault();

                Assert.That(row.Id, Is.GreaterThan(0));
                Assert.That(row.DefaultInt, Is.EqualTo(10));
                Assert.That(row.DefaultString, Is.EqualTo("String"));
            }
        }

        [Test]
        public void Does_only_use_defaults_for_all_default_properties()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithDefaults>();

                db.InsertUsingDefaults(
                    new ModelWithDefaults { Name = "foo", DefaultInt = 10 },
                    new ModelWithDefaults { Name = "bar", DefaultString = "qux" });

                var rows = db.Select<ModelWithDefaults>();
                rows.PrintDump();

                Assert.That(rows.All(x => x.Id > 0));
                Assert.That(rows.All(x => x.DefaultInt == 1));
                Assert.That(rows.All(x => x.DefaultString == "String"));
            }
        }
    }
    
    /// <summary>
    /// MySql 5.5 only allows a single timestamp default column
    /// </summary>
    [TestFixtureOrmLiteDialects(Dialect.MySql, MySqlDb.V5_5)]
    public class MySqlDefaultValueTests : OrmLiteProvidersTestBase
    {
        public MySqlDefaultValueTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_table_with_DefaultValues()
        {
            using (var db = OpenDbConnection())
            {
                var row = CreateAndInitialize(db);

                var expectedDate = DateTime.Now.Date; //MySql CURRENT_TIMESTAMP == LOCAL_TIME

                Assert.That(row.UpdatedDateUtc, Is.GreaterThan(expectedDate));
            }
        }

        private MySqlDefaultValues CreateAndInitialize(IDbConnection db, int count = 1)
        {
            db.DropAndCreateTable<MySqlDefaultValues>();
            db.GetLastSql().Print();

            MySqlDefaultValues firstRow = null;
            for (var i = 1; i <= count; i++)
            {
                var defaultValues = new MySqlDefaultValues { Id = i };
                db.Insert(defaultValues);

                var row = db.SingleById<MySqlDefaultValues>(1);
                row.PrintDump();
                Assert.That(row.DefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultIntNoDefault, Is.EqualTo(0));
                Assert.That(row.NDefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.NDefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.DefaultString, Is.EqualTo("String"));

                if (firstRow == null)
                    firstRow = row;
            }

            return firstRow;
        }

        [Test]
        public void Can_use_ToUpdateStatement_to_generate_inline_SQL()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                var row = db.SingleById<MySqlDefaultValues>(1);
                row.DefaultIntNoDefault = 42;

                var sql = db.ToUpdateStatement(row);
                sql.Print();
                db.ExecuteSql(sql);

                row = db.SingleById<MySqlDefaultValues>(1);

                Assert.That(row.DefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultIntNoDefault, Is.EqualTo(42));
                Assert.That(row.NDefaultInt, Is.EqualTo(1));
                Assert.That(row.DefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.NDefaultDouble, Is.EqualTo(1.1).Within(.1d));
                Assert.That(row.DefaultString, Is.EqualTo("String"));
            }
        }

        [Test]
        public void Can_filter_update_method1_to_insert_date()
        {
            using var db = OpenDbConnection();
            CreateAndInitialize(db, 2);

            ResetUpdateDate(db);
            db.Update(cmd => 
                    cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider),
                new MySqlDefaultValues { Id = 1, DefaultInt = 45 }, 
                new MySqlDefaultValues { Id = 2, DefaultInt = 72 });
            VerifyUpdateDate(db);
            VerifyUpdateDate(db, id: 2);
        }

        private static void ResetUpdateDate(IDbConnection db)
        {
            var updateTime = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            db.Update<MySqlDefaultValues>(new { UpdatedDateUtc = updateTime }, p => p.Id == 1);
        }

        private void VerifyUpdateDate(IDbConnection db, int id = 1)
        {
            var row = db.SingleById<MySqlDefaultValues>(id);
            row.PrintDump();

            if (!Dialect.HasFlag(Dialect.AnyMySql)) //not returning UTC
                Assert.That(row.UpdatedDateUtc, Is.GreaterThan(DateTime.UtcNow - TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void Can_filter_update_method2_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                db.Update(new MySqlDefaultValues { Id = 1, DefaultInt = 2342 }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_update_method3_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<MySqlDefaultValues>(1);
                row.DefaultInt = 3245;
                row.DefaultDouble = 978.423;
                db.Update(row, cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_update_method4_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                db.Update<MySqlDefaultValues>(new { DefaultInt = 765 }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateAll_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db, 2);

                ResetUpdateDate(db);
                db.UpdateAll(new [] { new MySqlDefaultValues { Id = 1, DefaultInt = 45 },
                                      new MySqlDefaultValues { Id = 2, DefaultInt = 72 } },
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
                VerifyUpdateDate(db, id: 2);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method1_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                db.UpdateOnly(() => new MySqlDefaultValues {DefaultInt = 345}, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method2_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                db.UpdateOnly(() => new MySqlDefaultValues { DefaultInt = 345 }, db.From<MySqlDefaultValues>().Where(p => p.Id == 1),
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method3_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<MySqlDefaultValues>(1);
                row.DefaultDouble = 978.423;
                db.UpdateOnlyFields(row, db.From<MySqlDefaultValues>().Update(p => p.DefaultDouble),
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method4_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<MySqlDefaultValues>(1);
                row.DefaultDouble = 978.423;
                db.UpdateOnlyFields(row, p => p.DefaultDouble, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateOnly_method5_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);
                var row = db.SingleById<MySqlDefaultValues>(1);
                row.DefaultDouble = 978.423;
                db.UpdateOnlyFields(row, new[] { nameof(MySqlDefaultValues.DefaultDouble) }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateAdd_expression_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);

                var count = db.UpdateAdd(() => new MySqlDefaultValues { DefaultInt = 5, DefaultDouble = 7.2 }, p => p.Id == 1,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));

                Assert.That(count, Is.EqualTo(1));
                var row = db.SingleById<MySqlDefaultValues>(1);
                Assert.That(row.DefaultInt, Is.EqualTo(6));
                Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Can_filter_updateAdd_SqlExpression_to_insert_date()
        {
            using (var db = OpenDbConnection())
            {
                CreateAndInitialize(db);

                ResetUpdateDate(db);

                var where = db.From<MySqlDefaultValues>().Where(p => p.Id == 1);
                var count = db.UpdateAdd(() => new MySqlDefaultValues { DefaultInt = 5, DefaultDouble = 7.2 }, where,
                    cmd => cmd.SetUpdateDate<MySqlDefaultValues>(nameof(MySqlDefaultValues.UpdatedDateUtc), DialectProvider));

                Assert.That(count, Is.EqualTo(1));
                var row = db.SingleById<MySqlDefaultValues>(1);
                Assert.That(row.DefaultInt, Is.EqualTo(6));
                Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
                VerifyUpdateDate(db);
            }
        }

        [Test]
        public void Does_use_defaults_for_missing_values()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithDefaults>();

                db.Insert(new ModelWithDefaults { DefaultInt = 10 });

                var row = db.Select<ModelWithDefaults>().FirstOrDefault();

                Assert.That(row.Id, Is.GreaterThan(0));
                Assert.That(row.DefaultInt, Is.EqualTo(10));
                Assert.That(row.DefaultString, Is.EqualTo("String"));
            }
        }

        [Test]
        public void Does_only_use_defaults_for_all_default_properties()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithDefaults>();

                db.InsertUsingDefaults(
                    new ModelWithDefaults { Name = "foo", DefaultInt = 10 },
                    new ModelWithDefaults { Name = "bar", DefaultString = "qux" });

                var rows = db.Select<ModelWithDefaults>();
                rows.PrintDump();

                Assert.That(rows.All(x => x.Id > 0));
                Assert.That(rows.All(x => x.DefaultInt == 1));
                Assert.That(rows.All(x => x.DefaultString == "String"));
            }
        }
        
        private class MySqlDefaultValues
        {
            public int Id { get; set; }

            [Default(1)]
            public int DefaultInt { get; set; }

            public int DefaultIntNoDefault { get; set; }

            [Default(1)]
            public int? NDefaultInt { get; set; }

            [Default(1.1)]
            public double DefaultDouble { get; set; }

            [Default(1.1)]
            public double? NDefaultDouble { get; set; }

            [Default("'String'")]
            public string DefaultString { get; set; }

            [Default(OrmLiteVariables.SystemUtc)]
            public DateTime UpdatedDateUtc { get; set; }
        }
    }
}
