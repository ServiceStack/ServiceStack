using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ServiceCollectionTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
    }

    public class MyTable
    {
        public int Id { get; set; }
    }

    [Test]
    public void Can_configure_table_aliases()
    {
        var services = new ServiceCollection();
        services.AddOrmLite(options => options.UsePostgres("", dialect => {
            dialect.NamingStrategy.TableAliases["MyTable"] = "my_table";
            dialect.NamingStrategy.SchemaAliases["MySchema"] = "my_schema";
            dialect.NamingStrategy.ColumnAliases["MyColumn"] = "my_column";
        }));

        var provider = services.BuildServiceProvider();
        var dbFactory = provider.GetService<IDbConnectionFactory>();
        var dialect = dbFactory.GetDialectProvider();
        
        Assert.That(dialect.GetQuotedTableName("MyTable"), Is.EqualTo("\"my_table\""));
        Assert.That(dialect.QuoteTable(new("MySchema","MyTable")), 
            Is.EqualTo("\"my_schema\".\"my_table\""));
        Assert.That(dialect.GetQuotedColumnName("MyColumn"), Is.EqualTo("\"my_column\""));

        Assert.That(dialect.QuoteTable(new(typeof(MyTable))), Is.EqualTo("\"my_table\""));
        Assert.That(dialect.QuoteTable(new(ModelDefinition<MyTable>.Definition)), Is.EqualTo("\"my_table\""));
        Assert.That(dialect.QuoteTable(new("MyTable")), Is.EqualTo("\"my_table\""));
        Assert.That(dialect.QuoteTable(new("MySchema", "MyTable")), Is.EqualTo("\"my_schema\".\"my_table\""));
        Assert.That(dialect.QuoteTable(TableRef.Literal("\"MyTable\"")), Is.EqualTo("\"MyTable\""));
        Assert.That(dialect.QuoteTable("MyTable"), Is.EqualTo("\"my_table\""));
    }
    
    [Test]
    public void Can_configure_OrmLite_with_ServiceCollection_Extensions()
    {
        var services = new ServiceCollection();
        
        services.AddOrmLite(options => {
                options.UseSqlite(":memory:", dialect => {
                        dialect.NamingStrategy.TableAliases["MyTable"] = "my_table";
                        dialect.NamingStrategy.SchemaAliases["MySchema"] = "my_schema";
                        dialect.NamingStrategy.ColumnAliases["MyColumn"] = "my_column";
                    })
                    .ConfigureJson(json => {
                        json.DefaultSerializer = JsonSerializerType.ServiceStackJson;
                        json.JsonObjectTypes.Add(typeof(object));
                        json.ServiceStackJsonTypes.Add(typeof(ConcurrentDictionary<string,object>));
                        json.SystemJsonTypes.Add(typeof(ConcurrentBag<object>));
                    });
            })
            .AddSqlite("db1", "db1.sqlite")
            .AddSqlite("db2", "db2.sqlite")
            .AddPostgres("postgres", PostgreSqlDb.DefaultConnection)
            .AddSqlServer("sqlserver", SqlServerDb.DefaultConnection)
            .AddSqlServer<SqlServer.SqlServer2016OrmLiteDialectProvider>("sqlserver2016", SqlServerDb.DefaultConnection)
            .AddMySql("mysql", MySqlDb.DefaultConnection)
            .AddMySqlConnector("mysqlconnector", MySqlDb.DefaultConnection)
            .AddOracle("oracle", OracleDb.DefaultConnection)
            .AddFirebird("firebird", FirebirdDb.DefaultConnection);
        
        var provider = services.BuildServiceProvider();
        var dbFactory = provider.GetService<IDbConnectionFactory>();
        Assert.That(dbFactory, Is.Not.Null);
        
        using var app = dbFactory.Open();
        Assert.That(app.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        using var db1 = dbFactory.Open("db1");
        Assert.That(db1.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        using var db2 = dbFactory.Open("db2");
        Assert.That(db2.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));

        if (Dialect.HasFlag(Dialect.SqlServer))
        {
            using var sqlServer = dbFactory.Open("sqlserver");
            Assert.That(sqlServer.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
            using var sqlServer2016 = dbFactory.Open("sqlserver2016");
            Assert.That(sqlServer.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.AnyPostgreSql))
        {
            using var postgres = dbFactory.Open("postgres");
            Assert.That(postgres.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.MySql))
        {
            using var mysql = dbFactory.Open("mysql");
            Assert.That(mysql.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.MySqlConnector))
        {
            using var mysql = dbFactory.Open("mysqlconnector");
            Assert.That(mysql.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
    }

    class JsonObjectTypes
    {
        public object Object { get; set; }
        public Dictionary<string, object> ObjectDictionary { get; set; }
        public List<object> ObjectList { get; set; }
    }

    [Test]
    public void Can_serialize_Complex_Types_with_JsonObject()
    {
        var serializer = new JsonComplexTypeSerializer();
        var person = new Person { Id = 1, FirstName = "FirstName", LastName = "LastName", Age = 27 };
        var dto = new JsonObjectTypes
        {
            Object = person,
            ObjectDictionary = new()
            {
                ["Id"] = 1,
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName",
                ["Age"] = 27,
            },
        };
        dto.ObjectList = [dto.ObjectDictionary];

        var json = serializer.SerializeToString(dto.Object);
        var dtoObject = serializer.DeserializeFromString<Person>(json);
        Assert.That(dtoObject, Is.EqualTo(dto.Object));

        json = serializer.SerializeToString(dto.ObjectDictionary);
        var dtoObjectDictionary = serializer.DeserializeFromString<Dictionary<string, object>>(json);
        Assert.That(dtoObjectDictionary, Is.EqualTo(dto.ObjectDictionary));
        
        var fromObjDict = dtoObjectDictionary.FromObjectDictionary<Person>();
        Assert.That(fromObjDict, Is.EqualTo(person));
        
        json = serializer.SerializeToString(dto.ObjectList);
        var fromObjList = serializer.DeserializeFromString<List<object>>(json);
        Assert.That(fromObjList, Is.EqualTo(dto.ObjectList));
    }
    
    class PersonJsonObjectTypes
    {
        public Person Person { get; set; }
        public List<Person> PersonList { get; set; }
        public Dictionary<int,Person> PersonDictionary { get; set; }
    }

    [Test]
    public void Can_serialize_typed_Complex_Types_with_JsonObject()
    {
        var serializer = new JsonComplexTypeSerializer
        {
            DefaultSerializer = JsonSerializerType.JsonObject
        };
        var person = new Person { Id = 1, FirstName = "FirstName", LastName = "LastName", Age = 27 };
 
        var dto = new PersonJsonObjectTypes
        {
            Person = person,
            PersonList = [person],
            PersonDictionary = new()
            {
                [person.Id] = person,
            }
        };

        var json = serializer.SerializeToString(dto.Person);
        var dtoObject = serializer.DeserializeFromString<Person>(json);
        Assert.That(dtoObject, Is.EqualTo(dto.Person));

        json = serializer.SerializeToString(dto.PersonList);
        var fromObjList = serializer.DeserializeFromString<List<Person>>(json);
        Assert.That(fromObjList, Is.EqualTo(dto.PersonList));

        json = serializer.SerializeToString(dto.PersonDictionary);
        var fromPersonList = serializer.DeserializeFromString<Dictionary<int,Person>>(json);
        Assert.That(fromPersonList, Is.EqualTo(dto.PersonDictionary));
    }
}