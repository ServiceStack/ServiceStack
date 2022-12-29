using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;
using IgnoreAttribute = NUnit.Framework.IgnoreAttribute;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class OrmLiteExecuteProcedureTests : OrmLiteProvidersTestBase
    {
        public OrmLiteExecuteProcedureTests(DialectContext context) : base(context) {}

        private const string Create = @"
            CREATE OR REPLACE FUNCTION f_service_stack(
                v_string_values text[],
                v_integer_values integer[]
            ) RETURNS BOOLEAN AS
            $BODY$
            BEGIN
                IF v_string_values[1] <> 'ServiceStack' THEN
                    RAISE EXCEPTION 'Unexpected value in string array[1] %', v_string_values[1];
                END IF;
                IF v_string_values[2] <> 'Thoughtfully Architected' THEN
                    RAISE EXCEPTION 'Unexpected value in string array[2] %', v_string_values[2];
                END IF;
                IF v_integer_values[1] <> 1 THEN
                    RAISE EXCEPTION 'Unexpected value in integer array[1] %', v_integer_values[1];
                END IF;
                IF v_integer_values[2] <> 2 THEN
                    RAISE EXCEPTION 'Unexpected value in integer array[2] %', v_integer_values[2];
                END IF;
                IF v_integer_values[3] <> 3 THEN
                    RAISE EXCEPTION 'Unexpected value in integer array[3] %', v_integer_values[3];
                END IF;
                RETURN TRUE;
            END;
            $BODY$
            LANGUAGE plpgsql VOLATILE COST 100;
            ";

        private const string Drop = "DROP FUNCTION f_service_stack(text[], integer[]);";

        [Alias("f_service_stack")]
        public class ServiceStackFunctionWithAlias
        {
            [CustomField("text[]")]
            [Alias("v_string_values")]
            public string[] StringValues { get; set; }
            [CustomField("integer[]")]
            [Alias("v_integer_values")]
            public int[] IntegerValues { get; set; }
        }

        [Alias("f_service_stack")]
        public class ServiceStackFunctionNoAlias
        {
            [CustomField("text[]")]
            public string[] v_string_values { get; set; }
            [CustomField("int[]")]
            public int[] v_integer_values { get; set; }
        }

        [Test]
        public void Can_execute_stored_procedure_with_array_arguments()
        {
            using (var db = OpenDbConnection())
            {
                db.ExecuteSql(Create);
                db.GetLastSql().Print();

                // Execute using [Alias()] attribute
                db.ExecuteProcedure(new ServiceStackFunctionWithAlias
                {
                    StringValues = new[] { "ServiceStack", "Thoughtfully Architected" },
                    IntegerValues = new[] { 1, 2, 3 }
                });
                db.GetLastSql().Print();

                // Execute without using [Alias()] attribute
                db.ExecuteProcedure(new ServiceStackFunctionNoAlias
                {
                    v_string_values = new[] { "ServiceStack", "Thoughtfully Architected" },
                    v_integer_values = new[] { 1, 2, 3 }
                });
                db.GetLastSql().Print();

                db.ExecuteSql(Drop);
            }
        }
    }
}
