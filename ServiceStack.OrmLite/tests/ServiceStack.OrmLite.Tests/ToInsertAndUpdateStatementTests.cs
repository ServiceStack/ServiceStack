using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ToInsertAndUpdateStatementTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_use_ToUpdateStatement_to_generate_inline_SQL()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            var row = db.SingleById<Person>(1);
            row.Age = 42;

            var sql = db.ToUpdateStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            var updatedRow = db.SingleById<Person>(1);
            Assert.That(updatedRow.Equals(row));
        }
    }

    [Test]
    public void Can_use_ToInsertStatement_to_generate_inline_SQL()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Person>();
            var row = Person.Rockstars[0];

            var sql = db.ToInsertStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            var insertedRow = db.SingleById<Person>(row.Id);
            Assert.That(insertedRow.Equals(row));
        }
    }

    [Test]
    public void Correct_DbNull_in_ToInsertStatement()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<PersonWithReferenceType>();
            var row = PersonWithReferenceType.TestValues[0];

            var sql = db.ToInsertStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            var insertedRow = db.SingleById<PersonWithReferenceType>(row.Id);
            Assert.That(insertedRow.Equals(row));
        }
    }
        
    [Test]
    public void Correct_Ref_in_ToInsertStatement()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<PersonWithReferenceType>();
            var row = PersonWithReferenceType.TestValues[1];

            var sql = db.ToInsertStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            var insertedRow = db.SingleById<PersonWithReferenceType>(row.Id);
            Assert.That(insertedRow.Equals(row));
        }
    }
        
    [Test]
    public void Correct_nullable_in_ToInsertStatement()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<TestProduct>();
            var row = TestProduct.TestValues[0];

            var sql = db.ToInsertStatement(row);
            sql.Print();
            db.ExecuteSql(sql);

            var insertedRow = db.SingleById<TestProduct>(row.Id);
            Assert.AreEqual(insertedRow, row);
        }
    }
}