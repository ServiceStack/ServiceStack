using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.UseCase;

public class Author
{
    [AutoIncrement]
    [Alias("AuthorID")]
    public int Id { get; set; }
    [Index(Unique = true)]
    [StringLength(40)]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public DateTime? LastActivity { get; set; }
    public decimal? Earnings { get; set; }
    public bool Active { get; set; }
    [StringLength(80)]
    [Alias("JobCity")]
    public string City { get; set; }
    [StringLength(80)]
    [Alias("Comment")]
    public string Comments { get; set; }
    public short Rate { get; set; }
}

[TestFixtureOrmLite]
public class ExpressionsAuthorTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Run_Expressions_Author_tests()
    {
        var hold = OrmLiteConfig.StripUpperInLike;
        OrmLiteConfig.StripUpperInLike = false;

        using (var db = OpenDbConnection())
        {
            var dialect = DialectProvider;
            var q = db.From<Author>();

            db.DropTable<Author>();

            var tableName = dialect.NamingStrategy.GetTableName(nameof(Author));
            var tableExists = dialect.DoesTableExist(db, tableName);
            Assert.That(tableExists, Is.False);

            db.CreateTable<Author>();

            tableExists = dialect.DoesTableExist(db, tableName);
            Assert.That(tableExists);

            db.DeleteAll<Author>();

            "Inserting...".Print();
            var t1 = DateTime.Now;
            var authors = GetAuthors();
            db.InsertAll(authors);
            var t2 = DateTime.Now;
            "Inserted {0} rows in {1}".Print(authors.Count, t2 - t1);

            "Selecting.....".Print();
            var year = DateTime.Today.AddYears(-20).Year;
            var lastDay = new DateTime(year, 12, 31);
            var expected = 5;

            q.Where().Where(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay);
            q.ToSelectStatement().Print();

            var result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));

            result = db.Select(db.From<Author>().Where(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay));
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay);
            Assert.That(result.Count, Is.EqualTo(expected));
            var a = new Author { Birthday = lastDay };
            result = db.Select<Author>(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= a.Birthday);
            Assert.That(result.Count, Is.EqualTo(expected));

            // select authors from London, Berlin and Madrid : 6
            expected = 6;
            //Sql.In can take params object[]
            var city = "Berlin";
            q.Where().Where(rn => Sql.In(rn.City, "London", "Madrid", city)); //clean prev
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => Sql.In(rn.City, "London", "Madrid", "Berlin"));
            Assert.That(result.Count, Is.EqualTo(expected));

            // select authors from Bogota and Cartagena : 7
            expected = 7;
            //... or Sql.In can  take List<Object>
            city = "Bogota";
            var cities = new List<object> { city, "Cartagena" };
            q.Where().Where(rn => Sql.In(rn.City, cities));
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => Sql.In(rn.City, "Bogota", "Cartagena"));
            Assert.That(result.Count, Is.EqualTo(expected));


            // select authors which name starts with A
            expected = 3;
            q.Where().Where(rn => rn.Name.StartsWith("A"));
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => rn.Name.StartsWith("A"));
            Assert.That(result.Count, Is.EqualTo(expected));

            // select authors which name ends with Garzon o GARZON o garzon ( no case sensitive )
            expected = 3;
            var name = "GARZON";
            q.Where().Where(rn => rn.Name.ToUpper().EndsWith(name));
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => rn.Name.ToUpper().EndsWith(name));
            Assert.That(result.Count, Is.EqualTo(expected));

            // select authors which name ends with garzon
            //A percent symbol ("%") in the LIKE pattern matches any sequence of zero or more characters 
            //in the string. 
            //An underscore ("_") in the LIKE pattern matches any single character in the string. 
            //Any other character matches itself or its lower/upper case equivalent (i.e. case-insensitive matching).
            expected = 3;
            q.Where().Where(rn => rn.Name.EndsWith("garzon"));
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => rn.Name.EndsWith("garzon"));
            Assert.That(result.Count, Is.EqualTo(expected));


            // select authors which name contains  Benedict 
            expected = 2;
            name = "Benedict";
            q.Where().Where(rn => rn.Name.Contains(name));
            result = db.Select(q);
            q.WhereExpression.Print();
            Assert.That(result.Count, Is.EqualTo(expected));
            result = db.Select<Author>(rn => rn.Name.Contains("Benedict"));
            Assert.That(result.Count, Is.EqualTo(expected));
            a.Name = name;
            result = db.Select<Author>(rn => rn.Name.Contains(a.Name));
            Assert.That(result.Count, Is.EqualTo(expected));


            // select authors with Earnings <= 50 
            expected = 3;
            var earnings = 50;
            q.Where().Where(rn => rn.Earnings <= earnings);
            result = db.Select(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");
            result = db.Select<Author>(rn => rn.Earnings <= 50);
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");

            // select authors with Rate = 10 and city=Mexio 
            expected = 1;
            city = "Mexico";
            q.Where().Where(rn => rn.Rate == 10 && rn.City == city);
            result = db.Select(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");
            result = db.Select<Author>(rn => rn.Rate == 10 && rn.City == "Mexico");
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");

            a.City = city;
            result = db.Select<Author>(rn => rn.Rate == 10 && rn.City == a.City);
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");

            //  enough selecting, lets update;
            // set Active=false where rate =0
            expected = 2;
            var rate = 0;
            q.Where().Where(rn => rn.Rate == rate).Update(rn => rn.Active);
            var rows = db.UpdateOnlyFields(new Author { Active = false }, q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, rows, expected == rows ? "OK" : "**************  FAILED ***************");

            // insert values  only in Id, Name, Birthday, Rate and Active fields 
            expected = 4;
            db.InsertOnly(new Author() { Active = false, Rate = 0, Name = "Victor Grozny", Birthday = DateTime.Today.AddYears(-18) }, rn => new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate });
            db.InsertOnly(new Author() { Active = false, Rate = 0, Name = "Ivan Chorny", Birthday = DateTime.Today.AddYears(-19) }, rn => new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate });
            q.Where().Where(rn => !rn.Active);
            result = db.Select(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");

            //update comment for City == null 
            expected = 2;
            q.Where().Where(rn => rn.City == null).Update(rn => rn.Comments);
            rows = db.UpdateOnlyFields(new Author() { Comments = "No comments" }, q);
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, rows, expected == rows ? "OK" : "**************  FAILED ***************");

            // delete where City is null 
            expected = 2;
            rows = db.Delete(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, rows, expected == rows ? "OK" : "**************  FAILED ***************");


            //   lets select  all records ordered by Rate Descending and Name Ascending
            expected = 14;
            q.Where().OrderBy(rn => new { at = Sql.Desc(rn.Rate), rn.Name }); // clear where condition
            result = db.Select(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");
            Console.WriteLine(q.OrderByExpression);
            var author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Claudia Espinel", author.Name, "Claudia Espinel" == author.Name ? "OK" : "**************  FAILED ***************");

            // select  only first 5 rows ....

            expected = 5;
            q.Limit(5); // note: order is the same as in the last sentence
            result = db.Select(q);
            q.WhereExpression.Print();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");


            // and finally lets select only Name and City (name will be "UPPERCASED" )

            q.Select(rn => new { at = Sql.As(rn.Name.ToUpper(), "Name"), rn.City });
            q.SelectExpression.Print();
            result = db.Select(q);
            author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Claudia Espinel".ToUpper(), author.Name, "Claudia Espinel".ToUpper() == author.Name ? "OK" : "**************  FAILED ***************");

            q.Select(rn => new { at = Sql.As(rn.Name.ToUpper(), rn.Name), rn.City });
            q.SelectExpression.Print();
            result = db.Select(q);
            author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Claudia Espinel".ToUpper(), author.Name, "Claudia Espinel".ToUpper() == author.Name ? "OK" : "**************  FAILED ***************");

            //paging :
            q.Limit(0, 4);// first page, page size=4;
            result = db.Select(q);
            author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Claudia Espinel".ToUpper(), author.Name, "Claudia Espinel".ToUpper() == author.Name ? "OK" : "**************  FAILED ***************");

            q.Limit(4, 4);// second page
            result = db.Select(q);
            author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Jorge Garzon".ToUpper(), author.Name, "Jorge Garzon".ToUpper() == author.Name ? "OK" : "**************  FAILED ***************");

            q.Limit(8, 4);// third page
            result = db.Select(q);
            author = result.FirstOrDefault();
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", "Rodger Contreras".ToUpper(), author.Name, "Rodger Contreras".ToUpper() == author.Name ? "OK" : "**************  FAILED ***************");

            // select distinct..
            q.Limit().OrderBy(); // clear limit, clear order for postres
            q.SelectDistinct(r => r.City);
            expected = 6;
            result = db.Select(q);
            Console.WriteLine("Expected:{0}  Selected:{1}  {2}", expected, result.Count, expected == result.Count ? "OK" : "**************  FAILED ***************");

            q.Select(r => Sql.As(Sql.Max(r.Birthday), "Birthday"));
            result = db.Select(q);
            var expectedResult = authors.Max(r => r.Birthday);
            Assert.That(result[0].Birthday, Is.EqualTo(expectedResult));

            q.Select(r => Sql.As(Sql.Max(r.Birthday), r.Birthday));
            result = db.Select(q);
            expectedResult = authors.Max(r => r.Birthday);
            Assert.That(result[0].Birthday, Is.EqualTo(expectedResult));

            var r1 = db.Single(q);
            Assert.That(r1.Birthday, Is.EqualTo(expectedResult));

            var r2 = db.Scalar<Author, DateTime>(e => Sql.Max(e.Birthday));
            Assert.That(r2, Is.EqualTo(expectedResult));

            q.Select(r => Sql.As(Sql.Min(r.Birthday), "Birthday"));
            result = db.Select(q);
            expectedResult = authors.Min(r => r.Birthday);
            Assert.That(result[0].Birthday, Is.EqualTo(expectedResult));

            q.Select(r => Sql.As(Sql.Min(r.Birthday), r.Birthday));
            result = db.Select(q);
            expectedResult = authors.Min(r => r.Birthday);
            Assert.That(result[0].Birthday, Is.EqualTo(expectedResult));

            q.Select(r => new { r.City, MaxResult = Sql.As(Sql.Min(r.Birthday), "Birthday") })
                .GroupBy(r => r.City)
                .OrderBy(r => r.City);
            result = db.Select(q);
            var expectedStringResult = "Berlin";
            Assert.That(result[0].City, Is.EqualTo(expectedStringResult));

            q.Select(r => new { r.City, MaxResult = Sql.As(Sql.Min(r.Birthday), r.Birthday) })
                .GroupBy(r => r.City)
                .OrderBy(r => r.City);
            result = db.Select(q);
            expectedStringResult = "Berlin";
            Assert.That(result[0].City, Is.EqualTo(expectedStringResult));

            r1 = db.Single(q);
            Assert.That(r1.City, Is.EqualTo(expectedStringResult));

            var expectedDecimal = authors.Max(e => e.Earnings);
            var r3 = db.Scalar<Author, decimal?>(e => Sql.Max(e.Earnings));
            Assert.That(r3.Value, Is.EqualTo(expectedDecimal));

            var expectedString = authors.Max(e => e.Name);
            var r4 = db.Scalar<Author, string>(e => Sql.Max(e.Name));
            Assert.That(r4, Is.EqualTo(expectedString));

            var expectedDate = authors.Max(e => e.LastActivity);
            var r5 = db.Scalar<Author, DateTime?>(e => Sql.Max(e.LastActivity));
            Assert.That(r5, Is.EqualTo(expectedDate));

            var expectedDate51 = authors.Where(e => e.City == "Bogota").Max(e => e.LastActivity);
            var r51 = db.Scalar<Author, DateTime?>(
                e => Sql.Max(e.LastActivity),
                e => e.City == "Bogota");
            Assert.That(r51, Is.EqualTo(expectedDate51));

            try
            {
                var expectedBool = authors.Max(e => e.Active);
                var r6 = db.Scalar<Author, bool>(e => Sql.Max(e.Active));
                Assert.That(r6, Is.EqualTo(expectedBool));
            }
            catch (Exception e)
            {
                if (Dialect.HasFlag(Dialect.AnyPostgreSql))
                    Console.WriteLine("OK PostgreSQL: " + e.Message);
                else
                    Console.WriteLine("**************  FAILED *************** " + e.Message);
            }



            // Tests for predicate overloads that make use of the expression visitor
            "First author by name (exists)".Print();
            author = db.Single<Author>(x => x.Name == "Jorge Garzon");
            Assert.That(author.Name, Is.EqualTo("Jorge Garzon"));

            "First author by name (does not exist)".Print();
            author = db.Single<Author>(x => x.Name == "Does not exist");
            Assert.That(author, Is.Null);

            "First author or default (does not exist)".Print();
            author = db.Single<Author>(x => x.Name == "Does not exist");
            Assert.That(author, Is.Null);

            "First author or default by city (multiple matches)".Print();
            author = db.Single<Author>(x => x.City == "Bogota");
            Assert.That(author.Name, Is.EqualTo("Angel Colmenares"));

            a.City = "Bogota";
            author = db.Single<Author>(x => x.City == a.City);
            Assert.That(author.Name, Is.EqualTo("Angel Colmenares"));

            // count test
            var expectedCount = authors.Count;
            var r7 = db.Scalar<Author, long>(e => Sql.Count(e.Id));
            Assert.That(r7, Is.EqualTo(expectedCount));

            expectedCount = authors.Count(e => e.City == "Bogota");
            r7 = db.Scalar<Author, long>(
                e => Sql.Count(e.Id),
                e => e.City == "Bogota");
            Assert.That(r7, Is.EqualTo(expectedCount));

            // more updates.....
            Console.WriteLine("more updates.....................");
            q.Update();// all fields will be updated
            // select and update 
            expected = 1;
            var rr = db.Single<Author>(rn => rn.Name == "Luis garzon");
            rr.City = "Madrid";
            rr.Comments = "Updated";
            q.Where().Where(r => r.Id == rr.Id); // if omit,  then all records will be updated 
            rows = db.UpdateOnlyFields(rr, q); // == dbCmd.Update(rr) but it returns void
            Assert.That(rows, Is.EqualTo(expected));

            expected = 0;
            q.Where().Where(r => r.City == "Ciudad Gotica");
            rows = db.UpdateOnlyFields(rr, q);
            Assert.That(rows, Is.EqualTo(expected));

            expected = db.Select<Author>(x => x.City == "Madrid").Count;
            author = new Author { Active = false };
            rows = db.UpdateOnlyFields(author, x => x.Active, x => x.City == "Madrid");
            Assert.That(rows, Is.EqualTo(expected));

            expected = db.Select<Author>(x => x.Active == false).Count;
            rows = db.Delete<Author>(x => x.Active == false);
            Assert.That(rows, Is.EqualTo(expected));

            var t3 = DateTime.Now;
            "Expressions test in: {0}".Print(t3 - t2);
            "All test in :        {0}".Print(t3 - t1);
        }

        OrmLiteConfig.StripUpperInLike = hold;
    }

    public List<Author> GetAuthors()
    {
        return new List<Author>
        {
            new Author { Name = "Demis Bellot", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 99.9m, Comments = "CSharp books", Rate = 10, City = "London" },
            new Author { Name = "Angel Colmenares", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 50.0m, Comments = "CSharp books", Rate = 5, City = "Bogota" },
            new Author { Name = "Adam Witco", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 80.0m, Comments = "Math Books", Rate = 9, City = "London" },
            new Author { Name = "Claudia Espinel", Birthday = DateTime.Today.AddYears(-23), Active = true, Earnings = 60.0m, Comments = "Cooking books", Rate = 10, City = "Bogota" },
            new Author { Name = "Libardo Pajaro", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 80.0m, Comments = "CSharp books", Rate = 9, City = "Bogota" },
            new Author { Name = "Jorge Garzon", Birthday = DateTime.Today.AddYears(-28), Active = true, Earnings = 70.0m, Comments = "CSharp books", Rate = 9, City = "Bogota" },
            new Author { Name = "Alejandro Isaza", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 70.0m, Comments = "Java books", Rate = 0, City = "Bogota" },
            new Author { Name = "Wilmer Agamez", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 30.0m, Comments = "Java books", Rate = 0, City = "Cartagena" },
            new Author { Name = "Rodger Contreras", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 90.0m, Comments = "CSharp books", Rate = 8, City = "Cartagena" },
            new Author { Name = "Chuck Benedict", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "CSharp books", Rate = 8, City = "London" },
            new Author { Name = "James Benedict II", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "Java books", Rate = 5, City = "Berlin" },
            new Author { Name = "Ethan Brown", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 45.0m, Comments = "CSharp books", Rate = 5, City = "Madrid" },
            new Author { Name = "Xavi Garzon", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 75.0m, Comments = "CSharp books", Rate = 9, City = "Madrid" },
            new Author { Name = "Luis garzon", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.0m, Comments = "CSharp books", Rate = 10, City = "Mexico" },
        };
    }

}