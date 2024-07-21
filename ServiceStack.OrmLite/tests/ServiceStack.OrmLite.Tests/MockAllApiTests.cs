// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class MockAllApiTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private IDbConnection db;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        db = base.OpenDbConnection();

        db.DropAndCreateTable<Person>();
        db.InsertAll(Person.Rockstars);
    }

    [OneTimeTearDown]
    public new void TestFixtureTearDown()
    {
        db.Dispose();
    }

    [Test]
    public void Can_mock_all_Select_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   Results = new[] {
                       new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 }
                   },
               })
        {
            Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select(db.From<Person>().Where(x => x.Age > 40))[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("Age > 40")[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age > 40")[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("Age > @age", new { age = 40 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new { age = 40 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("Age > @age", new Dictionary<string, object> { { "age", 40 } })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new Dictionary<string, object> { { "age", 40 } })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Where<Person>("Age", 27)[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Where<Person>(new { Age = 27 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SelectByIds<Person>(new[] { 1, 2, 3 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SelectNonDefaults(new Person { Id = 1 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SelectNonDefaults("Age > @Age", new Person { Age = 40 })[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SelectLazy<Person>().ToList()[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.WhereLazy<Person>(new { Age = 27 }).ToList()[0].FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Select<Person>()[0].FirstName, Is.EqualTo("Mocked"));

            Assert.That(db.Single<Person>(x => x.Age == 42).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single(db.From<Person>().Where(x => x.Age == 42)).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single<Person>(new { Age = 42 }).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single<Person>("Age = @age", new { age = 42 }).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SingleById<Person>(1).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SingleWhere<Person>("Age", 42).FirstName, Is.EqualTo("Mocked"));

            Assert.That(db.Exists<Person>(new { Age = 42 }), Is.True);
            Assert.That(db.Exists<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 42 }), Is.True);
        }
    }

    [Test]
    public void Can_nest_ResultFilters()
    {
        using (new OrmLiteResultsFilter
               {
                   Results = new[] { new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 } }
               })
        {
            Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("Mocked"));

            using (new OrmLiteResultsFilter
                   {
                       Results = new[] { new Person { Id = 1, FirstName = "MockedInner", LastName = "Person", Age = 100 } }
                   })
            {
                Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("MockedInner"));

                using (new OrmLiteResultsFilter
                       {
                           Results = new[] { new Person { Id = 1, FirstName = "MockedInnerInner", LastName = "Person", Age = 100 } }
                       })
                {
                    Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("MockedInnerInner"));
                }

                Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("MockedInner"));
            }

            Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("Mocked"));
        }
    }

    [Test]
    public void Can_mock_Apis_with_FilterFns()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ResultsFn = (dbCmd,type) => new[] { new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 } },
                   SingleResultFn = (dbCmd, type) => new Person { Id = 1, FirstName = "MockedSingle", LastName = "Person", Age = 100 },
                   ScalarResultFn = (dbCmd, type) => 1000,
               })
        {
            Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("Mocked"));

            Assert.That(db.Single<Person>(x => x.Age == 42).FirstName, Is.EqualTo("MockedSingle"));

            Assert.That(db.Scalar<Person, int>(x => Sql.Max(x.Age)), Is.EqualTo(1000));
        }
    }

    [Test]
    public void Can_trace_all_generated_sql()
    {
        var sqlStatements = new List<string>();
        var sqlCommandStatements = new List<SqlCommandDetails>();
        using (new OrmLiteResultsFilter
               {
                   SqlFilter = sql => sqlStatements.Add(sql),
                   SqlCommandFilter = sql => sqlCommandStatements.Add(new SqlCommandDetails(sql)),
                   ResultsFn = (dbCmd, type) => new[] { new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 } },
                   SingleResultFn = (dbCmd, type) => new Person { Id = 1, FirstName = "MockedSingle", LastName = "Person", Age = 100 },
                   ScalarResultFn = (dbCmd, type) => 1000,
               })
        {
            Assert.That(db.Select<Person>(x => x.Age > 40)[0].FirstName, Is.EqualTo("Mocked"));

            Assert.That(db.Single<Person>(x => x.Age == 42).FirstName, Is.EqualTo("MockedSingle"));

            Assert.That(db.Scalar<Person, int>(x => Sql.Max(x.Age)), Is.EqualTo(1000));
                
            Assert.That(sqlStatements.Count, Is.EqualTo(3));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(3));

            sqlStatements.Each(x => x.Print());
            sqlCommandStatements.Each(x => x.PrintDump());
        }
    }

    [Test]
    public void Can_mock_all_Single_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   SingleResult = new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 },
               })
        {
            Assert.That(db.Single<Person>(x => x.Age == 42).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single(db.From<Person>().Where(x => x.Age == 42)).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single<Person>(new { Age = 42 }).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.Single<Person>("Age = @age", new { age = 42 }).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SingleById<Person>(1).FirstName, Is.EqualTo("Mocked"));
            Assert.That(db.SingleWhere<Person>("Age", 42).FirstName, Is.EqualTo("Mocked"));
        }
    }

    [Test]
    public void Can_mock_all_Scalar_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ScalarResult = 1000,
               })
        {
            Assert.That(db.Scalar<Person, int>(x => Sql.Max(x.Age)), Is.EqualTo(1000));
            Assert.That(db.Scalar<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50), Is.EqualTo(1000));
            Assert.That(db.Scalar<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new { age = 40 }), Is.EqualTo(1000));

            Assert.That(db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 }), Is.EqualTo(1000));
            Assert.That(db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } }), Is.EqualTo(1000));
        }
    }

    [Test]
    public void Can_mock_all_Column_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ColumnResults = new[] { "Mock1", "Mock2", "Mock3" },
               })
        {
            Assert.That(db.Column<string>("SELECT LastName FROM Person WHERE Age = @age", new { age = 27 })[0], Is.EqualTo("Mock1"));
            Assert.That(db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 })[0], Is.EqualTo("Mock1"));
            Assert.That(db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } })[0], Is.EqualTo("Mock1"));
        }
    }

    [Test]
    public void Can_mock_all_ColumnDistinct_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ColumnDistinctResults = new[] { 101, 102, 103 },
               })
        {
            Assert.That(db.ColumnDistinct<int>("SELECT Age FROM Person WHERE Age < @age", new { age = 50 }).Count, Is.EqualTo(3));
        }
    }

    [Test]
    public void Can_mock_all_Dictionary_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   DictionaryResults = new Dictionary<int, string> { { 1, "MockValue" } },
               })
        {
            Assert.That(db.Dictionary<int, string>("SELECT Id, LastName FROM Person WHERE Age < @age", new { age = 50 })[1], Is.EqualTo("MockValue"));
        }
    }

    [Test]
    public void Can_mock_all_Update_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ExecuteSqlResult = 10,
               })
        {
            Assert.That(db.Update(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 }), Is.EqualTo(10));
            Assert.That(db.Update(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } }), Is.EqualTo(10));
            Assert.That(db.UpdateAll(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } }), Is.EqualTo(10));
            Assert.That(db.Update(new Person { Id = 1, FirstName = "JJ", Age = 27 }, p => p.LastName == "Hendrix"), Is.EqualTo(10));
            Assert.That(db.Update<Person>(new { FirstName = "JJ" }, p => p.LastName == "Hendrix"), Is.EqualTo(10));
            Assert.That(db.UpdateNonDefaults(new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix"), Is.EqualTo(10));
            Assert.That(db.UpdateOnlyFields(new Person { FirstName = "JJ" }, p => p.FirstName), Is.EqualTo(10));
            Assert.That(db.UpdateOnlyFields(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix"), Is.EqualTo(10));
            Assert.That(db.UpdateOnlyFields(new Person { FirstName = "JJ", LastName = "Hendo" }, db.From<Person>().Update(p => p.FirstName)), Is.EqualTo(10));
            Assert.That(db.UpdateOnlyFields(new Person { FirstName = "JJ" }, db.From<Person>().Update(p => p.FirstName).Where(x => x.FirstName == "Jimi")), Is.EqualTo(10));
        }
    }

    [Test]
    public void Can_mock_all_Delete_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   ExecuteSqlResult = 10,
               })
        {
            Assert.That(db.Delete<Person>(new { FirstName = "Jimi", Age = 27 }), Is.EqualTo(10));
            Assert.That(db.Delete(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 }), Is.EqualTo(10));
            Assert.That(db.DeleteNonDefaults(new Person { FirstName = "Jimi", Age = 27 }), Is.EqualTo(10));
            Assert.That(db.DeleteById<Person>(1), Is.EqualTo(10));
            Assert.That(db.DeleteByIds<Person>(new[] { 1, 2, 3 }), Is.EqualTo(10));
            Assert.That(db.Delete<Person>("Age = @age", new { age = 27 }), Is.EqualTo(10));
            Assert.That(db.Delete(typeof(Person), "Age = @age", new { age = 27 }), Is.EqualTo(10));
            Assert.That(db.Delete<Person>(p => p.Age == 27), Is.EqualTo(10));
            Assert.That(db.Delete(db.From<Person>().Where(p => p.Age == 27)), Is.EqualTo(10));
        }
    }

    [Test]
    public void Can_mock_all_CustomSql_Apis()
    {
        using (new OrmLiteResultsFilter
               {
                   PrintSql = true,
                   Results = new[] { new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 } },
                   SingleResult = new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 },
                   ColumnResults = new[] { "Mocked", "Mocked", "Mocked" },
                   ScalarResult = 1000,
                   ExecuteSqlResult = 10,
               })
        {
            Assert.That(db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 })[0], Is.EqualTo("Mocked"));
            Assert.That(db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } })[0], Is.EqualTo("Mocked"));
            Assert.That(db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 }), Is.EqualTo(1000));
            Assert.That(db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } }), Is.EqualTo(1000));

            Assert.That(db.ExecuteNonQuery("UPDATE Person SET LastName={0} WHERE Id={1}".SqlFmt(DialectProvider, "WaterHouse", 7)), Is.EqualTo(10));
            Assert.That(db.ExecuteNonQuery("UPDATE Person SET LastName=@name WHERE Id=@id", new { name = "WaterHouse", id = 7 }), Is.EqualTo(10));
        }
    }

    [Test]
    public void Can_hijack_all_Insert_Apis()
    {
        //Most INSERT Statements return void. To check each Insert uses the results filter (i.e. instead of the db)
        //we count the number of sql statements generated instead.

        var sqlStatements = new List<string>();
        var sqlCommandStatements = new List<SqlCommandDetails>();
        using (new OrmLiteResultsFilter
               {
                   SqlFilter = sql => sqlStatements.Add(sql),
                   SqlCommandFilter = sql => sqlCommandStatements.Add(new SqlCommandDetails(sql)),
               })
        {
            int i = 0;

            i++; db.Insert(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));


            i++; db.InsertAll(new[] { new Person { Id = 10, FirstName = "Biggie", LastName = "Smalls", Age = 24 } });
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            i++; db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, p => new { p.FirstName, p.Age });
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            sqlStatements.Each(x => x.Print());
            sqlCommandStatements.Each(x => x.PrintDump());

        }
    }

    [Test]
    public void Can_hijack_Save_Apis()
    {
        //Save Statements perform multiple queries. To check each Save uses the results filter (i.e. instead of the db)
        //we count the number of sql statements generated instead.

        var sqlStatements = new List<string>();
        var sqlCommandStatements = new List<SqlCommandDetails>();
        using (new OrmLiteResultsFilter
               {
                   SingleResult = new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 },
                   SqlFilter = sql => sqlStatements.Add(sql),
                   SqlCommandFilter = sql => sqlCommandStatements.Add(new SqlCommandDetails(sql)),
               })
        {
            int i = 0;

            //Force Insert by returning null for existingRow
            using (new OrmLiteResultsFilter {   
                       SqlFilter = sql => sqlStatements.Add(sql),
                       SqlCommandFilter = sql => sqlCommandStatements.Add(new SqlCommandDetails(sql))
                   })
            {
                i += 2; db.Save(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 }); //1 Read + 1 Insert
            }

            i += 2; db.Save(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 }); //1 Read + 1 Update
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            i += 3; db.SaveAll(new[]{ new Person { Id = 14, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
                new Person { Id = 15, FirstName = "Amy", LastName = "Winehouse", Age = 27 } }); //1 Read + 2 Update
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            sqlStatements.Each(x => x.Print());
            sqlCommandStatements.Each(x => x.PrintDump());
        }
    }

    [Test]
    [IgnoreDialect(Tests.Dialect.AnyOracle, "This seems wrong here as the save actually goes through to the database in Oracle to get the next number from the sequence")]
    public void Can_hijack_References_Apis()
    {
        var customer = new Customer
        {
            Id = 1,
            Name = "Customer 1",
            PrimaryAddress = new CustomerAddress
            {
                AddressLine1 = "1 Humpty Street",
                City = "Humpty Doo",
                State = "Northern Territory",
                Country = "Australia"
            },
            Orders = new[] { 
                new Order { LineItem = "Line 1", Qty = 1, Cost = 1.99m },
                new Order { LineItem = "Line 2", Qty = 2, Cost = 2.99m },
            }.ToList(),
        };

        var sqlStatements = new List<string>();
        var sqlCommandStatements = new List<SqlCommandDetails>();
        using (new OrmLiteResultsFilter
               {
                   SqlFilter = sql => sqlStatements.Add(sql),
                   SqlCommandFilter = sql => sqlCommandStatements.Add(new SqlCommandDetails(sql)),
                   SingleResult = customer,
                   RefSingleResultFn = (dbCmd, refType) => customer.PrimaryAddress,
                   RefResultsFn = (dbCmd, refType) => customer.Orders,
               })
        {
            int i = 0;

            i += 2; db.Save(customer);
            Assert.That(sqlStatements.Count, Is.EqualTo(i));

            i += 1; db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            i += 2; db.SaveReferences(customer, customer.Orders);
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            i += 3; var dbCustomer = db.LoadSingleById<Customer>(customer.Id);
            Assert.That(sqlStatements.Count, Is.EqualTo(i));
            Assert.That(sqlCommandStatements.Count, Is.EqualTo(i));

            sqlStatements.Each(x => x.Print());
            sqlCommandStatements.Each(x => x.PrintDump());
        }
    }

    [Test]
    public void Can_mock_stored_procedures()
    {
        using (new OrmLiteResultsFilter {
                   Results = new[] {
                       new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 }
                   },
               })
        {
            Assert.That(db.SqlList<Person>("exec sp_name @firstName, @age",
                    new { firstName = "aName", age = 1 }).First().FirstName,
                Is.EqualTo("Mocked"));
        }

        using (new OrmLiteResultsFilter {
                   ScalarResult = new Person { Id = 1, FirstName = "Mocked", LastName = "Person", Age = 100 },
               })
        {
            Assert.That(db.SqlScalar<Person>("exec sp_name @firstName, @age",
                    new { firstName = "aName", age = 1 }).FirstName,
                Is.EqualTo("Mocked"));
        }
    }
}