using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Dapper;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class CsvTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_serialize_Dapper_results_to_CSV()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Poco>();

        db.Insert(new Poco { Id = 1, Name = "Foo" });
        db.Insert(new Poco { Id = 2, Name = "Bar" });

        var results = db.Query("select * from Poco Order By Id");

        var json = JsonSerializer.SerializeToString(results);
        Assert.That(json.ToLower(), Is.EqualTo("[{\"Id\":1,\"Name\":\"Foo\"},{\"Id\":2,\"Name\":\"Bar\"}]".ToLower()));

        var csv = CsvSerializer.SerializeToCsv(results);
        Assert.That(csv.NormalizeNewLines().ToLower(), Is.EqualTo("Id,Name\n1,Foo\n2,Bar".ToLower()));
    }

    [Test]
    public void Can_serialize_OrmLite_results_to_CSV()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Poco>();

        db.Insert(new Poco { Id = 1, Name = "Foo" });
        db.Insert(new Poco { Id = 2, Name = "Bar" });

        var results = db.Select<Poco>();

        var json = JsonSerializer.SerializeToString(results);
        Assert.That(json.ToLower(), Is.EqualTo("[{\"Id\":1,\"Name\":\"Foo\"},{\"Id\":2,\"Name\":\"Bar\"}]".ToLower()));

        var csv = results.ToCsv();
        Assert.That(csv.NormalizeNewLines().ToLower(), Is.EqualTo("Id,Name\n1,Foo\n2,Bar".ToLower()));

        var rows = db.Select<Dictionary<string,object>>("select * from Poco Order By Id");
        csv = rows.ToCsv();
        Assert.That(csv.NormalizeNewLines().ToLower(), Is.EqualTo("Id,Name\n1,Foo\n2,Bar".ToLower()));
    }
}