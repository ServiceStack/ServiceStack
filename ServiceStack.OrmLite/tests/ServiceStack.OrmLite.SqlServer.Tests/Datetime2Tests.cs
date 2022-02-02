using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.SqlServer.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests
{
	public class Datetime2Tests : OrmLiteTestBase
	{
		private OrmLiteConnectionFactory dbFactory;

		[OneTimeSetUp]
		public new void TestFixtureSetUp()
		{
			base.TestFixtureSetUp();
			
			//change to datetime2 - check for higher range and precision
			//default behaviour: normal datetime can't hold DateTime values of year 1.
			dbFactory = new OrmLiteConnectionFactory(base.ConnectionString, SqlServerOrmLiteDialectProvider.Instance);
			var dp2 = new SqlServerOrmLiteDialectProvider();
			dp2.RegisterConverter<DateTime>(new SqlServerDateTime2Converter());
			dbFactory.RegisterConnection("dt2", base.ConnectionString, dp2);
		}
		
		[Test]
		public void datetime_tests__can_use_datetime2()
		{
            using (var conn = dbFactory.OpenDbConnection("dt2")) {
				var test_object_ValidForDatetime2 = Datetime2Test.get_test_object_ValidForDatetime2();

				conn.CreateTable<Datetime2Test>(true);

				//normal insert
                var insertedId = conn.Insert(test_object_ValidForDatetime2, selectIdentity:true);

				//read back, and verify precision
                var fromDb = conn.SingleById<Datetime2Test>(insertedId);
				Assert.AreEqual(test_object_ValidForDatetime2.ToVerifyPrecision, fromDb.ToVerifyPrecision);

				//update
				fromDb.ToVerifyPrecision = test_object_ValidForDatetime2.ToVerifyPrecision.Value.AddYears(1);
				conn.Update(fromDb);
                var fromDb2 = conn.SingleById<Datetime2Test>(insertedId);
				Assert.AreEqual(test_object_ValidForDatetime2.ToVerifyPrecision.Value.AddYears(1), fromDb2.ToVerifyPrecision);

				//check InsertParam
				conn.Insert(test_object_ValidForDatetime2);

                //check select on datetime2 value
                var result = conn.Select<Datetime2Test>(t => t.ToVerifyPrecision == test_object_ValidForDatetime2.ToVerifyPrecision);
                Assert.AreEqual(result.Single().ToVerifyPrecision, test_object_ValidForDatetime2.ToVerifyPrecision);
            }
		}
		
		[Test]
		public void datetime_tests__check_default_behaviour()
		{
            using (var conn = dbFactory.OpenDbConnection()) {
				var test_object_ValidForDatetime2 = Datetime2Test.get_test_object_ValidForDatetime2();
				var test_object_ValidForNormalDatetime = Datetime2Test.get_test_object_ValidForNormalDatetime();

				conn.CreateTable<Datetime2Test>(true);

				// normal insert
                var insertedId = conn.Insert(test_object_ValidForNormalDatetime, selectIdentity:true);

				// insert works, but can't regular datetime's precision is not great enough.
                var fromDb = conn.SingleById<Datetime2Test>(insertedId);
				Assert.AreNotEqual(test_object_ValidForNormalDatetime.ToVerifyPrecision, fromDb.ToVerifyPrecision);

				var thrown = Assert.Throws<SqlTypeException>(() => {
                    conn.Insert(test_object_ValidForDatetime2);
				});
                Assert.That(thrown.Message.Contains("SqlDateTime overflow"));

				
				// check InsertParam
				conn.Insert(test_object_ValidForNormalDatetime);
				// InsertParam fails differently:
				var insertParamException = Assert.Throws<System.Data.SqlTypes.SqlTypeException>(() => {
					conn.Insert(test_object_ValidForDatetime2);
				});
				Assert.That(insertParamException.Message.Contains("SqlDateTime overflow"));
			}
		}

		[Test]
		public void Can_Select_DateTime()
		{
			using (var db = dbFactory.OpenDbConnection("dt2"))
			{
				db.DropAndCreateTable<Datetime2Test>();
				db.Insert(Datetime2Test.get_test_object_ValidForNormalDatetime());

				var now = DateTime.UtcNow;
				var q = db.From<Datetime2Test>()
					.Select(x => new { SomeDateTime = now });
				
				var result = db.Select(q)[0];
				
				Assert.That(result.SomeDateTime, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));
			}
		}

		[Test]
		[NonParallelizable]
		public void Can_Select_DateTime2()
		{
			using (var db = dbFactory.OpenDbConnection("dt2"))
			{
				db.DropAndCreateTable<Datetime2Test>();
				db.Insert(Datetime2Test.get_test_object_ValidForDatetime2());

				var now = DateTime.Parse("2019-03-12 09:10:48.3477082");
				var q = db.From<Datetime2Test>()
					.Select(x => new { SomeDateTime = now });
				
				var result = db.Select(q)[0];
				
				Assert.That(result.SomeDateTime, Is.EqualTo(now));
			}
		}

		private class Datetime2Test
		{
			[AutoIncrement]
			public int Id { get; set; }
			public DateTime SomeDateTime { get; set; }
			public DateTime? ToVerifyPrecision { get; set; }
			public DateTime? NullableDateTimeLeaveItNull { get; set; }

		    /// <summary>
		    /// to check datetime(2)'s precision. A regular 'datetime' is not precise enough
		    /// </summary>
		    public static readonly DateTime regular_datetime_field_cant_hold_this_exact_moment = new DateTime(2013, 3, 17, 21, 29, 1, 678).AddTicks(1);

			public static Datetime2Test get_test_object_ValidForDatetime2() { return new Datetime2Test { SomeDateTime = new DateTime(1, 1, 1), ToVerifyPrecision = regular_datetime_field_cant_hold_this_exact_moment }; }

			public static Datetime2Test get_test_object_ValidForNormalDatetime() { return new Datetime2Test { SomeDateTime = new DateTime(2001, 1, 1), ToVerifyPrecision = regular_datetime_field_cant_hold_this_exact_moment }; }

		}
	}
}
