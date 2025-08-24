using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class BulkSqlInsertTests : OrmLiteTestBase
{
    private List<Person> People = 15001.Times(i => new Person
        { Id = i + 1, Age = (10 + i) % 90, FirstName = $"First{i+1}", LastName = $"Last{i+1}" });

    [Test]
    public void Can_InsertRowsSql()
    {
        if (Dialect is Dialect.Firebird or Dialect.Firebird4) //Needs to run in batch
            return;
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Person>();
        
        var sql = db.Dialect().ToInsertRowsSql(Person.Rockstars);
        sql.Print();
        db.ExecuteSql(sql);
        
        var rows = db.Select<Person>();
        Assert.That(rows.Count, Is.EqualTo(Person.Rockstars.Length));
    }

    [Test]
    public void Can_BulkInsert_Csv()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Person>();

        db.BulkInsert(People, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Csv,
        });

        var rowsCount = db.Count<Person>();
        Assert.That(rowsCount, Is.EqualTo(People.Count));
    }

    [Test]
    public void Can_BulkInsert_Sql()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Person>();

        db.BulkInsert(People, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Sql,
        });

        var rowsCount = db.Count<Person>();
        Assert.That(rowsCount, Is.EqualTo(People.Count));
    }

    [Test]
    public void Can_BulkInsert_Csv_AllNumbers()
    {
        var rows = 1.Times(i => AllNumbers.Create(i + 1));
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<AllNumbers>();
        
        db.BulkInsert(rows, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Csv,
        });

        var dbRows = db.Select<AllNumbers>();
        dbRows.PrintDump();
        Assert.That(dbRows.Count, Is.EqualTo(rows.Count));
    }

    [Test]
    public void Can_BulkInsert_Csv_AllTypes()
    {
        var rows = 3.Times(i => AllTypes.Create(i + 1));
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<AllTypes>();
        
        db.BulkInsert(rows, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Csv,
        });

        var dbRows = db.Select<AllTypes>();
        dbRows.PrintDump();
        Assert.That(dbRows.Count, Is.EqualTo(rows.Count));
    }

    [Test]
    public void Can_BulkInsert_Csv_AllTypes_nulls()
    {
        // PostgreSQL can't store \0 char
        var rows = new[] { 
            new AllTypes {
                Char = 'A',
                DateTime = DateTime.UtcNow,
            } 
        }.ToList();
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<AllTypes>();
        
        db.BulkInsert(rows, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Csv,
        });

        var dbRows = db.Select<AllTypes>();
        dbRows.PrintDump();
        Assert.That(dbRows.Count, Is.EqualTo(rows.Count));
    }

    [Test]
    public void Can_BulkInsert_PersonWithAutoId()
    {
        var rows = 3.Times(i => new PersonWithAutoId
        {
            FirstName = "First" + i,
            LastName = "Last" + i,
            Age = i + 13 % 100
        });
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<PersonWithAutoId>();

        db.BulkInsert(rows, new BulkInsertConfig
        {
            Mode = BulkInsertMode.Sql,
        });

        var dbRows = db.Select<PersonWithAutoId>();
        Assert.That(dbRows.Count, Is.EqualTo(rows.Count));
    }
}
