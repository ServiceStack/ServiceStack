using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteGetScalarTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_get_scalar_value()
    {
        List<Author> authors = new List<Author>();
        authors.Add(new Author() { Name = "Demis Bellot", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 99.9m, Comments = "CSharp books", Rate = 10, City = "London", FloatProperty = 10.25f, DoubleProperty = 3.23 });
        authors.Add(new Author() { Name = "Angel Colmenares", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 50.0m, Comments = "CSharp books", Rate = 5, City = "Bogota", FloatProperty = 7.59f, DoubleProperty = 4.23 });
        authors.Add(new Author() { Name = "Adam Witco", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 80.0m, Comments = "Math Books", Rate = 9, City = "London", FloatProperty = 15.5f, DoubleProperty = 5.42 });
        authors.Add(new Author() { Name = "Claudia Espinel", Birthday = DateTime.Today.AddYears(-23), Active = true, Earnings = 60.0m, Comments = "Cooking books", Rate = 10, City = "Bogota", FloatProperty = 0.57f, DoubleProperty = 8.76 });
        authors.Add(new Author() { Name = "Libardo Pajaro", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 80.0m, Comments = "CSharp books", Rate = 9, City = "Bogota", FloatProperty = 8.43f, DoubleProperty = 7.35 });
        authors.Add(new Author() { Name = "Jorge Garzon", Birthday = DateTime.Today.AddYears(-28), Active = true, Earnings = 70.0m, Comments = "CSharp books", Rate = 9, City = "Bogota", FloatProperty = 1.25f, DoubleProperty = 0.3652 });
        authors.Add(new Author() { Name = "Alejandro Isaza", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 70.0m, Comments = "Java books", Rate = 0, City = "Bogota", FloatProperty = 1.5f, DoubleProperty = 100.563 });
        authors.Add(new Author() { Name = "Wilmer Agamez", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 30.0m, Comments = "Java books", Rate = 0, City = "Cartagena", FloatProperty = 3.5f, DoubleProperty = 7.23 });
        authors.Add(new Author() { Name = "Rodger Contreras", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 90.0m, Comments = "CSharp books", Rate = 8, City = "Cartagena", FloatProperty = 0.25f, DoubleProperty = 9.23 });
        authors.Add(new Author() { Name = "Chuck Benedict", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "CSharp books", Rate = 8, City = "London", FloatProperty = 9.95f, DoubleProperty = 4.91 });
        authors.Add(new Author() { Name = "James Benedict II", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "Java books", Rate = 5, City = "Berlin", FloatProperty = 4.44f, DoubleProperty = 6.41 });
        authors.Add(new Author() { Name = "Ethan Brown", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 45.0m, Comments = "CSharp books", Rate = 5, City = "Madrid", FloatProperty = 6.67f, DoubleProperty = 8.05 });
        authors.Add(new Author() { Name = "Xavi Garzon", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 75.0m, Comments = "CSharp books", Rate = 9, City = "Madrid", FloatProperty = 1.25f, DoubleProperty = 3.99 });
        authors.Add(new Author()
        {
            Name = "Luis garzon",
            Birthday = DateTime.Today.AddYears(-22),
            Active = true,
            Earnings = 85.0m,
            Comments = "CSharp books",
            Rate = 10,
            City = "Mexico",
            LastActivity = DateTime.Today,
            NRate = 5,
            FloatProperty = 1.25f,
            NFloatProperty = 3.15f,
            DoubleProperty = 1.25,
            NDoubleProperty = 8.25
        });

        using (var db = OpenDbConnection())
        {
            db.CreateTable<Author>(true);
            db.DeleteAll<Author>();

            db.InsertAll(authors);

            var expectedDate = authors.Max(e => e.Birthday);
            var r1 = db.Scalar<Author, DateTime>(e => Sql.Max(e.Birthday));
            Assert.That(expectedDate, Is.EqualTo(r1));

            expectedDate = authors.Where(e => e.City == "London").Max(e => e.Birthday);
            r1 = db.Scalar<Author, DateTime>(e => Sql.Max(e.Birthday), e => e.City == "London");
            Assert.That(expectedDate, Is.EqualTo(r1));

            r1 = db.Scalar<Author, DateTime>(e => Sql.Max(e.Birthday), e => e.City == "SinCity");
            Assert.That(default(DateTime), Is.EqualTo(r1));


            var expectedNullableDate = authors.Max(e => e.LastActivity);
            DateTime? r2 = db.Scalar<Author, DateTime?>(e => Sql.Max(e.LastActivity));
            Assert.That(expectedNullableDate, Is.EqualTo(r2));

            expectedNullableDate = authors.Where(e => e.City == "Bogota").Max(e => e.LastActivity);
            r2 = db.Scalar<Author, DateTime?>(
                e => Sql.Max(e.LastActivity),
                e => e.City == "Bogota");
            Assert.That(r2, Is.EqualTo(expectedNullableDate));

            r2 = db.Scalar<Author, DateTime?>(e => Sql.Max(e.LastActivity), e => e.City == "SinCity");
            Assert.That(default(DateTime?), Is.EqualTo(r2));


            var expectedDecimal = authors.Max(e => e.Earnings);
            decimal r3 = db.Scalar<Author, decimal>(e => Sql.Max(e.Earnings));
            Assert.That(expectedDecimal, Is.EqualTo(r3));

            expectedDecimal = authors.Where(e => e.City == "London").Max(e => e.Earnings);
            r3 = db.Scalar<Author, decimal>(e => Sql.Max(e.Earnings), e => e.City == "London");
            Assert.That(expectedDecimal, Is.EqualTo(r3));

            r3 = db.Scalar<Author, decimal>(e => Sql.Max(e.Earnings), e => e.City == "SinCity");
            Assert.That(default(decimal), Is.EqualTo(r3));


            var expectedNullableDecimal = authors.Max(e => e.NEarnings);
            decimal? r4 = db.Scalar<Author, decimal?>(e => Sql.Max(e.NEarnings));
            Assert.That(expectedNullableDecimal, Is.EqualTo(r4));

            expectedNullableDecimal = authors.Where(e => e.City == "London").Max(e => e.NEarnings);
            r4 = db.Scalar<Author, decimal?>(e => Sql.Max(e.NEarnings), e => e.City == "London");
            Assert.That(expectedNullableDecimal, Is.EqualTo(r4));

            r4 = db.Scalar<Author, decimal?>(e => Sql.Max(e.NEarnings), e => e.City == "SinCity");
            Assert.That(default(decimal?), Is.EqualTo(r4));


            var expectedDouble = authors.Max(e => e.DoubleProperty);
            double r5 = db.Scalar<Author, double>(e => Sql.Max(e.DoubleProperty));
            Assert.That(expectedDouble, Is.EqualTo(r5).Within(.1d));

            expectedDouble = authors.Where(e => e.City == "London").Max(e => e.DoubleProperty);
            r5 = db.Scalar<Author, double>(e => Sql.Max(e.DoubleProperty), e => e.City == "London");
            Assert.That(expectedDouble, Is.EqualTo(r5).Within(.1d));

            r5 = db.Scalar<Author, double>(e => Sql.Max(e.DoubleProperty), e => e.City == "SinCity");
            Assert.That(default(double), Is.EqualTo(r5));


            var expectedNullableDouble = authors.Max(e => e.NDoubleProperty);
            double? r6 = db.Scalar<Author, double?>(e => Sql.Max(e.NDoubleProperty));
            Assert.That(expectedNullableDouble, Is.EqualTo(r6));


            expectedNullableDouble = authors.Where(e => e.City == "London").Max(e => e.NDoubleProperty);
            r6 = db.Scalar<Author, double?>(e => Sql.Max(e.NDoubleProperty), e => e.City == "London");
            Assert.That(expectedNullableDouble, Is.EqualTo(r6));

            r6 = db.Scalar<Author, double?>(e => Sql.Max(e.NDoubleProperty), e => e.City == "SinCity");
            Assert.That(default(double?), Is.EqualTo(r6));



            var expectedFloat = authors.Max(e => e.FloatProperty);
            var r7 = db.Scalar<Author, float>(e => Sql.Max(e.FloatProperty));
            Assert.That(expectedFloat, Is.EqualTo(r7));

            expectedFloat = authors.Where(e => e.City == "London").Max(e => e.FloatProperty);
            r7 = db.Scalar<Author, float>(e => Sql.Max(e.FloatProperty), e => e.City == "London");
            Assert.That(expectedFloat, Is.EqualTo(r7));

            r7 = db.Scalar<Author, float>(e => Sql.Max(e.FloatProperty), e => e.City == "SinCity");
            Assert.That(default(float), Is.EqualTo(r7));


            var expectedNullableFloat = authors.Max(e => e.NFloatProperty);
            var r8 = db.Scalar<Author, float?>(e => Sql.Max(e.NFloatProperty));
            Assert.That(expectedNullableFloat, Is.EqualTo(r8));

            expectedNullableFloat = authors.Where(e => e.City == "London").Max(e => e.NFloatProperty);
            r8 = db.Scalar<Author, float?>(e => Sql.Max(e.NFloatProperty), e => e.City == "London");
            Assert.That(expectedNullableFloat, Is.EqualTo(r8));

            r8 = db.Scalar<Author, float?>(e => Sql.Max(e.NFloatProperty), e => e.City == "SinCity");
            Assert.That(default(float?), Is.EqualTo(r8));


            var expectedString = authors.Min(e => e.Name);
            var r9 = db.Scalar<Author, string>(e => Sql.Min(e.Name));
            Assert.That(expectedString, Is.EqualTo(r9));

            expectedString = authors.Where(e => e.City == "London").Min(e => e.Name);
            r9 = db.Scalar<Author, string>(e => Sql.Min(e.Name), e => e.City == "London");
            Assert.That(expectedString, Is.EqualTo(r9));

            r9 = db.Scalar<Author, string>(e => Sql.Max(e.Name), e => e.City == "SinCity");
            Assert.That(string.IsNullOrEmpty(r9));

            //Can't MIN(bit)/MAX(bit) in SQL Server
            if (Dialect == Dialect.SqlServer)
            {
                var expectedBool = authors.Min(e => e.Active);
                var r10 = db.Scalar<Author, bool>(e => Sql.Count(e.Active));
                Assert.That(expectedBool, Is.EqualTo(r10));

                expectedBool = authors.Max(e => e.Active);
                r10 = db.Scalar<Author, bool>(e => Sql.Count(e.Active));
                Assert.That(expectedBool, Is.EqualTo(r10));

                r10 = db.Scalar<Author, bool>(e => Sql.Count(e.Active), e => e.City == "SinCity");
                Assert.IsFalse(r10);
            }

            var expectedShort = authors.Max(e => e.Rate);
            var r11 = db.Scalar<Author, short>(e => Sql.Max(e.Rate));
            Assert.That(expectedShort, Is.EqualTo(r11));

            expectedShort = authors.Where(e => e.City == "London").Max(e => e.Rate);
            r11 = db.Scalar<Author, short>(e => Sql.Max(e.Rate), e => e.City == "London");
            Assert.That(expectedShort, Is.EqualTo(r11));

            r11 = db.Scalar<Author, short>(e => Sql.Max(e.Rate), e => e.City == "SinCity");
            Assert.That(default(short), Is.EqualTo(r7));


            var expectedNullableShort = authors.Max(e => e.NRate);
            var r12 = db.Scalar<Author, short?>(e => Sql.Max(e.NRate));
            Assert.That(expectedNullableShort, Is.EqualTo(r12));

            expectedNullableShort = authors.Where(e => e.City == "London").Max(e => e.NRate);
            r12 = db.Scalar<Author, short?>(e => Sql.Max(e.NRate), e => e.City == "London");
            Assert.That(expectedNullableShort, Is.EqualTo(r12));

            r12 = db.Scalar<Author, short?>(e => Sql.Max(e.NRate), e => e.City == "SinCity");
            Assert.That(default(short?), Is.EqualTo(r12));

        }

    }

}


internal class Author
{
    public Author() { }

    [AutoIncrement]
    [Alias("AuthorID")]
    public Int32 Id { get; set; }

    [Index(Unique = true)]
    [StringLength(40)]
    public string Name { get; set; }

    public DateTime Birthday { get; set; }
    public DateTime? LastActivity { get; set; }
    public decimal Earnings { get; set; }
    public decimal? NEarnings { get; set; }

    public bool Active { get; set; }

    [StringLength(80)]
    [Alias("JobCity")]
    public string City { get; set; }

    [StringLength(80)]
    [Alias("Comment")]
    public string Comments { get; set; }

    public short Rate { get; set; }
    public short? NRate { get; set; }
    public float FloatProperty { get; set; }
    public float? NFloatProperty { get; set; }
    public double DoubleProperty { get; set; }
    public double? NDoubleProperty { get; set; }

}