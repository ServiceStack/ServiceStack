using System;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests;

public class DialectProviderApiTests : OrmLiteTestBase
{
    [Test]
    public void Dialect_APIs_referencing_tables_should_allow_and_return_null()
    {
        var dialect = DbFactory.DialectProvider;
        Assert.That(dialect.QuoteTable((string)null), Is.Null);
        Assert.That(dialect.GetQuotedTableName((string)null), Is.Null);
        Assert.That(dialect.GetQuotedTableName((Type)null), Is.Null);
        Assert.That(dialect.GetQuotedTableName((ModelDefinition)null), Is.Null);
        Assert.That(dialect.GetTableName((string)null), Is.Null);
        Assert.That(dialect.GetTableName((Type)null), Is.Null);
        Assert.That(dialect.GetTableName((ModelDefinition)null), Is.Null);
    }
    
    [Test]
    public void Dialect_APIs_referencing_columns_should_allow_and_return_null()
    {
        var dialect = DbFactory.DialectProvider;
        Assert.That(dialect.GetQuotedColumnName((string)null), Is.Null);
        Assert.That(dialect.GetQuotedColumnName((FieldDefinition)null), Is.Null);
        Assert.That(dialect.GetQuotedName(null), Is.Null);
    }
}