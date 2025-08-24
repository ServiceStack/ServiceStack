using System;
using System.Globalization;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteCreateTableWithNamingStrategyTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_create_TableWithNamingStrategy_table_prefix()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
        }
    }

    [Test]
    public void Can_create_TableWithNamingStrategy_table_lowered()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new LowercaseNamingStrategy()))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
        }
    }


    [Test]
    public void Can_create_TableWithNamingStrategy_table_nameUnderscoreCompound()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new UnderscoreSeparatedCompoundNamingStrategy()))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
        }
    }

    [Test]
    public void Can_create_TableWithNamingStrategy_table_aliases()
    {
        var aliasNamingStrategy = new AliasNamingStrategy
        {
            TableAliases = { { "ModelWithOnlyStringFields", "TableAlias" } },
            ColumnAliases = { { "Name", "ColumnAlias" } },
        };

        using (new TemporaryNamingStrategy(DialectProvider, aliasNamingStrategy))
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<ModelWithOnlyStringFields>();

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("CREATE TABLE TableAlias".NormalizeSql()));
            Assert.That(sql, Does.Contain("ColumnAlias".NormalizeSql()));

            var result = db.SqlList<ModelWithIdAndName>(
                $"SELECT * FROM {nameof(ModelWithOnlyStringFields).SqlTable(DialectProvider)} WHERE {"Name".SqlColumn(DialectProvider)} = 'foo'");
                
            Assert.That(db.GetLastSql().NormalizeSql(),
                Is.EqualTo("SELECT * FROM TableAlias WHERE ColumnAlias = 'foo'".NormalizeSql()));

            db.DropTable<ModelWithOnlyStringFields>();

            aliasNamingStrategy.UseNamingStrategy = new LowerCaseUnderscoreNamingStrategy();

            db.CreateTable<ModelWithOnlyStringFields>(true);
            sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("CREATE TABLE table_alias".NormalizeSql()));
            Assert.That(sql, Does.Contain("column_alias".NormalizeSql()));
        }
    }

    [Test]
    public void Can_get_data_from_TableWithNamingStrategy_with_GetById()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields { Id = "999", AlbumId = "112", AlbumName = "ElectroShip", Name = "MyNameIsBatman" };

            db.Save(m);
            var modelFromDb = db.SingleById<ModelWithOnlyStringFields>("999");

            Assert.AreEqual(m.Name, modelFromDb.Name);
        }
    }


    [Test]
    public void Can_get_data_from_TableWithNamingStrategy_with_query_by_example()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

            db.Save(m);
            var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

            Assert.AreEqual(m.Name, modelFromDb.Name);
        }
    }

    [Test]
    public void Can_get_data_from_TableWithUnderscoreSeparatedCompoundNamingStrategy_with_ReadConnectionExtension()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new UnderscoreSeparatedCompoundNamingStrategy()))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields
            {
                Id = "997",
                AlbumId = "112",
                AlbumName = "ElectroShip",
                Name = "ReadConnectionExtensionFirst"
            };
            db.Save(m);
            var modelFromDb = db.Single<ModelWithOnlyStringFields>(x => x.Name == "ReadConnectionExtensionFirst");
            Assert.AreEqual(m.AlbumName, modelFromDb.AlbumName);
        }
    }

    [Test]
    public void Can_get_data_from_TableWithNamingStrategy_AfterChangingNamingStrategy()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

            db.Save(m);
            var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

            Assert.AreEqual(m.Name, modelFromDb.Name);

            modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
            Assert.AreEqual(m.Name, modelFromDb.Name);
        }

        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

            db.Save(m);
            var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

            Assert.AreEqual(m.Name, modelFromDb.Name);

            modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
            Assert.AreEqual(m.Name, modelFromDb.Name);
        }

        using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithOnlyStringFields>(true);
            var m = new ModelWithOnlyStringFields { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

            db.Save(m);
            var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

            Assert.AreEqual(m.Name, modelFromDb.Name);

            modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
            Assert.AreEqual(m.Name, modelFromDb.Name);
        }
    }
    
    public class Thread
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(AppUser))]
        public string UserId { get; set; }
        
        public string ThreadName { get; set; }
    }

    [Alias("AspNetUsers")]
    public class AppUser
    {
        [Alias("Id")]
        public string Id { get; set; }
    }

    [Test]
    public void Can_create_table_with_FK_alias()
    {
        using (new TemporaryNamingStrategy(DialectProvider, new LowercaseNamingStrategy()))
        {
            using var db = OpenDbConnection();
            db.DropTable<Thread>();
            db.DropTable<AppUser>();
            Assert.That(db.GetLastSql(), Does.Contain("AspNetUsers"));
            
            db.CreateTable<AppUser>();
            Assert.That(db.GetLastSql(), Does.Contain("AspNetUsers"));
            db.CreateTable<Thread>();
            Assert.That(db.GetLastSql(), Does.Contain("AspNetUsers"));
            Assert.That(db.GetLastSql().Replace('`','"'), Does.Contain("(\"Id\")"));
        }
    }
}

internal class PrefixNamingStrategy : OrmLiteNamingStrategyBase
{

    public string TablePrefix { get; set; }

    public string ColumnPrefix { get; set; }

    public override string GetTableName(string name)
    {
        return TablePrefix + name;
    }

    public override string GetColumnName(string name)
    {
        return ColumnPrefix + name;
    }

}

internal class LowercaseNamingStrategy : OrmLiteNamingStrategyBase
{
    public bool IgnoreAlias { get; set; } = true;
    public override string GetAlias(string name) => IgnoreAlias 
        ? name 
        : name.ToLowercaseUnderscore();
        
    public override string GetSchemaName(string name) => name == null ? null 
        : SchemaAliases.TryGetValue(name, out var alias) 
            ? alias 
            : name.ToLowercaseUnderscore();

    public override string GetTableName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToLowercaseUnderscore();

    public override string GetColumnName(string name) => ColumnAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name.ToLowercaseUnderscore();
}

internal class UnderscoreSeparatedCompoundNamingStrategy : OrmLiteNamingStrategyBase
{

    public override string GetTableName(string name)
    {
        return toUnderscoreSeparatedCompound(name);
    }

    public override string GetColumnName(string name)
    {
        return toUnderscoreSeparatedCompound(name);
    }


    string toUnderscoreSeparatedCompound(string name)
    {
#if NETCORE
            string r = char.ToLower(name[0]).ToString();
#else
        string r = char.ToLower(name[0]).ToString(CultureInfo.InvariantCulture);
#endif
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                r += "_";
                r += char.ToLower(name[i]);
            }
            else
            {
                r += name[i];
            }
        }
        return r;
    }

}

internal class LowerCaseUnderscoreNamingStrategy : OrmLiteNamingStrategyBase
{
    public override string GetTableName(string name)
    {
        return name.ToLowercaseUnderscore();
    }

    public override string GetColumnName(string name)
    {
        return name.ToLowercaseUnderscore();
    }
}