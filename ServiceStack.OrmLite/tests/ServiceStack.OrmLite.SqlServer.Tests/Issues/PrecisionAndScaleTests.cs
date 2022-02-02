using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    [TestFixture]
    public class PrecisionAndScaleTests : OrmLiteTestBase
    {
        private const string DropProcedureSql = @"
            IF OBJECT_ID('spGetNumber') IS NOT NULL
                    DROP PROCEDURE spGetNumber";

        private const string CreateProcedureSql = @"
            CREATE PROCEDURE spGetNumber 
            (
                @pNumber decimal(8,3) OUT
            )
            AS
            BEGIN
                SELECT @pNumber = 12345.678
            END";

        [Test]
        public void Can_execute_stored_procedure_with_scale_precision_params_()
        {
            using (var db = OpenDbConnection())
            {
                db.ExecuteSql(DropProcedureSql);
                db.ExecuteSql(CreateProcedureSql);

                var cmd = db.SqlProc("spGetNumber");

                var pNumber = cmd.AddParam("pNumber", 1.0, direction: ParameterDirection.Output, precision: 8, scale: 3);

                cmd.ExecuteNonQuery();

                Assert.That(pNumber.Value, Is.EqualTo(12345.678));
            }
        }
    }
}
