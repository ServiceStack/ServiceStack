using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.OrmLite.MySql;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixture]
public class JsonExpressionTests
{
    public class JsonRow
    {
        public int Id { get; set; }
        public string Data { get; set; }
        public JsonMetadata TypedData { get; set; }
    }

    public class JsonMetadata
    {
        public Customer Customer { get; set; }
        public List<OrderLine> Lines { get; set; }
        public List<string> Tags { get; set; }
        public List<string> NullableTags { get; set; }
        public List<int> Numbers { get; set; }
        public decimal Total { get; set; }
    }

    public class Customer
    {
        public Address Address { get; set; }
    }

    public class Address
    {
        [DataMember(Name = "shipping_state")]
        public string State { get; set; }
    }

    public class OrderLine
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }

    private static IEnumerable<TestCaseData> Dialects
    {
        get
        {
            yield return new TestCaseData(new SqliteOrmLiteDialectProvider(), "json_extract", "json_type").SetName("SQLite JSON SQL");
            yield return new TestCaseData(new PostgreSqlDialectProvider(), "jsonb_path_query_first", "jsonb_typeof").SetName("PostgreSQL JSON SQL");
            yield return new TestCaseData(new SqlServer2022OrmLiteDialectProvider(), "JSON_VALUE", "OPENJSON").SetName("SQL Server JSON SQL");
            yield return new TestCaseData(new MySqlDialectProvider(), "JSON_EXTRACT", "JSON_TYPE").SetName("MySQL JSON SQL");
        }
    }

    [TestCaseSource(nameof(Dialects))]
    public void Generates_portable_raw_and_typed_JSON_expressions(
        IOrmLiteDialectProvider dialect, string valueFunction, string typeFunction)
    {
        var q = dialect.SqlExpression<JsonRow>()
            .Where(x =>
                Sql.IsJson(x.Data) == true &&
                Sql.JsonValue<string>(x.Data, "$.Customer.Address.shipping_state") == "WA" &&
                Sql.JsonValue<decimal?>(x.Data, "$.Total") >= 100m &&
                Sql.JsonExists(x.Data, "$.Tags[0]") &&
                Sql.JsonArrayContains(x.Data, "$.Tags", "priority"))
            .Select(x => new
            {
                State = Sql.JsonValue<string>(x.Data, "$.Customer.Address.shipping_state"),
                Address = Sql.JsonQuery<Address>(x.Data, "$.Customer.Address"),
                Type = Sql.JsonType(x.Data, "$.Customer.Address"),
                Count = Sql.JsonArrayLength(x.Data, "$.Lines"),
            });

        var sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain(valueFunction).IgnoreCase);
        Assert.That(sql, Does.Contain(typeFunction).IgnoreCase);
        Assert.That(sql, Does.Contain("$.Customer.Address.shipping_state"));
        Assert.That(q.Params, Has.Count.EqualTo(3));

        var typed = dialect.SqlExpression<JsonRow>()
            .Where(x =>
                Sql.Json<JsonMetadata>(x.Data).Customer.Address.State == "WA" &&
                Sql.Json<JsonMetadata>(x.Data).Lines[0].Quantity > 1 &&
                Sql.Json<JsonMetadata>(x.Data).Tags.Contains("priority") &&
                Sql.Json<JsonMetadata>(x.Data).Lines.Count == 1 &&
                Sql.Json<JsonMetadata>(x.Data).Customer.Address.State.Length == 2 &&
                Sql.Json(x.TypedData).Customer.Address.State == "WA")
            .Select(x => new
            {
                State = Sql.Json<JsonMetadata>(x.Data).Customer.Address.State,
                Address = Sql.Json<JsonMetadata>(x.Data).Customer.Address,
                Quantity = Sql.Json<JsonMetadata>(x.Data).Lines[0].Quantity,
            });

        var typedSql = typed.ToSelectStatement();
        Assert.That(typedSql, Does.Contain("$.Customer.Address.shipping_state"));
        Assert.That(typedSql, Does.Contain("$.Lines[0].Quantity"));
        Assert.That(typedSql, Does.Contain(valueFunction).IgnoreCase);
        if (dialect is SqlServer2022OrmLiteDialectProvider)
        {
            Assert.That(sql, Does.Contain("ISJSON(").And.Contain("VALUE"));
            Assert.That(sql, Does.Contain("WHERE [key]").IgnoreCase);
        }
    }

    private static IEnumerable<TestCaseData> ContainmentDialects
    {
        get
        {
            yield return new TestCaseData(new PostgreSqlDialectProvider(), "@>").SetName("PostgreSQL JSON containment");
            yield return new TestCaseData(new MySqlDialectProvider(), "JSON_CONTAINS").SetName("MySQL JSON containment");
        }
    }

    [TestCaseSource(nameof(ContainmentDialects))]
    public void Generates_native_JSON_document_containment(IOrmLiteDialectProvider dialect, string operation)
    {
        var candidate = new[] { "priority" };
        var q = dialect.SqlExpression<JsonRow>()
            .Where(x => Sql.JsonContains(x.Data, candidate, "$.Tags"));

        var sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain(operation));
        Assert.That(q.Params, Has.Count.EqualTo(1));
        Assert.That(q.Params[0].Value, Is.EqualTo("[\"priority\"]"));
    }

    [TestCaseSource(nameof(Dialects))]
    public void Rejects_JSON_array_document_candidates(
        IOrmLiteDialectProvider dialect, string _, string __)
    {
        Assert.Throws<NotSupportedException>(() => dialect.SqlExpression<JsonRow>()
            .Where(x => Sql.JsonArrayContains(x.Data, "$.Tags", new[] { "priority" }))
            .ToSelectStatement());
    }

    [Test]
    public void SQLite_executes_portable_and_typed_JSON_queries()
    {
        var dialect = new SqliteOrmLiteDialectProvider { UseJson = true };
        var factory = new OrmLiteConnectionFactory(":memory:", dialect);
        using var db = factory.OpenDbConnection();
        db.DropAndCreateTable<JsonRow>();

        var metadata = new JsonMetadata
        {
            Customer = new Customer { Address = new Address { State = "WA" } },
            Lines = [new OrderLine { Sku = "A-1", Quantity = 2 }],
            Tags = ["priority", "paid"],
            NullableTags = [null, "x"],
            Numbers = [1, 2],
            Total = 125.50m,
        };
        db.Insert(new JsonRow { Id = 1, Data = metadata.ToJson(), TypedData = metadata });
        db.Insert(new JsonRow { Id = 2, Data = "not json", TypedData = metadata });

        var row = db.Single<JsonRow>(x =>
            Sql.IsJson(x.Data) == true &&
            Sql.Json<JsonMetadata>(x.Data).Customer.Address.State == "WA" &&
            Sql.Json<JsonMetadata>(x.Data).Lines[0].Quantity == 2 &&
            Sql.Json<JsonMetadata>(x.Data).Tags.Contains("priority") &&
            Sql.Json<JsonMetadata>(x.Data).Lines.Count == 1 &&
            Sql.Json<JsonMetadata>(x.Data).Customer.Address.State.Length == 2 &&
            Sql.Json(x.TypedData).Customer.Address.State == "WA" &&
            Sql.JsonArrayContains(x.Data, "$.Tags", "priority"));

        Assert.That(row.Id, Is.EqualTo(1));
        Assert.That(db.Scalar<string>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonValue<string>(x.Data, "$.Customer.Address.shipping_state"))), Is.EqualTo("WA"));
        Assert.That(db.Scalar<int?>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonArrayLength(x.Data, "$.Lines"))), Is.EqualTo(1));
        Assert.That(db.Scalar<JsonValueType?>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonType(x.Data, "$.Customer.Address"))), Is.EqualTo(JsonValueType.Object));
        Assert.That(db.Scalar<Address>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.Json<JsonMetadata>(x.Data).Customer.Address)).State, Is.EqualTo("WA"));
        Assert.That(db.Scalar<string>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonValue<string>(x.Data, "$.Customer.Address"))), Is.Null);

        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonExists(x.Data, "$.NullableTags[0]")), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonExists(x.Data, "$.Missing")), Is.EqualTo(0));
        Assert.That(db.Scalar<JsonValueType?>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonType(x.Data, "$.NullableTags[0]"))), Is.EqualTo(JsonValueType.Null));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains<string>(x.Data, "$.NullableTags", null)), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains(x.Data, "$.Numbers", 1)), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains(x.Data, "$.Numbers", "1")), Is.EqualTo(0));
    }
}
