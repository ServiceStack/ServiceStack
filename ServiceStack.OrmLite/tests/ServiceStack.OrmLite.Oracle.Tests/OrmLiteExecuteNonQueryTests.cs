namespace ServiceStack.OrmLite.Tests
{
    #region using

    using DataAnnotations;
    using NUnit.Framework;
    using System.Collections.Generic;

    #endregion using

    [TestFixture]
    public class OrmLiteExecuteNonQueryTests
    {
        public class UsingAnonType : OrmLiteTestBase
        {
            [Test]
            public void Can_insert_one_row_and_get_one_affected_row()
            {
                SuppressIfOracle("Need trigger for autoincrement keys to work in Oracle with caller supplied SQL");

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<Person>();

                    var name = "Jane Doe";

                    var affectedRows = db.ExecuteNonQuery("insert into Person (Name) Values (:name)", new {
                        name
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
                SuppressIfOracle("Oracle does not accept 'select x', requiring 'select x from dual'");

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<Person>();

                    var name1 = "Jane Doe";
                    var name2 = "john Smith";

                    var affectedRows = db.ExecuteNonQuery(@"
                                                insert into Person (Name)
                                                Select :name1
                                                Union
                                                Select :name2", new
                    {
                        name1,
                        name2
                    });

                    var rows = db.SqlColumn<Person>("select * from Person order by name");

                    Assert.That(affectedRows, Is.EqualTo(2));
                    Assert.That(rows[0].Name, Is.EqualTo(name1));
                    Assert.That(rows[1].Name, Is.EqualTo(name2));
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

                    var affectedRows = db.ExecuteNonQuery("Update Person Set Name = :newName where Id = :personId", new
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

                    var affectedRows = db.ExecuteNonQuery("Delete From Person where Id = :personId", new {
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

                    var affectedRows = db.ExecuteNonQuery(@"Delete From Person Where Id = :nonExistingId", new
                    {
                        nonExistingId = -1
                    });

                    Assert.That(affectedRows, Is.EqualTo(0));
                }
            }
        }

        public class UsingDictionary : OrmLiteTestBase
        {
            [Test]
            public void Can_insert_one_row_and_get_one_affected_row()
            {
                SuppressIfOracle("Need trigger for autoincrement keys to work in Oracle with caller supplied SQL");

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<Person>();

                    var name = "Jane Doe";

                    var affectedRows = db.ExecuteNonQuery("insert into Person (Name) Values (:name)", new Dictionary<string, object>
                    {
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
                    SuppressIfOracle("Oracle does not accept 'select x', requiring 'select x from dual'");

                    db.DropAndCreateTable<Person>();

                    var name1 = "Jane Doe";
                    var name2 = "john Smith";

                    var affectedRows = db.ExecuteNonQuery(@"
                                                insert into Person (Name)
                                                Select :name1
                                                Union
                                                Select :name2", new Dictionary<string, object>
                    {
                        { "name1", name1 },
                        { "name2", name2 }
                    });

                    var rows = db.SqlColumn<Person>("select * from Person order by name");

                    Assert.That(affectedRows, Is.EqualTo(2));
                    Assert.That(rows[0].Name, Is.EqualTo(name1));
                    Assert.That(rows[1].Name, Is.EqualTo(name2));
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

                    var affectedRows = db.ExecuteNonQuery("Update Person Set Name = :newName where Id = :personId", new Dictionary<string, object>
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

                    var affectedRows = db.ExecuteNonQuery("Delete From Person where Id = :personId", new Dictionary<string, object>
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

                    var affectedRows = db.ExecuteNonQuery(@"Delete From Person Where Id = :nonExistingId", new Dictionary<string, object>
                    {
                        { "nonExistingId", -1 }
                    });

                    Assert.That(affectedRows, Is.EqualTo(0));
                }
            }
        }

        public class WithoutParams : OrmLiteTestBase
        {
            [Test]
            public void Can_insert_one_row_and_get_one_affected_row()
            {
                SuppressIfOracle("Need trigger for autoincrement keys to work in Oracle with caller supplied SQL");

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<Person>();

                    var name = "Jane Doe";

                    var sql = string.Format("insert into Person (Name) Values ('{0}')", name);

                    var affectedRows = db.ExecuteNonQuery(sql);

                    var personId = db.LastInsertId();

                    var insertedRow = db.SingleById<Person>(personId);

                    Assert.That(affectedRows, Is.EqualTo(1));
                    Assert.That(insertedRow.Name, Is.EqualTo(name));
                }
            }

            [Test]
            public void Can_insert_multiple_rows_and_get_matching_number_of_affected_rows()
            {
                SuppressIfOracle("Oracle does not accept 'select x', requiring 'select x from dual'");

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<Person>();

                    var name1 = "Jane Doe";
                    var name2 = "john Smith";

                    var sql = string.Format(@"insert into Person (Name) Select '{0}' Union Select '{1}'", name1, name2);

                    var affectedRows = db.ExecuteNonQuery(sql);

                    var rows = db.SqlColumn<Person>("select * from Person order by name");

                    Assert.That(affectedRows, Is.EqualTo(2));
                    Assert.That(rows[0].Name, Is.EqualTo(name1));
                    Assert.That(rows[1].Name, Is.EqualTo(name2));
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
}