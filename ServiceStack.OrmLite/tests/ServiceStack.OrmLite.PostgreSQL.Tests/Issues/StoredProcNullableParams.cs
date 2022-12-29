using NUnit.Framework;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests.Issues
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class StoredProcNullableParams : OrmLiteProvidersTestBase
    {
        public StoredProcNullableParams(DialectContext context) : base(context) {}
        /* 
 *  Nullable Type tests
 *  NOTE: These test only test SqlList<T> they should probably also test Select<T>
 */

        private const string CreateFunction = @"
            CREATE OR REPLACE FUNCTION f_service_stack_function_{0}(
                v_{0} {0}
            ) RETURNS TABLE (
                id  int,
                val {0}                
            ) AS
            $BODY$
            BEGIN
                -- generate rows 1 - 10, but with the same val to make it look like a table.
                RETURN QUERY SELECT s AS id, v_{0} AS val FROM generate_series(1,10) s;
            END;
            $BODY$
            LANGUAGE plpgsql VOLATILE COST 100;
        ";

        private const string DropFunction = "DROP FUNCTION IF EXISTS f_service_stack_function_{0}({0});";


        public class ServiceStackTypeFunctionResultNullableInt
        {
            public int Id { get; set; }
            public int? Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_nullable_int_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "int";
                int? testVal = null;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));

                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                // Fix: DbCommand.Parameter for NULL values defaults to NpgsqlTypes.NpgsqlDbType.Text should be NpgsqlTypes.NpgsqlDbType.Integer

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultNullableInt>(
                    sql,
                    new
                    {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.Null);

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }

        public class ServiceStackTypeFunctionResultNullableShort
        {
            public int Id { get; set; }
            public short? Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_nullable_short_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "smallint";
                short? testVal = null;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));

                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                // Fix: DbCommand.Parameter for NULL values defaults to NpgsqlTypes.NpgsqlDbType.Text should be NpgsqlTypes.NpgsqlDbType.Smallint

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultNullableShort>(
                    sql,
                    new
                    {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.Null);

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }

        public class ServiceStackTypeFunctionResultNullableLong
        {
            public int Id { get; set; }
            public long? Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_nullable_long_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "bigint";
                long? testVal = null;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));

                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                // Fix: DbCommand.Parameter for NULL values defaults to NpgsqlTypes.NpgsqlDbType.Text should be NpgsqlTypes.NpgsqlDbType.Bigint

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultNullableLong>(
                    sql,
                    new
                    {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.Null);

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }
        
        public class ServiceStackTypeFunctionResultInt
        {
            public int Id { get; set; }
            public int Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_int_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "int";
                const int testVal = 123;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));

                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultInt>(
                    sql,
                    new {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.EqualTo(testVal));

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }

        public class ServiceStackTypeFunctionResultShort
        {
            public int Id { get; set; }
            public short Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_short_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "smallint";
                const short testVal = 123;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));

                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultShort>(
                    sql, new {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.EqualTo(testVal));

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }

        public class ServiceStackTypeFunctionResultLong
        {
            public int Id { get; set; }
            public long Val { get; set; }
        }

        [Test]
        public void Can_execute_function_with_long_param()
        {
            using (var db = OpenDbConnection())
            {
                const string pgTypeToTest = "bigint";
                const long testVal = long.MaxValue - 100;

                // if function already exists drop before create (can't change result)
                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
                // Make sure there isn't an INT function to fallback to.
                db.ExecuteSql(DropFunction.Fmt("int"));


                db.ExecuteSql(CreateFunction.Fmt(pgTypeToTest));
                db.GetLastSql().Print();

                var sql = "SELECT * FROM f_service_stack_function_{0}(@paramValue);".Fmt(pgTypeToTest);

                var rows = db.SqlList<ServiceStackTypeFunctionResultLong>(
                    sql,
                    new {
                        paramValue = testVal
                    });

                Assert.That(rows.Count, Is.EqualTo((10)));
                Assert.That(rows[0].Val, Is.EqualTo(testVal));

                db.ExecuteSql(DropFunction.Fmt(pgTypeToTest));
            }
        }
    }
}