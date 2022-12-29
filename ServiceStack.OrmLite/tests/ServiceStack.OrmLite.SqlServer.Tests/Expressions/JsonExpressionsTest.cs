using System;
using System.Collections.Generic;
using System.Configuration;

using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer.Converters;

using NUnit.Framework;

namespace ServiceStack.OrmLite.SqlServerTests.Expressions
{
	public class JsonExpressionsTest : OrmLiteTestBase
	{
		public static Address Addr { get; set; } 
			= new Address
				{
					Line1 = "1234 Main Street",
					Line2 = "Apt. 404",
					City = "Las Vegas",
					State = "NV"
				};

		[OneTimeSetUp]
		public override void TestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			OrmLiteConfig.DialectProvider = SqlServer2016Dialect.Provider;
			OrmLiteConfig.DialectProvider.RegisterConverter<string>(new SqlServerJsonStringConverter());
			OrmLiteConfig.DialectProvider.RegisterConverter<Address>(new SqlServerJsonStringConverter());

			ConnectionString = GetConnectionString();

			Db = OpenDbConnection();

			// Load test data
			Db.DropAndCreateTable<TestType>();
			Db.Insert(new TestType { Id = 1, StringColumn = "not json" });
			Db.Insert(new TestType { Id = 2, StringColumn = Addr.ToJson() });
		}

		[OneTimeTearDown]
		public void TextFixtureTearDown()
		{
			if (Db != null)
			{
				if (Db.State == System.Data.ConnectionState.Open)
					Db.Close();

				Db.Dispose();
			}
		}


		[Test]
		public void Can_test_if_string_field_contains_json()
		{
			// test if string field is not JSON with Sql.IsJson
			var j = Db.From<TestType>()
				.Select(x => Sql.IsJson(x.StringColumn))
				.Where(x => x.Id == 1);
			var isJson = Db.Scalar<bool>(j);

			Assert.IsFalse(isJson);

			// test if string field is JSON with Sql.IsJson
			j = Db.From<TestType>()
				.Select(x => Sql.IsJson(x.StringColumn))
				.Where(x => x.Id == 2);
			isJson = Db.Scalar<bool>(j);

			Assert.IsTrue(isJson);		
		}

		[Test]
		public void Can_select_using_a_json_scalar_filter()
		{
			// retrieve records where City in Address is NV (1 record)
			var actual = Db.Select<TestType>(q => 
					Sql.JsonValue(q.StringColumn, "$.State") == "NV" 
					&& q.Id == 2 
				);

			Assert.IsNotEmpty(actual);

			// retrieve records where City in Address is FL (0 records)
			actual = Db.Select<TestType>(q => 
					Sql.JsonValue(q.StringColumn, "$.State") == "FL" 
					&& q.Id == 2 
				);

			Assert.IsEmpty(actual);
		}

		[Test]
		public void Can_select_a_json_scalar_value()
		{
			// retrieve only the State in a field that contains a JSON Address
			var state = Db.Scalar<string>(
				Db.From<TestType>()
					.Where(q => q.Id == 2)
					.Select(q =>
						Sql.JsonValue(q.StringColumn, "$.State")
					)
			);

			Assert.AreEqual(state, Addr.State);
		}

		[Test]
		public void Can_select_a_json_object_value()
		{
			// demo how to retrieve inserted JSON string directly to an object
			var address = Db.Scalar<Address>(
					Db.From<TestType>()
					.Where(q => q.Id == 2)
					.Select(q => q.StringColumn)
				);

			Assert.That(Addr.Line1, Is.EqualTo(address.Line1));
			Assert.That(Addr.Line2, Is.EqualTo(address.Line2));
			Assert.That(Addr.City, Is.EqualTo(address.City));
			Assert.That(Addr.State, Is.EqualTo(address.State));
		}

		[Ignore("Not functioning properly, issue with converter")]
		[Test]
		public void Can_insert_an_object_directly_to_json()
		{
			var tableName = Db.GetDialectProvider().GetQuotedTableName(ModelDefinition<TestType>.Definition);
			var sql = $"INSERT {tableName} (StringColumn) VALUES (@Address);";

			// breaks here with an invalid conversion error from Address to string
			Db.ExecuteSql(sql, new { Id = 3, Address = Addr });

			// demo how to retrieve inserted JSON string directly to an object
			var address = Db.Single<Address>(
				Db.From<TestType>()
					.Where(q => q.Id == 3)
					.Select(q => q.StringColumn)
				);

			Assert.That(Addr.Line1, Is.EqualTo(address.Line1));
			Assert.That(Addr.Line2, Is.EqualTo(address.Line2));
			Assert.That(Addr.City, Is.EqualTo(address.City));
			Assert.That(Addr.State, Is.EqualTo(address.State));
		}

		[SqlJson]
		public class Address
		{
			public string Line1 { get; set; }
			public string Line2 { get; set; }
			public string City { get; set; }
			public string State { get; set; }
		}
	}
}