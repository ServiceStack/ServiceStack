using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expressions
{
    public class AuthorUseCase : OrmLiteTestBase
    {
        private List<Author> authors; 

        public AuthorUseCase()
        {
            authors = new List<Author>();
            authors.Add(new Author() { Name = "Demis Bellot", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 99.9m, Comments = "CSharp books", Rate = 10, City = "London" });
            authors.Add(new Author() { Name = "Angel Colmenares", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 50.0m, Comments = "CSharp books", Rate = 5, City = "Bogota" });
            authors.Add(new Author() { Name = "Adam Witco", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 80.0m, Comments = "Math Books", Rate = 9, City = "London" });
            authors.Add(new Author() { Name = "Claudia Espinel", Birthday = DateTime.Today.AddYears(-23), Active = true, Earnings = 60.0m, Comments = "Cooking books", Rate = 10, City = "Bogota" });
            authors.Add(new Author() { Name = "Libardo Pajaro", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 80.0m, Comments = "CSharp books", Rate = 9, City = "Bogota" });
            authors.Add(new Author() { Name = "Jorge Garzon", Birthday = DateTime.Today.AddYears(-28), Active = true, Earnings = 70.0m, Comments = "CSharp books", Rate = 9, City = "Bogota" });
            authors.Add(new Author() { Name = "Alejandro Isaza", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 70.0m, Comments = "Java books", Rate = 0, City = "Bogota" });
            authors.Add(new Author() { Name = "Wilmer Agamez", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 30.0m, Comments = "Java books", Rate = 0, City = "Cartagena" });
            authors.Add(new Author() { Name = "Rodger Contreras", Birthday = DateTime.Today.AddYears(-25), Active = true, Earnings = 90.0m, Comments = "CSharp books", Rate = 8, City = "Cartagena" });
            authors.Add(new Author() { Name = "Chuck Benedict", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "CSharp books", Rate = 8, City = "London" });
            authors.Add(new Author() { Name = "James Benedict II", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.5m, Comments = "Java books", Rate = 5, City = "Berlin" });
            authors.Add(new Author() { Name = "Ethan Brown", Birthday = DateTime.Today.AddYears(-20), Active = true, Earnings = 45.0m, Comments = "CSharp books", Rate = 5, City = "Madrid" });
            authors.Add(new Author() { Name = "Xavi Garzon", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 75.0m, Comments = "CSharp books", Rate = 9, City = "Madrid" });
            authors.Add(new Author() { Name = "Luis garzon", Birthday = DateTime.Today.AddYears(-22), Active = true, Earnings = 85.0m, Comments = "CSharp books", Rate = 10, City = "Mexico", LastActivity = DateTime.Today });   
        }

        [SetUp]
        public void Setup()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<Author>(true);
                con.SaveAll(authors);
            }
        }

        [Test]
        public void AuthorUsesCases()
        {
            var ev = OrmLiteConfig.DialectProvider.SqlExpression<Author>();

            using (var db = OpenDbConnection())
            {
                int year = DateTime.Today.AddYears(-20).Year;
                var lastDay = new DateTime(year, 12, 31);
                int expected = 5;

                ev.Where(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay);
                List<Author> result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select(db.From<Author>().Where(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay));
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= lastDay);
                Assert.AreEqual(expected, result.Count);
                Author a = new Author() { Birthday = lastDay };
                result = db.Select<Author>(rn => rn.Birthday >= new DateTime(year, 1, 1) && rn.Birthday <= a.Birthday);
                Assert.AreEqual(expected, result.Count);

                // select authors from London, Berlin and Madrid : 6
                expected = 6;
                //Sql.In can take params object[]
                var city = "Berlin";
                ev.Where().Where(rn => Sql.In(rn.City, "London", "Madrid", city));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => Sql.In(rn.City, new[] { "London", "Madrid", "Berlin" }));
                Assert.AreEqual(expected, result.Count);

                // select authors from Bogota and Cartagena : 7
                expected = 7;
                //... or Sql.In can  take List<Object>
                city = "Bogota";
                List<Object> cities = new List<Object>();
                cities.Add(city);
                cities.Add("Cartagena");
				ev.Where().Where(rn => Sql.In(rn.City, cities));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => Sql.In(rn.City, "Bogota", "Cartagena"));
                Assert.AreEqual(expected, result.Count);


                // select authors which name starts with A
                expected = 3;
				ev.Where().Where(rn => rn.Name.StartsWith("A"));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Name.StartsWith("A"));
                Assert.AreEqual(expected, result.Count);

                // select authors which name ends with Garzon o GARZON o garzon ( no case sensitive )
                expected = 3;
                var name = "GARZON";
				ev.Where().Where(rn => rn.Name.ToUpper().EndsWith(name));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Name.ToUpper().EndsWith(name));
                Assert.AreEqual(expected, result.Count);

                // select authors which name ends with garzon
                //A percent symbol ("%") in the LIKE pattern matches any sequence of zero or more characters 
                //in the string. 
                //An underscore ("_") in the LIKE pattern matches any single character in the string. 
                //Any other character matches itself or its lower/upper case equivalent (i.e. case-insensitive matching).
                expected = 3;
				ev.Where().Where(rn => rn.Name.EndsWith("garzon"));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Name.EndsWith("garzon"));
                Assert.AreEqual(expected, result.Count);


                // select authors which name contains  Benedict 
                expected = 2;
                name = "Benedict";
				ev.Where().Where(rn => rn.Name.Contains(name));
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Name.Contains("Benedict"));
                Assert.AreEqual(expected, result.Count);
                a.Name = name;
                result = db.Select<Author>(rn => rn.Name.Contains(a.Name));
                Assert.AreEqual(expected, result.Count);


                // select authors with Earnings <= 50 
                expected = 3;
                var earnings = 50;
				ev.Where().Where(rn => rn.Earnings <= earnings);
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Earnings <= 50);
                Assert.AreEqual(expected, result.Count);

                // select authors with Rate = 10 and city=Mexio 
                expected = 1;
                city = "Mexico";
				ev.Where().Where(rn => rn.Rate == 10 && rn.City == city);
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                result = db.Select<Author>(rn => rn.Rate == 10 && rn.City == "Mexico");
                Assert.AreEqual(expected, result.Count);

                a.City = city;
                result = db.Select<Author>(rn => rn.Rate == 10 && rn.City == a.City);
                Assert.AreEqual(expected, result.Count);

                //  enough selecting, lets update;
                // set Active=false where rate =0
                expected = 2;
                var rate = 0;
				ev.Where().Where(rn => rn.Rate == rate).Update(rn => rn.Active);
                var rows = db.UpdateOnlyFields(new Author() { Active = false }, ev);
                Assert.AreEqual(expected, rows);

                // insert values  only in Id, Name, Birthday, Rate and Active fields 
                expected = 4;
                ev.Insert(rn => new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate });
                db.InsertOnly(new Author() { Active = false, Rate = 0, Name = "Victor Grozny", Birthday = DateTime.Today.AddYears(-18) }, rn => new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate });
                db.InsertOnly(new Author() { Active = false, Rate = 0, Name = "Ivan Chorny", Birthday = DateTime.Today.AddYears(-19) }, rn => new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate });
				ev.Where().Where(rn => !rn.Active);
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);

                //update comment for City == null 
                expected = 2;
				ev.Where().Where(rn => rn.City == null).Update(rn => rn.Comments);
                rows = db.UpdateOnlyFields(new Author() { Comments = "No comments" }, ev);
                Assert.AreEqual(expected, rows);

                // delete where City is null 
                expected = 2;
                rows = db.Delete(ev);
                Assert.AreEqual(expected, rows);


                //   lets select  all records ordered by Rate Descending and Name Ascending
                expected = 14;
                ev.Where().OrderBy(rn => new { at = Sql.Desc(rn.Rate), rn.Name }); // clear where condition
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);
                var author = result.FirstOrDefault();
                Assert.AreEqual("Claudia Espinel", author.Name);

                // select  only first 5 rows ....
                expected = 5;
                ev.Limit(5); // note: order is the same as in the last sentence
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);


                // and finally lets select only Name and City (name will be "UPPERCASED" )

                ev.Select(rn => new { at = Sql.As(rn.Name.ToUpper(), "Name"), rn.City });
                Console.WriteLine(ev.SelectExpression);
                result = db.Select(ev);
                author = result.FirstOrDefault();
                Assert.AreEqual("Claudia Espinel".ToUpper(), author.Name);

                ev.Select(rn => new { at = Sql.As(rn.Name.ToUpper(), rn.Name), rn.City });
                Console.WriteLine(ev.SelectExpression);
                result = db.Select(ev);
                author = result.FirstOrDefault();
                Assert.AreEqual("Claudia Espinel".ToUpper(), author.Name);

                //paging :
                ev.Limit(0, 4);// first page, page size=4;
                result = db.Select(ev);
                author = result.FirstOrDefault();
                Assert.AreEqual("Claudia Espinel".ToUpper(), author.Name);

                ev.Limit(4, 4);// second page
                result = db.Select(ev);
                author = result.FirstOrDefault();
                Assert.AreEqual("Jorge Garzon".ToUpper(), author.Name);

                ev.Limit(8, 4);// third page
                result = db.Select(ev);
                author = result.FirstOrDefault();
                Assert.AreEqual("Rodger Contreras".ToUpper(), author.Name);

                // select distinct..
                ev.Limit().OrderBy(); // clear limit, clear order for postres
                ev.SelectDistinct(r => r.City);
                expected = 6;
                result = db.Select(ev);
                Assert.AreEqual(expected, result.Count);

                ev.Select(r => Sql.As(Sql.Max(r.Birthday), "Birthday"));
                result = db.Select(ev);
                var expectedResult = authors.Max(r => r.Birthday);
                Assert.AreEqual(expectedResult, result[0].Birthday);

                ev.Select(r => Sql.As(Sql.Max(r.Birthday), r.Birthday));
                result = db.Select(ev);
                expectedResult = authors.Max(r => r.Birthday);
                Assert.AreEqual(expectedResult, result[0].Birthday);

                var r1 = db.Single(ev);
                Assert.AreEqual(expectedResult, r1.Birthday);

                var r2 = db.Scalar<Author, DateTime>(e => Sql.Max(e.Birthday));
                Assert.AreEqual(expectedResult, r2);

                ev.Select(r => Sql.As(Sql.Min(r.Birthday), "Birthday"));
                result = db.Select(ev);
                expectedResult = authors.Min(r => r.Birthday);
                Assert.AreEqual(expectedResult, result[0].Birthday);

                ev.Select(r => Sql.As(Sql.Min(r.Birthday), r.Birthday));
                result = db.Select(ev);
                expectedResult = authors.Min(r => r.Birthday);
                Assert.AreEqual(expectedResult, result[0].Birthday);


                ev.Select(r => new { r.City, MaxResult = Sql.As(Sql.Min(r.Birthday), "Birthday") })
                        .GroupBy(r => r.City)
                        .OrderBy(r => r.City);
                result = db.Select(ev);
                var expectedStringResult = "Berlin";
                Assert.AreEqual(expectedStringResult, result[0].City);

                ev.Select(r => new { r.City, MaxResult = Sql.As(Sql.Min(r.Birthday), r.Birthday) })
                        .GroupBy(r => r.City)
                        .OrderBy(r => r.City);
                result = db.Select(ev);
                expectedStringResult = "Berlin";
                Assert.AreEqual(expectedStringResult, result[0].City);

                r1 = db.Single(ev);
                Assert.AreEqual(expectedStringResult, r1.City);

                var expectedDecimal = authors.Max(e => e.Earnings);
                Decimal? r3 = db.Scalar<Author, Decimal?>(e => Sql.Max(e.Earnings));
                Assert.AreEqual(expectedDecimal, r3.Value);

                var expectedString = authors.Max(e => e.Name);
                string r4 = db.Scalar<Author, String>(e => Sql.Max(e.Name));
                Assert.AreEqual(expectedString, r4);

                var expectedDate = authors.Max(e => e.LastActivity);
                DateTime? r5 = db.Scalar<Author, DateTime?>(e => Sql.Max(e.LastActivity));
                Assert.AreEqual(expectedDate, r5);

                var expectedDate51 = authors.Where(e => e.City == "Bogota").Max(e => e.LastActivity);
                DateTime? r51 = db.Scalar<Author, DateTime?>(
                    e => Sql.Max(e.LastActivity),
                    e => e.City == "Bogota");
                Assert.AreEqual(expectedDate51, r51);

                try
                {
                    var expectedBool = authors.Max(e => e.Active);
                    bool r6 = db.Scalar<Author, bool>(e => Sql.Max(e.Active));
                    Assert.AreEqual(expectedBool, r6);
                }
                catch (Exception)
                {
                    //????
                    //if (dialect.Name == "PostgreSQL")
                    //    Console.WriteLine("OK PostgreSQL: " + e.Message);
                    //else
                    //    Console.WriteLine("**************  FAILED *************** " + e.Message);
                }



                // Tests for predicate overloads that make use of the expression visitor
                author = db.Single<Author>(q => q.Name == "Jorge Garzon");

                author = db.Single<Author>(q => q.Name == "Does not exist");
                Assert.IsNull(author);

                author = db.Single<Author>(q => q.City == "Bogota");
                Assert.AreEqual("Angel Colmenares", author.Name);

                a.City = "Bogota";
                author = db.Single<Author>(q => q.City == a.City);
                Assert.AreEqual("Angel Colmenares", author.Name);

                // count test

                var expectedCount = authors.Count();
                long r7 = db.Scalar<Author, long>(e => Sql.Count(e.Id));
                Assert.AreEqual(expectedCount, r7);

                expectedCount = authors.Count(e => e.City == "Bogota");
                r7 = db.Scalar<Author, long>(
                    e => Sql.Count(e.Id),
                    e => e.City == "Bogota");
                Assert.AreEqual(expectedCount, r7);

                ev.Update();// all fields will be updated
                // select and update 
                expected = 1;
                var rr = db.Single<Author>(rn => rn.Name == "Luis garzon");
                rr.City = "Madrid";
                rr.Comments = "Updated";
				ev.Where().Where(r => r.Id == rr.Id); // if omit,  then all records will be updated 
                rows = db.UpdateOnlyFields(rr, ev); // == dbCmd.Update(rr) but it returns void
                Assert.AreEqual(expected, rows);

                expected = 0;
				ev.Where().Where(r => r.City == "Ciudad Gotica");
                rows = db.UpdateOnlyFields(rr, ev);
                Assert.AreEqual(expected, rows);

                expected = db.Select<Author>(x => x.City == "Madrid").Count;
                author = new Author() { Active = false };
                rows = db.UpdateOnlyFields(author, x => x.Active, x => x.City == "Madrid");
                Assert.AreEqual(expected, rows);

                expected = db.Select<Author>(x => x.Active == false).Count;
                rows = db.Delete<Author>(x => x.Active == false);
                Assert.AreEqual(expected, rows);
            }
        }
    }
}
