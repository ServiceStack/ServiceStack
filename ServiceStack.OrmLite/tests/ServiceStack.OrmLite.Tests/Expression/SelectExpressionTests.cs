using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression
{

    [TestFixtureOrmLite]
    public class SelectExpressionTests : ExpressionsTestBase
    {
        public SelectExpressionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_select_where_and_limit_expression()
        {
            Init(20);

            using (var db = OpenDbConnection())
            {
                var rows = db.Select(db.From<TestType>().Where(x => x.BoolColumn).Limit(5));
                db.GetLastSql().Print();

                Assert.That(rows.Count, Is.EqualTo(5));
                Assert.That(rows.All(x => x.BoolColumn), Is.True);
            }
        }

        [Test]
        public void Can_select_on_TimeSpans()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TestType>();

                db.Insert(new TestType { TimeSpanColumn = TimeSpan.FromHours(1) });

                var rows = db.Select<TestType>(q =>
                    q.TimeSpanColumn > TimeSpan.FromMinutes(30)
                    && q.TimeSpanColumn < TimeSpan.FromHours(2));

                Assert.That(rows.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_select_on_Dates()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Submission>();

                var dates = new[]
                {
                    new DateTime(2014,1,1),
                    new DateTime(2014,1,1,1,0,0),
                    new DateTime(2014,1,1,2,0,0),
                    new DateTime(2014,1,2),
                    new DateTime(2014,2,1),
                    DateTime.UtcNow,
                    new DateTime(2015,1,1),
                };

                var i = 0;
                dates.Each(x => db.Insert(new Submission
                {
                    Id = i++,
                    StoryDate = x,
                    Headline = "Headline" + i,
                    Body = "Body" + i,
                }));

                Assert.That(db.Select<Submission>(q => q.StoryDate >= new DateTime(2014, 1, 1)).Count,
                    Is.EqualTo(dates.Length));

                Assert.That(db.Select<Submission>(q => q.StoryDate <= new DateTime(2014, 1, 1, 2, 0, 0)).Count,
                    Is.EqualTo(3));

                var storyDateTime = new DateTime(2014, 1, 1);
                Assert.That(db.Select<Submission>(q => q.StoryDate > storyDateTime - new TimeSpan(1, 0, 0, 0) &&
                                                       q.StoryDate < storyDateTime + new TimeSpan(1, 0, 0, 0)).Count,
                    Is.EqualTo(3));
            }
        }

        public class Shipper
        {
            [AutoIncrement]
            public int Id { get; set; }

            public string CompanyName { get; set; }

            public string Phone { get; set; }

            public int ShipperTypeId { get; set; }
        }

        public class SubsetOfShipper
        {
            public string Phone { get; set; }
            public string CompanyName { get; set; }
        }

        [Test]
        public void Can_select_Partial_SQL_Statements()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Shipper>();

                db.Insert(new Shipper { CompanyName = "Trains R Us", Phone = "555-TRAINS", ShipperTypeId = 1 });
                db.Insert(new Shipper { CompanyName = "Planes R Us", Phone = "555-PLANES", ShipperTypeId = 2 });
                db.Insert(new Shipper { CompanyName = "We do everything!", Phone = "555-UNICORNS", ShipperTypeId = 2 });

                var partialColumns = db.Select<SubsetOfShipper>(
                    db.From<Shipper>().Where(q => q.ShipperTypeId == 2));

                Assert.That(partialColumns.Map(x => x.Phone),
                    Is.EquivalentTo(new[] { "555-UNICORNS", "555-PLANES" }));
                Assert.That(partialColumns.Map(x => x.CompanyName),
                    Is.EquivalentTo(new[] { "Planes R Us", "We do everything!" }));


                var partialDto = db.Select(db.From<Shipper>()
                    .Select(x => new { x.Phone, x.CompanyName })
                    .Where(x => x.ShipperTypeId == 2));

                Assert.That(partialDto.Map(x => x.Phone),
                    Is.EquivalentTo(new[] { "555-UNICORNS", "555-PLANES" }));
                Assert.That(partialDto.Map(x => x.CompanyName),
                    Is.EquivalentTo(new[] { "Planes R Us", "We do everything!" }));


                partialDto = db.Select(db.From<Shipper>()
                    .Select("Phone, " + "CompanyName".SqlColumn(DialectProvider))
                    .Where(x => x.ShipperTypeId == 2));

                Assert.That(partialDto.Map(x => x.Phone),
                    Is.EquivalentTo(new[] { "555-UNICORNS", "555-PLANES" }));
                Assert.That(partialDto.Map(x => x.CompanyName),
                    Is.EquivalentTo(new[] { "Planes R Us", "We do everything!" }));
            }
        }

        public class Text
        {
             public string Name { get; set; }
        }

        [Test]
        public void Can_escape_wildcards()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Text>();

                db.Insert(new Text { Name = "a" });
                db.Insert(new Text { Name = "ab" });
                db.Insert(new Text { Name = "a_c" });
                db.Insert(new Text { Name = "a_cd" });
                db.Insert(new Text { Name = "abcd" });
                db.Insert(new Text { Name = "a%" });
                db.Insert(new Text { Name = "a%b" });
                db.Insert(new Text { Name = "a%bc" });
                db.Insert(new Text { Name = "a\\" });
                db.Insert(new Text { Name = "a\\b" });
                db.Insert(new Text { Name = "a\\bc" });
                db.Insert(new Text { Name = "a^" });
                db.Insert(new Text { Name = "a^b" });
                db.Insert(new Text { Name = "a^bc" });

                Assert.That(db.Count<Text>(q => q.Name == "a_"), Is.EqualTo(0));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith("a_")), Is.EqualTo(2));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith("a%")), Is.EqualTo(3));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith("a_c")), Is.EqualTo(2));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith(@"a\")), Is.EqualTo(3));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith(@"a\b")), Is.EqualTo(2));
                Assert.That(db.Count<Text>(q => q.Name.StartsWith(@"a^b")), Is.EqualTo(2));
                Assert.That(db.Count<Text>(q => q.Name.EndsWith(@"_cd")), Is.EqualTo(1));
                Assert.That(db.Count<Text>(q => q.Name.Contains(@"abc")), Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_have_multiple_escape_wildcards()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                db.Save(new Person
                {
                    FirstName = "First",
                    LastName = "Last",
                });

                var someText = "ast";

                var ev = db.From<Person>();
                ev.Where(p => p.FirstName.Contains(someText)
                    || p.LastName.Contains(someText));
                ev.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
                var rows = db.Select(ev);﻿

                Assert.That(rows.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_perform_case_sensitive_likes()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Text>();

                db.Insert(new Text { Name = "Apple" });
                db.Insert(new Text { Name = "ABCDE" });
                db.Insert(new Text { Name = "abc" });

                Func<string> normalizedSql = () =>
                    db.GetLastSql().Replace("\"", "").Replace("`", "").Replace("Name", "name").Replace("NAME", "name").Replace(":","@");

                var hold = OrmLiteConfig.StripUpperInLike; //.NET Core defaults to `true`
                OrmLiteConfig.StripUpperInLike = false;

                db.Count<Text>(q => q.Name.StartsWith("A"));
                Assert.That(normalizedSql(),
                    Does.Contain("upper(name) like @0".NormalizeSql()));

                db.Count<Text>(q => q.Name.EndsWith("e"));
                Assert.That(normalizedSql(),
                    Does.Contain("upper(name) like @0".NormalizeSql()));

                db.Count<Text>(q => q.Name.Contains("b"));
                Assert.That(normalizedSql(),
                    Does.Contain("upper(name) like @0".NormalizeSql()));

                OrmLiteConfig.StripUpperInLike = true;

                db.Count<Text>(q => q.Name.StartsWith("A"));
                Assert.That(normalizedSql(),
                    Does.Contain("name like @0".NormalizeSql()));

                db.Count<Text>(q => q.Name.EndsWith("e"));
                Assert.That(normalizedSql(),
                    Does.Contain("name like @0".NormalizeSql()));

                db.Count<Text>(q => q.Name.Contains("b"));
                Assert.That(normalizedSql(),
                    Does.Contain("name like @0".NormalizeSql()));

                OrmLiteConfig.StripUpperInLike = hold;
            }
        }

        class Record
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        class Output
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Test]
        public void Can_Select_Single_from_just_FROM_Expression()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Record>();

                db.InsertAll(new Record[] {
                    new Record { Id = 1, Name = "Record 1" }, 
                });

                var q = db.From<Record>();
                var results = db.Select<Output>(q);
                Assert.That(results.Count, Is.EqualTo(1));

                var result = db.Single<Output>(q);
                Assert.That(result, Is.Not.Null);
            }
        }

        [Test]
        public void Can_Select_Filed_Alias_Expression()
        {
            using var db = OpenDbConnection();
            var sql = db.From<Employee>()
                .Join<Company>((e, c) => e.CompanyId == c.Id)
                .Select<Employee, Company>((e, c) => new {
                        Id = e.Id,
                        Name = e.Name, // test this property use alias
                        CompanyName = c.Name
                    });
            Assert.That(sql.SelectExpression.NormalizeSql(), Is.EqualTo(
                "select employee.id, employee.employeename as name, company.companyname as companyname"));
        }
    }

    public class Submission
    {
        public int Id { get; set; }
        public DateTime StoryDate { get; set; }
        public string Headline { get; set; }
        public string Body { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        [Alias("CompanyName")] 
        public string Name { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [Alias("EmployeeName")] 
        public string Name { get; set; }
    }

}