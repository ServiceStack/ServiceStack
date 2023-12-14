#nullable enable

using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteCreateTableWithNullableTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public interface IDemo
    {
        string? StringData { get; set; }
        DateTimeOffset? DateTimeOffsetData { get; set; }
        long? LongData { get; set; }
    }

    [Alias("demos")]
    public class DemoDb : IDemo
    {
        [AutoIncrement] public long? Id { get; set; }

        [Required] public string? StringData { get; set; }

        [Required] public DateTimeOffset? DateTimeOffsetData { get; set; }

        [Required] public long? LongData { get; set; }
    }

    [Test]
    public void Does_create_table_with_not_null_for_required_nullable_properties()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        db.CreateTable<DemoDb>();
        
        var capturedSql = string.Join("\n", captured.SqlStatements);
        capturedSql.Print();
        
        Assert.That(capturedSql.ToUpper().CountOccurrencesOf("NOT NULL"), Is.EqualTo(3));
    }
}