using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    public class Sqltest
    {
        [AutoIncrement]
        public int Id { get; set; }
        public double Value { get; set; }
        public bool Bool { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    public class SqlServerDialectTests : OrmLiteProvidersTestBase
    {
        public SqlServerDialectTests(DialectContext context) : base(context) {}

        [Test]
        public void Does_concat_values()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                db.Insert(new Sqltest { Value = 123.456 });

                var sqlConcat = DialectProvider.SqlConcat(new object[]{ "'a'", 2, "'c'" });
                var result = db.Scalar<string>($"SELECT {sqlConcat} from sqltest");
                Assert.That(result, Is.EqualTo("a2c"));

                sqlConcat = DialectProvider.SqlConcat(new object[] { "'$'", "value" });
                result = db.Scalar<string>($"SELECT {sqlConcat} from sqltest");
                Assert.That(result, Is.EqualTo("$123.456"));
            }
        }

        [Test]
        public void Does_concat_values_in_SqlExpression()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                db.Insert(new Sqltest { Value = 123.456, Bool = true });

                var results = db.Select<Dictionary<string, object>>(db.From<Sqltest>()
                    .Select(x => new {
                        x.Id,
                        text = Sql.As(Sql.Cast(x.Id, Sql.VARCHAR) + " : " + Sql.Cast(x.Value, Sql.VARCHAR) + " : " + Sql.Cast(x.Bool, Sql.VARCHAR) + " string", "text") 
                    }));

                Assert.That(results[0]["text"], Is.EqualTo("1 : 123.456 : 1 string")
                                               .Or.EqualTo("1 : 123.456 : true string"));
            }
        }

        [Test]
        public void Does_concat_values_in_SqlExpression_using_tuple()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                db.Insert(new Sqltest { Value = 123.456 });

                var results = db.Select<(int id, string text)>(db.From<Sqltest>()
                    .Select(x => new {
                        x.Id,
                        text = Sql.Cast(x.Id, Sql.VARCHAR) + " : " + Sql.Cast(x.Value, Sql.VARCHAR) + " : " + Sql.Cast("1 + 2", Sql.VARCHAR) + " string"
                    }));

                Assert.That(results[0].text, Is.EqualTo("1 : 123.456 : 3 string"));
            }
        }

        [Test]
        public void Does_format_currency()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                db.Insert(new Sqltest { Value = 12 });

                var sqlCurrency = DialectProvider.SqlCurrency("12.3456");
                var result = db.Scalar<string>($"SELECT {sqlCurrency} from sqltest");
                Assert.That(result, Is.EqualTo("$12.35"));

                sqlCurrency = DialectProvider.SqlCurrency("12.3456", "£");
                result = db.Scalar<string>($"SELECT {sqlCurrency} from sqltest");
                Assert.That(result, Is.EqualTo("£12.35"));

                db.Insert(new Sqltest { Value = 12.3 });
                db.Insert(new Sqltest { Value = 12.34 });
                db.Insert(new Sqltest { Value = 12.345 });

                var sqlConcat = DialectProvider.SqlCurrency("value");
                var results = db.SqlList<string>($"SELECT {sqlConcat} from sqltest");

                Assert.That(results, Is.EquivalentTo(new[]
                {
                    "$12.00",
                    "$12.30",
                    "$12.34",
                    "$12.35",
                }));

                sqlConcat = DialectProvider.SqlCurrency("value", "£");
                results = db.SqlList<string>($"SELECT {sqlConcat} from sqltest");

                Assert.That(results, Is.EquivalentTo(new[]
                {
                    "£12.00",
                    "£12.30",
                    "£12.34",
                    "£12.35",
                }));
            }
        }

        [Test]
        public void Does_handle_booleans()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                db.Insert(new Sqltest { Value = 0, Bool = false });
                db.Insert(new Sqltest { Value = 1, Bool = true });

                var sqlBool = DialectProvider.SqlBool(false);
                var result = db.Scalar<double>($"SELECT Value from sqltest where Bool = {sqlBool}");
                Assert.That(result, Is.EqualTo(0));

                sqlBool = DialectProvider.SqlBool(true);
                result = db.Scalar<double>($"SELECT Value from sqltest where Bool = {sqlBool}");
                Assert.That(result, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_use_limit()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Sqltest>();

                5.Times(i => db.Insert(new Sqltest { Value = i + 1 }));

                var sqlLimit = DialectProvider.SqlLimit(rows: 1);
                var results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(1));

                sqlLimit = DialectProvider.SqlLimit(rows: 3);
                results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(6));

                sqlLimit = DialectProvider.SqlLimit(offset: 1);
                results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(14));

                sqlLimit = DialectProvider.SqlLimit(offset: 4);
                results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(5));

                sqlLimit = DialectProvider.SqlLimit(offset: 1, rows: 1);
                results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(2));

                sqlLimit = DialectProvider.SqlLimit(offset: 2, rows: 2);
                results = db.SqlList<double>($"SELECT Value from sqltest ORDER BY Id {sqlLimit}").Sum();
                Assert.That(results, Is.EqualTo(7));
            }
        }

    }
}
