namespace ServiceStack.OrmLite.Tests;

#region using

using DataAnnotations;
using NUnit.Framework;
using System.Collections.Generic;

#endregion using

public class OrmLiteExecuteNonQueryTests
{
    [TestFixtureOrmLite]
    public class WithDbCmdFilter(DialectContext context) : OrmLiteProvidersTestBase(context)
    {
        [Test]
        public void Can_insert_one_row_and_get_one_affected_row()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name = "Jane Doe";

                var affectedRows = db.ExecuteNonQuery(Dialect != Dialect.Firebird
                    ? "insert into Person (Name) Values (@name);"
                    : "insert into Person (Id, Name) Values (1, @name);", cmd =>
                {
                    cmd.AddParam("name", name);
                });

                var personId = db.Single<Person>(q => q.Name == name).Id;

                var insertedRow = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(insertedRow.Name, Is.EqualTo(name));
            }
        }

        [Test]
        public void Can_insert_multiple_rows_and_get_matching_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name1 = "Jane Doe";
                var name2 = "john Smith";

                int affectedRows = 0;
                if (Dialect != Dialect.Firebird)
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Name)
                        SELECT @name1
                        UNION
                        SELECT @name2", cmd =>
                    {
                        cmd.AddParam("name1", name1);
                        cmd.AddParam("name2", name2);
                    });
                }
                else
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Id, Name)
                        SELECT 1, CAST(@name1 as VARCHAR(128)) FROM RDB$DATABASE
                        UNION
                        SELECT 2, CAST(@name2 as VARCHAR(128)) FROM RDB$DATABASE", cmd =>
                    {
                        cmd.AddParam("name1", name1);
                        cmd.AddParam("name2", name2);
                    });
                }

                var rows = db.SqlColumn<Person>("select * from Person order by name");

                Assert.That(affectedRows, Is.EqualTo(2));
                Assert.That(rows[0].Name, Is.EqualTo(name1));
                Assert.That(rows[1].Name, Is.EqualTo(name2));

                var ids = db.SqlColumn<int>("select Id from Person order by Id");
                Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void Can_update_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var newName = "John Smith";

                var affectedRows = db.ExecuteNonQuery("Update Person Set Name = @newName where Id = @personId", cmd =>
                {
                    cmd.AddParam("personId", personId);
                    cmd.AddParam("newName", newName);
                });

                var updatedPerson = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(updatedPerson.Name, Is.EqualTo(newName));
            }
        }

        [Test]
        public void Can_delete_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var affectedRows = db.ExecuteNonQuery("Delete From Person where Id = @personId", cmd =>
                {
                    cmd.AddParam("personId", personId);
                });

                var count = db.Count<Person>();

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Can_exec_statement_which_performs_no_changes_and_returns_no_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var affectedRows = db.ExecuteNonQuery(@"Delete From Person Where Id = @nonExistingId", cmd =>
                {
                    cmd.AddParam("nonExistingId", -1);
                });

                Assert.That(affectedRows, Is.EqualTo(0));
            }
        }
    }

    [TestFixtureOrmLite]
    public class UsingAnonType : OrmLiteProvidersTestBase
    {
        public UsingAnonType(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_one_row_and_get_one_affected_row()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name = "Jane Doe";

                var affectedRows = db.ExecuteNonQuery(Dialect != Dialect.Firebird 
                    ? "insert into Person (Name) Values (@name);" 
                    : "insert into Person (Id, Name) Values (1, @name);", new { name });

                var personId = db.Single<Person>(q => q.Name == name).Id;

                var insertedRow = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(insertedRow.Name, Is.EqualTo(name));
            }
        }

        [Test]
        public void Can_insert_multiple_rows_and_get_matching_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name1 = "Jane Doe";
                var name2 = "john Smith";

                int affectedRows = 0;
                if (Dialect != Dialect.Firebird)
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Name)
                        SELECT @name1
                        UNION
                        SELECT @name2", new { name1, name2 });
                }
                else
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Id, Name)
                        SELECT 1, CAST(@name1 as VARCHAR(128)) FROM RDB$DATABASE
                        UNION
                        SELECT 2, CAST(@name2 as VARCHAR(128)) FROM RDB$DATABASE", new { name1, name2 });
                }

                var rows = db.SqlColumn<Person>("select * from Person order by name");

                Assert.That(affectedRows, Is.EqualTo(2));
                Assert.That(rows[0].Name, Is.EqualTo(name1));
                Assert.That(rows[1].Name, Is.EqualTo(name2));

                var ids = db.SqlColumn<int>("select Id from Person order by Id");
                Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void Can_update_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person()
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity:true);

                var newName = "John Smith";

                var affectedRows = db.ExecuteNonQuery("Update Person Set Name = @newName where Id = @personId", new
                {
                    personId,
                    newName
                });

                var updatedPerson = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(updatedPerson.Name, Is.EqualTo(newName));
            }
        }

        [Test]
        public void Can_delete_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity:true);

                var affectedRows = db.ExecuteNonQuery("Delete From Person where Id = @personId", new {
                    personId
                });

                var count = db.Count<Person>();

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Can_exec_statement_which_performs_no_changes_and_returns_no_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var affectedRows = db.ExecuteNonQuery(@"Delete From Person Where Id = @nonExistingId", new
                {
                    nonExistingId = -1
                });

                Assert.That(affectedRows, Is.EqualTo(0));
            }
        }
    }

    [TestFixtureOrmLite]
    public class UsingDictionary : OrmLiteProvidersTestBase
    {
        public UsingDictionary(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_one_row_and_get_one_affected_row()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name = "Jane Doe";

                var affectedRows = db.ExecuteNonQuery(Dialect != Dialect.Firebird
                    ? "insert into Person (Name) Values (@name);"
                    : "insert into Person (Id, Name) Values (1, @name);", new Dictionary<string, object> {
                    { "name", name }
                });

                var personId = db.Single<Person>(q => q.Name == name).Id;

                var insertedRow = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(insertedRow.Name, Is.EqualTo(name));
            }
        }

        [Test]
        public void Can_insert_multiple_rows_and_get_matching_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name1 = "Jane Doe";
                var name2 = "john Smith";

                int affectedRows;
                if (Dialect != Dialect.Firebird)
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Name)
                        SELECT @name1
                        UNION
                        SELECT @name2", new Dictionary<string, object> {
                        { "name1", name1 },
                        { "name2", name2 }
                    });
                }
                else
                {
                    affectedRows = db.ExecuteNonQuery(@"
                        INSERT INTO Person (Id, Name)
                        SELECT 1, CAST(@name1 as VARCHAR(128)) FROM RDB$DATABASE
                        UNION
                        SELECT 2, CAST(@name2 as VARCHAR(128)) FROM RDB$DATABASE",
                        new Dictionary<string, object> {
                            { "name1", name1 },
                            { "name2", name2 }
                        });
                }

                var rows = db.SqlColumn<Person>("select * from Person order by name");

                Assert.That(affectedRows, Is.EqualTo(2));
                Assert.That(rows[0].Name, Is.EqualTo(name1));
                Assert.That(rows[1].Name, Is.EqualTo(name2));

                var ids = db.SqlColumn<int>("select Id from Person order by Id");
                Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void Can_update_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person()
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var newName = "John Smith";

                var affectedRows = db.ExecuteNonQuery("Update Person Set Name = @newName where Id = @personId", new Dictionary<string, object>
                {
                    { "personId", personId },
                    { "newName", newName }
                });

                var updatedPerson = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(updatedPerson.Name, Is.EqualTo(newName));
            }
        }

        [Test]
        public void Can_delete_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person()
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var affectedRows = db.ExecuteNonQuery("Delete From Person where Id = @personId", new Dictionary<string, object>
                {
                    { "personId", personId }
                });

                var count = db.Count<Person>();

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Can_exec_statement_which_performs_no_changes_and_returns_no_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var affectedRows = db.ExecuteNonQuery(@"Delete From Person Where Id = @nonExistingId", new Dictionary<string, object>
                {
                    { "nonExistingId", -1 }
                });

                Assert.That(affectedRows, Is.EqualTo(0));
            }
        }
    }

    [TestFixtureOrmLite]
    public class WithoutParams : OrmLiteProvidersTestBase
    {
        public WithoutParams(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_one_row_and_get_one_affected_row()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name = "Jane Doe";

                var affectedRows = db.ExecuteNonQuery(Dialect != Dialect.Firebird
                    ? "insert into Person (Name) Values ('{0}');".Fmt(name)
                    : "insert into Person (Id, Name) Values (1, '{0}');".Fmt(name));

                var personId = Dialect != Dialect.Firebird
                    ? db.LastInsertId()
                    : 1;

                var insertedRow = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(insertedRow.Name, Is.EqualTo(name));
            }
        }

        [Test]
        public void Can_insert_multiple_rows_and_get_matching_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var name1 = "Jane Doe";
                var name2 = "john Smith";

                var sql = Dialect != Dialect.Firebird
                    ? "insert into Person (Name) Select '{0}' Union Select '{1}'".Fmt(name1, name2)
                    : "insert into Person (Id, Name) Select 1, '{0}' FROM RDB$DATABASE Union Select 2, '{1}' FROM RDB$DATABASE"
                        .Fmt(name1, name2);

                var affectedRows = db.ExecuteNonQuery(sql);

                var rows = db.SqlColumn<Person>("select * from Person order by name");

                Assert.That(affectedRows, Is.EqualTo(2));
                Assert.That(rows[0].Name.TrimEnd(), Is.EqualTo(name1));
                Assert.That(rows[1].Name, Is.EqualTo(name2));

                var ids = db.SqlColumn<int>("select Id from Person order by Id");
                Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void Can_update_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person()
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var newName = "John Smith";

                var sql = string.Format(@"Update Person Set Name = '{0}' where Id = {1}", newName, personId);

                var affectedRows = db.ExecuteNonQuery(sql);

                var updatedPerson = db.SingleById<Person>(personId);

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(updatedPerson.Name, Is.EqualTo(newName));
            }
        }

        [Test]
        public void Can_delete_and_returns_appropriate_number_of_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var person = new Person()
                {
                    Name = "Jane Doe"
                };

                var personId = db.Insert(person, selectIdentity: true);

                var sql = string.Format(@"Delete From Person where Id = {0}", personId);

                var affectedRows = db.ExecuteNonQuery(sql);

                var count = db.Count<Person>();

                Assert.That(affectedRows, Is.EqualTo(1));
                Assert.That(count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Can_exec_statement_which_performs_no_changes_and_returns_no_affected_rows()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                var sql = string.Format(@"Delete From Person where Id = {0}", -1);

                var affectedRows = db.ExecuteNonQuery(sql);

                Assert.That(affectedRows, Is.EqualTo(0));
            }
        }
    }

    private class Person
    {
        [AutoIncrement] // Creates Auto primary key
        public int Id { get; set; }

        public string Name { get; set; }
    }
}