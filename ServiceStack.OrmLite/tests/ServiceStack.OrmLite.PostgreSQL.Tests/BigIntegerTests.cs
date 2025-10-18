#nullable enable
using System;
using System.Data;
using System.Numerics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests;

public class BigIntegerConverter : NativeValueOrmLiteConverter
{
    public override DbType DbType => DbType.VarNumeric;
    public override string ColumnDefinition => "NUMERIC";
    public override object? ToDbValue(Type fieldType, object value) => value;
    public override object? FromDbValue(Type fieldType, object value)
    {
        if (value == null)
            return null;
        if (value.GetType() == fieldType)
            return value;
        return BigInteger.Parse(value.ToString()!);
    }
}

public class BigIntegerTest
{
    [AutoIncrement]
    public int Id { get; set; }
    public BigInteger Value { get; set; }
}

public class BigIntegerTests
{
    [Test]
    public void Can_use_BigInteger_TypeConverter()
    {
        //using var db = OpenDbConnection();
        var dbFactory = new OrmLiteConnectionFactory("Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200", PostgreSqlDialect.Provider);
        PostgreSqlDialect.Provider.RegisterConverter<BigInteger>(new BigIntegerConverter());
        using var db = dbFactory.Open();

        var test = new BigIntegerTest { Value = 12345678901234567890 };
        db.DropAndCreateTable<BigIntegerTest>();
        db.Insert(test);
        var result = db.SingleById<BigIntegerTest>(1);
        Assert.That(result.Value, Is.EqualTo(test.Value));
    }
}