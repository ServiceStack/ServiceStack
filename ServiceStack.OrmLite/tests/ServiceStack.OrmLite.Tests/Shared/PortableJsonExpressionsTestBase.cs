using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

/// <summary>
/// Shared behavioral contract for the portable JSON API. Each supported provider
/// runs these exact assertions against a real database connection.
/// </summary>
public abstract class PortableJsonExpressionsTestBase
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

    protected abstract IDbConnection OpenJsonDbConnection();
    protected virtual bool SupportsJsonContains => false;

    private static JsonMetadata CreateMetadata(string state = "WA", decimal total = 125.50m) => new()
    {
        Customer = new Customer { Address = new Address { State = state } },
        Lines = [new OrderLine { Sku = "A-1", Quantity = 2 }],
        Tags = ["priority", "paid"],
        NullableTags = [null, "x"],
        Numbers = [1, 2],
        Total = total,
    };

    [Test]
    public void Executes_the_portable_and_typed_JSON_behavioral_contract()
    {
        using var db = OpenJsonDbConnection();
        db.DropAndCreateTable<JsonRow>();

        var metadata = CreateMetadata();
        var other = CreateMetadata("OR", 50m);
        db.Insert(new JsonRow { Id = 1, Data = metadata.ToJson(), TypedData = metadata });
        db.Insert(new JsonRow { Id = 2, Data = other.ToJson(), TypedData = other });

        var row = db.Single<JsonRow>(x =>
            Sql.IsJson(x.Data) == true &&
            Sql.Json<JsonMetadata>(x.Data).Customer.Address.State == "WA" &&
            Sql.Json<JsonMetadata>(x.Data).Lines[0].Quantity == 2 &&
            Sql.Json<JsonMetadata>(x.Data).Total == 125.50m &&
            Sql.Json<JsonMetadata>(x.Data).Tags.Contains("priority") &&
            Sql.Json<JsonMetadata>(x.Data).Numbers.Contains(1) &&
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
        Assert.That(db.Scalar<JsonValueType?>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonType(x.Data))), Is.EqualTo(JsonValueType.Object));
        Assert.That(db.Scalar<Address>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.Json<JsonMetadata>(x.Data).Customer.Address)).State, Is.EqualTo("WA"));
        Assert.That(db.Scalar<JsonMetadata>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonQuery<JsonMetadata>(x.Data))).Total, Is.EqualTo(125.50m));
        Assert.That(db.Scalar<string>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonValue<string>(x.Data, "$.Customer.Address"))), Is.Null);
        Assert.That(db.Scalar<Address>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonQuery<Address>(x.Data, "$.Missing"))), Is.Null);

        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonExists(x.Data, "$.NullableTags[0]")), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonExists(x.Data, "$.Missing")), Is.Zero);
        Assert.That(db.Scalar<JsonValueType?>(db.From<JsonRow>()
            .Where(x => x.Id == 1)
            .Select(x => Sql.JsonType(x.Data, "$.NullableTags[0]"))), Is.EqualTo(JsonValueType.Null));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains<string>(x.Data, "$.NullableTags", null)), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains(x.Data, "$.Numbers", 1)), Is.EqualTo(1));
        Assert.That(db.Count<JsonRow>(x => x.Id == 1 &&
            Sql.JsonArrayContains(x.Data, "$.Numbers", "1")), Is.Zero);

        var ordered = db.Select(db.From<JsonRow>()
            .Where(x => Sql.JsonValue<decimal?>(x.Data, "$.Total") >= 0m)
            .OrderByDescending(x => Sql.JsonValue<decimal?>(x.Data, "$.Total")));
        Assert.That(ordered.Select(x => x.Id).ToArray(), Is.EqualTo(new[] { 1, 2 }));

        var candidate = new {
            Customer = new {
                Address = new { shipping_state = "WA" }
            }
        };
        var requiredTags = new[] { "priority" };
        if (SupportsJsonContains)
        {
            Assert.That(db.Count<JsonRow>(x => Sql.JsonContains(x.Data, candidate)), Is.EqualTo(1));
            Assert.That(db.Count<JsonRow>(x => Sql.JsonContains(x.Data, requiredTags, "$.Tags")), Is.EqualTo(2));
        }
        else
        {
            Assert.Throws<NotSupportedException>(() => db.From<JsonRow>()
                .Where(x => Sql.JsonContains(x.Data, candidate))
                .ToSelectStatement());
        }

        db.Insert(new JsonRow { Id = 3, Data = "[\"priority\",\"paid\"]", TypedData = metadata });
        Assert.That(db.Scalar<int?>(db.From<JsonRow>()
            .Where(x => x.Id == 3)
            .Select(x => Sql.JsonArrayLength(x.Data))), Is.EqualTo(2));
        Assert.That(db.Count<JsonRow>(x => x.Id == 3 &&
            Sql.JsonArrayContains(x.Data, "priority")), Is.EqualTo(1));

        db.Insert(new JsonRow { Id = 4, Data = "not json", TypedData = metadata });
        Assert.That(db.Count<JsonRow>(x => Sql.IsJson(x.Data) == true), Is.EqualTo(3));
    }
}
