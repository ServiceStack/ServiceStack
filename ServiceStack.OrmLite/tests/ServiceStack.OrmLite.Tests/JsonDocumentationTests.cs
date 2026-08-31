using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite.MySql;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

/// <summary>
/// Executable coverage for the examples in docs.servicestack.net/MyApp/_pages/ormlite/json.md.
/// Keep the model and query shapes aligned with the user documentation.
/// </summary>
[TestFixture]
[NonParallelizable]
public class JsonDocumentationTests
{
    public class OrderEvent
    {
        public long Id { get; set; }
        public string Data { get; set; }
        public OrderDocument Document { get; set; }
    }

    public class OrderDocument
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

    public class OrderSummary
    {
        public long Id { get; set; }
        public string State { get; set; }
        public decimal Total { get; set; }
        public Address Address { get; set; }
    }

    private static OrderDocument CreateDocument(string state = "WA", decimal total = 125.50m) => new()
    {
        Customer = new Customer {
            Address = new Address { State = state }
        },
        Lines = [new OrderLine { Sku = "A-1", Quantity = 2 }],
        Tags = ["priority", "paid"],
        NullableTags = [null, "x"],
        Numbers = [1, 2],
        Total = total,
    };

    private static IDbConnection OpenExampleDatabase(out OrderDocument document)
    {
        var dialect = new SqliteOrmLiteDialectProvider { UseJson = true };
        var factory = new OrmLiteConnectionFactory(":memory:", dialect);
        var db = factory.OpenDbConnection();
        db.CreateTable<OrderEvent>();

        document = CreateDocument();
        db.Insert(new OrderEvent {
            Data = document.ToJson(),
            Document = document,
        });

        var other = CreateDocument("OR", 50m);
        db.Insert(new OrderEvent {
            Id = 2,
            Data = other.ToJson(),
            Document = other,
        });
        return db;
    }

    [Test]
    public void Typed_query_and_projection_examples_execute()
    {
        using var db = OpenExampleDatabase(out _);

        var q = db.From<OrderEvent>()
            .Where(x =>
                Sql.Json<OrderDocument>(x.Data).Customer.Address.State == "WA" &&
                Sql.Json<OrderDocument>(x.Data).Tags.Contains("priority") &&
                Sql.Json<OrderDocument>(x.Data).Lines[0].Quantity > 1);

        var orders = db.Select(q);
        Assert.That(orders, Has.Count.EqualTo(1));

        var scalarQuery = db.From<OrderEvent>()
            .Where(x =>
                Sql.Json<OrderDocument>(x.Data).Customer.Address.State == "WA" &&
                Sql.Json<OrderDocument>(x.Data).Total >= 100m);
        Assert.That(db.Select(scalarQuery), Has.Count.EqualTo(1));

        var typedColumnQuery = db.From<OrderEvent>()
            .Where(x => Sql.Json(x.Document).Customer.Address.State == "WA");
        Assert.That(db.Select(typedColumnQuery), Has.Count.EqualTo(1));

        var index = 0;
        var arrayQuery = db.From<OrderEvent>()
            .Where(x =>
                Sql.Json<OrderDocument>(x.Data).Lines[0].Quantity >= 2 &&
                Sql.Json<OrderDocument>(x.Data).Lines[index].Sku == "A-1" &&
                Sql.Json<OrderDocument>(x.Data).Tags.Contains("priority") &&
                Sql.Json<OrderDocument>(x.Data).Numbers.Contains(1) &&
                Sql.Json<OrderDocument>(x.Data).Lines.Count > 0);
        Assert.That(db.Select(arrayQuery), Has.Count.EqualTo(2));

        var scalarOperationQuery = db.From<OrderEvent>()
            .Where(x => Sql.Json<OrderDocument>(x.Data).Customer.Address.State.Length == 2);
        Assert.That(db.Select(scalarOperationQuery), Has.Count.EqualTo(2));

        var summaryQuery = db.From<OrderEvent>()
            .Where(x => x.Id == 0)
            .Select(x => new {
                x.Id,
                State = Sql.Json<OrderDocument>(x.Data).Customer.Address.State,
                Total = Sql.Json<OrderDocument>(x.Data).Total,
                Address = Sql.Json<OrderDocument>(x.Data).Customer.Address,
            });

        var summaries = db.Select<OrderSummary>(summaryQuery);
        Assert.That(summaries, Has.Count.EqualTo(1));
        Assert.Multiple(() => {
            Assert.That(summaries[0].State, Is.EqualTo("WA"));
            Assert.That(summaries[0].Total, Is.EqualTo(125.50m));
            Assert.That(summaries[0].Address.State, Is.EqualTo("WA"));
        });

        const long id = 0;
        var address = db.Scalar<Address>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.Json<OrderDocument>(x.Data).Customer.Address));
        Assert.That(address.State, Is.EqualTo("WA"));
    }

    [Test]
    public void Explicit_path_root_array_null_and_type_examples_execute()
    {
        using var db = OpenExampleDatabase(out var document);
        const long id = 0;

        var statePath = "$.Customer.Address.shipping_state";
        var q = db.From<OrderEvent>()
            .Where(x =>
                Sql.JsonValue<string>(x.Data, statePath) == "WA" &&
                Sql.JsonExists(x.Data, "$.Tags[0]"));
        Assert.That(db.Select(q), Has.Count.EqualTo(1));

        var ordered = db.Select(db.From<OrderEvent>()
            .Where(x => Sql.JsonValue<decimal?>(x.Data, "$.Total") >= 0m)
            .OrderByDescending(x => Sql.JsonValue<decimal?>(x.Data, "$.Total")));
        Assert.That(ordered.Select(x => x.Id).ToArray(), Is.EqualTo(new long[] { 0, 2 }));

        var address = db.Scalar<Address>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonQuery<Address>(x.Data, "$.Customer.Address")));
        Assert.That(address.State, Is.EqualTo("WA"));

        var rootDocument = db.Scalar<OrderDocument>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonQuery<OrderDocument>(x.Data)));
        Assert.That(rootDocument.Total, Is.EqualTo(125.50m));

        var withTags = db.Select(db.From<OrderEvent>()
            .Where(x => Sql.JsonExists(x.Data, "$.Tags")));
        Assert.That(withTags, Has.Count.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => Sql.JsonExists(x.Data, "$.NullableTags[0]")), Is.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => Sql.JsonExists(x.Data, "$.Missing")), Is.Zero);

        var type = db.Scalar<JsonValueType?>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonType(x.Data, "$.Customer.Address")));
        Assert.That(type, Is.EqualTo(JsonValueType.Object));

        var rootType = db.Scalar<JsonValueType?>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonType(x.Data)));
        Assert.That(rootType, Is.EqualTo(JsonValueType.Object));

        Assert.That(db.Count<OrderEvent>(x => Sql.JsonArrayLength(x.Data, "$.Lines") > 0), Is.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => Sql.JsonArrayContains(x.Data, "$.Tags", "priority")), Is.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => Sql.JsonArrayContains(x.Data, "$.Numbers", 1)), Is.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => Sql.JsonArrayContains<string>(
            x.Data, "$.NullableTags", null)), Is.EqualTo(2));

        Assert.That(db.Scalar<string>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonValue(x.Data, "$.Customer.Address"))), Is.Null);
        Assert.That(db.Scalar<Address>(db.From<OrderEvent>()
            .Where(x => x.Id == id)
            .Select(x => Sql.JsonQuery<Address>(x.Data, "$.Missing"))), Is.Null);

        db.Insert(new OrderEvent {
            Id = 3,
            Data = "{\"Null\":null,\"String\":\"x\",\"Number\":1,\"Boolean\":true,\"Array\":[],\"Object\":{}}",
            Document = document,
        });
        var expectedTypes = new Dictionary<string, JsonValueType> {
            ["Null"] = JsonValueType.Null,
            ["String"] = JsonValueType.String,
            ["Number"] = JsonValueType.Number,
            ["Boolean"] = JsonValueType.Boolean,
            ["Array"] = JsonValueType.Array,
            ["Object"] = JsonValueType.Object,
        };
        foreach (var entry in expectedTypes)
        {
            var typePath = "$." + entry.Key;
            var actual = db.Scalar<JsonValueType?>(db.From<OrderEvent>()
                .Where(x => x.Id == 3)
                .Select(x => Sql.JsonType(x.Data, typePath)));
            Assert.That(actual, Is.EqualTo(entry.Value), entry.Key);
        }

        db.Insert(new OrderEvent { Id = 4, Data = "[\"priority\",\"paid\"]", Document = document });
        Assert.That(db.Scalar<int?>(db.From<OrderEvent>()
            .Where(x => x.Id == 4)
            .Select(x => Sql.JsonArrayLength(x.Data))), Is.EqualTo(2));
        Assert.That(db.Count<OrderEvent>(x => x.Id == 4 &&
            Sql.JsonArrayContains(x.Data, "priority")), Is.EqualTo(1));

        db.Insert(new OrderEvent { Id = 5, Data = "{\"property-name\":\"quoted\"}", Document = document });
        Assert.That(db.Scalar<string>(db.From<OrderEvent>()
            .Where(x => x.Id == 5)
            .Select(x => Sql.JsonValue<string>(x.Data, "$.\"property-name\""))), Is.EqualTo("quoted"));

        db.Insert(new OrderEvent { Id = 6, Data = "not json", Document = document });
        var validJsonRows = db.Count<OrderEvent>(x => Sql.IsJson(x.Data) == true);
        Assert.That(validJsonRows, Is.EqualTo(5));
    }

    private static IEnumerable<TestCaseData> DocumentationDialects
    {
        get
        {
            yield return new TestCaseData(new SqliteOrmLiteDialectProvider()).SetName("JSON docs compile for SQLite");
            yield return new TestCaseData(new PostgreSqlDialectProvider()).SetName("JSON docs compile for PostgreSQL");
            yield return new TestCaseData(new SqlServer2022OrmLiteDialectProvider()).SetName("JSON docs compile for SQL Server");
            yield return new TestCaseData(new MySqlDialectProvider()).SetName("JSON docs compile for MySQL");
        }
    }

    [TestCaseSource(nameof(DocumentationDialects))]
    public void Captured_path_index_root_overload_and_SQL_inspection_examples_generate(
        IOrmLiteDialectProvider dialect)
    {
        var path = "$.Customer.Address.shipping_state";
        var index = 0;
        var q = dialect.SqlExpression<OrderEvent>()
            .Where(x =>
                Sql.JsonValue<string>(x.Data, path) == "WA" &&
                Sql.Json<OrderDocument>(x.Data).Lines[index].Sku == "A-1" &&
                Sql.Json<OrderDocument>(x.Data).Numbers.Contains(1))
            .OrderByDescending(x => Sql.JsonValue<decimal?>(x.Data, "$.Total"));

        var sql = q.ToSelectStatement();
        var parameters = q.Params;
        Assert.Multiple(() => {
            Assert.That(sql, Does.Contain("$.Customer.Address.shipping_state"));
            Assert.That(sql, Does.Contain("$.Lines[0].Sku"));
            Assert.That(parameters, Is.Not.Empty);
        });

        var roots = dialect.SqlExpression<OrderEvent>()
            .Where(x => Sql.JsonArrayContains(x.Data, "priority"))
            .Select(x => new {
                Document = Sql.JsonQuery<OrderDocument>(x.Data),
                Type = Sql.JsonType(x.Data),
                Length = Sql.JsonArrayLength(x.Data),
            });
        Assert.That(roots.ToSelectStatement(), Is.Not.Empty);
    }

    private static IEnumerable<TestCaseData> ContainmentDialects
    {
        get
        {
            yield return new TestCaseData(new PostgreSqlDialectProvider(), "@>").SetName("JSON docs containment for PostgreSQL");
            yield return new TestCaseData(new MySqlDialectProvider(), "JSON_CONTAINS").SetName("JSON docs containment for MySQL");
        }
    }

    [TestCaseSource(nameof(ContainmentDialects))]
    public void Root_and_nested_document_containment_examples_generate(
        IOrmLiteDialectProvider dialect, string operation)
    {
        var candidate = new {
            Customer = new {
                Address = new { shipping_state = "WA" }
            }
        };
        var root = dialect.SqlExpression<OrderEvent>()
            .Where(x => Sql.JsonContains(x.Data, candidate));

        var requiredTags = new[] { "priority" };
        var nested = dialect.SqlExpression<OrderEvent>()
            .Where(x => Sql.JsonContains(x.Data, requiredTags, "$.Tags"));

        Assert.Multiple(() => {
            Assert.That(root.ToSelectStatement(), Does.Contain(operation));
            Assert.That(root.Params[0].Value.ToString(), Does.Contain("shipping_state"));
            Assert.That(nested.ToSelectStatement(), Does.Contain(operation));
            Assert.That(nested.Params[0].Value, Is.EqualTo("[\"priority\"]"));
        });
    }

    [Test]
    public void Unsupported_document_containment_examples_fail_while_building_SQL()
    {
        var candidate = new { Customer = "value" };
        foreach (var dialect in new IOrmLiteDialectProvider[] {
                     new SqliteOrmLiteDialectProvider(),
                     new SqlServer2022OrmLiteDialectProvider(),
                 })
        {
            Assert.Throws<NotSupportedException>(() => dialect.SqlExpression<OrderEvent>()
                .Where(x => Sql.JsonContains(x.Data, candidate))
                .ToSelectStatement());
        }
    }

    [Test]
    public void Fluent_configuration_examples_enable_JSON_serialization()
    {
        var dialects = new[] {
            Configure(options => options.UseSqlite(":memory:")),
            Configure(options => options.UsePostgres("Server=localhost")),
            Configure(options => options.UseSqlServer("Server=localhost")),
            Configure(options => options.UseMySql("Server=localhost")),
        };

        Assert.That(dialects.Select(x => x.StringSerializer),
            Has.All.InstanceOf<JsonComplexTypeSerializer>());

        var callbackInvoked = false;
        var explicitDialect = Configure(options => options.UseSqlite(":memory:", dialect => {
            dialect.UseJson = true;
            callbackInvoked = true;
        }));
        Assert.Multiple(() => {
            Assert.That(callbackInvoked, Is.True);
            Assert.That(explicitDialect.StringSerializer, Is.InstanceOf<JsonComplexTypeSerializer>());
        });
    }

    private static IOrmLiteDialectProvider Configure(Action<OrmLiteConfigOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddOrmLite(configure);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDbConnectionFactory>().GetDialectProvider();
    }

    [Test]
    public async Task Generated_JSON_expression_can_be_reused_with_async_APIs()
    {
        using var db = OpenExampleDatabase(out _);
        var q = db.From<OrderEvent>()
            .Where(x =>
                Sql.Json<OrderDocument>(x.Data).Customer.Address.State == "WA" &&
                Sql.Json<OrderDocument>(x.Data).Tags.Contains("priority"));

        var sql = q.ToSelectStatement();
        var parameters = q.Params;
        Assert.Multiple(() => {
            Assert.That(sql, Is.Not.Empty);
            Assert.That(parameters, Is.Not.Empty);
        });

        var results = await db.SelectAsync(q);
        Assert.That(results, Has.Count.EqualTo(1));
    }
}
