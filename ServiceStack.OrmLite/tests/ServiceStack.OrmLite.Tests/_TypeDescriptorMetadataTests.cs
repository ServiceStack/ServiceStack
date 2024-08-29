using System;
using System.Data;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

// Needs to be run first
[TestFixtureOrmLite]
public class _TypeDescriptorMetadataTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_add_AutoIncrement_Id_at_runtime()
    {
        using var db = OpenDbConnection();
        var model = new PersonDescriptor {FirstName = "Jimi", LastName = "Hendrix", Age = 27};

        typeof(PersonDescriptor).GetProperty("Id")
            .AddAttributes(new AutoIncrementAttribute());

        db.DropAndCreateTable<PersonDescriptor>();

        var oldRows = db.Select<PersonDescriptor>();

        db.Insert(model);
        db.Insert(model);
        model.Id = 0; // Oracle provider currently updates the id field so force it back to get an insert operation
        db.Save(model);

        var allRows = db.Select<PersonDescriptor>();
        Assert.That(allRows.Count - oldRows.Count, Is.EqualTo(3));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyOracle, "Test assert fails with Oracle because Oracle does not allow 64000 character fields and uses VARCHAR2 not VARCHAR")]
    [IgnoreDialect(Dialect.AnyPostgreSql, "Uses 'text' for strings by default")]
    public void Can_change_column_definition()
    {
        using var db = OpenDbConnection();
        typeof(DynamicCacheEntry)
            .GetProperty("Data")
            .AddAttributes(new StringLengthAttribute(7000));

        db.DropAndCreateTable<DynamicCacheEntry>();

        Assert.That(db.GetLastSql().NormalizeSql(),
            Does.Contain("Data VARCHAR(7000)".NormalizeSql()));
        db.GetLastSql().Print();
    }

    [Test]
    public void Can_Create_Table_with_MaxText_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CacheEntry>();

        var sql = db.GetLastSql();
        sql.Print();

        if (Dialect == Dialect.Sqlite)
        {
            Assert.That(sql, Does.Contain(" VARCHAR(1000000)"));
        }
        else if (Dialect == Dialect.AnyPostgreSql)
        {
            Assert.That(sql, Does.Contain(" TEXT"));
        }
        else if (Dialect == Dialect.AnyMySql)
        {
            Assert.That(sql, Does.Contain(" LONGTEXT"));
        }
        else if (Dialect == Dialect.AnyOracle)
        {
            Assert.That(sql, Does.Contain(" VARCHAR2(4000)"));
        }
        else if (Dialect == Dialect.SqlServer)
        {
            Assert.That(sql, Does.Contain(" VARCHAR(MAX)"));
        }
    }

    [Test]
    public void Can_Create_Table_with_MaxText_column_Unicode()
    {
        using var db = OpenDbConnection();
        var stringConverter = DialectProvider.GetStringConverter();
        var hold = stringConverter.UseUnicode;
        stringConverter.UseUnicode = true;

        try
        {
            db.DropAndCreateTable<CacheEntry>();
        }
        catch (Exception)
        {
            db.DropAndCreateTable<CacheEntry>();
        }
        finally
        {
            stringConverter.UseUnicode = hold;
        }

        var sql = db.GetLastSql();
        sql.Print();

        if (Dialect.Sqlite.HasFlag(Dialect))
        {
            Assert.That(sql, Does.Contain(" NVARCHAR(1000000)"));
        }
        else if (Dialect.AnyPostgreSql.HasFlag(Dialect))
        {
            Assert.That(sql, Does.Contain(" TEXT"));
        }
        else if (Dialect.AnyMySql.HasFlag(Dialect))
        {
            Assert.That(sql, Does.Contain(" LONGTEXT"));
        }
        else if (Dialect.AnyOracle.HasFlag(Dialect))
        {
            Assert.That(sql, Does.Contain(" NVARCHAR2(4000)"));
        }
        else if (Dialect.Firebird.HasFlag(Dialect))
        {
            Assert.That(sql, Does.Contain(" VARCHAR(10000)"));
        }
        else
        {
            Assert.That(sql, Does.Contain(" NVARCHAR(MAX)"));
        }
    }

    [Test]
    public void Does_save_cache_data_when_data_exceeds_StringConverter_max_size()
    {
        IDbDataParameter GetParam(IDbCommand cmd, string name)
        {
            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                if (cmd.Parameters[i] is IDbDataParameter p 
                    && p.ParameterName.Substring(1).EqualsIgnoreCase(name))
                {
                    return p;
                }
            }
            return null;
        }

        using var db = OpenDbConnection();
        var stringConverter = db.GetDialectProvider().GetStringConverter();
        var hold = stringConverter.StringLength;
        stringConverter.StringLength = 255;

        try
        {
            db.DropAndCreateTable<CacheEntry>();
        }
        catch (Exception)
        {
            db.DropAndCreateTable<CacheEntry>();
        }

        var sb = new StringBuilder();
        30.Times(i => sb.Append("0123456789"));
        Assert.That(sb.Length, Is.EqualTo(300));

        var id = "key";

        var original = new CacheEntry {
            Id = id,
            Data = sb.ToString(), 
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
        };
        db.Insert(original, cmd => {
            var idParam = GetParam(cmd, nameof(CacheEntry.Id));
            var dataParam = GetParam(cmd, nameof(CacheEntry.Data));
                    
            //MySql auto sets param size based on value
            Assert.That(idParam.Size, Is.EqualTo(stringConverter.StringLength)
                .Or.EqualTo(Math.Min((idParam.Value as string).Length, stringConverter.StringLength))
            ); 
            Assert.That(dataParam.Size, Is.EqualTo(300)
                .Or.EqualTo(Math.Min((dataParam.Value as string).Length, stringConverter.StringLength))
            );
        });

        var key = db.SingleById<CacheEntry>(id);
        Assert.That(key.Data, Is.EqualTo(original.Data));

        var updatedData = key.Data + "0123456789";
                
        var exists = db.UpdateOnlyFields(new CacheEntry
            {
                Id = id,
                Data = updatedData,
                ModifiedDate = DateTime.UtcNow,
            },
            onlyFields: q => new { q.Data, q.ModifiedDate },
            where: q => q.Id == id, 
            cmd => {
                var idParam = cmd.Parameters[0] as IDbDataParameter;
                var dataParam = cmd.Parameters[1] as IDbDataParameter;
                    
                //MySql auto sets param size based on value
                Assert.That(idParam.Size, Is.EqualTo(stringConverter.StringLength)
                    .Or.EqualTo(Math.Min((idParam.Value as string).Length, stringConverter.StringLength))
                ); 
                Assert.That(dataParam.Size, Is.EqualTo(310)
                    .Or.EqualTo(Math.Min((dataParam.Value as string).Length, stringConverter.StringLength))
                );
            }) == 1;
                
        Assert.That(exists);
                
        key = db.SingleById<CacheEntry>(id);
        Assert.That(key.Data, Is.EqualTo(updatedData));
                
        stringConverter.StringLength = hold;
    }
}