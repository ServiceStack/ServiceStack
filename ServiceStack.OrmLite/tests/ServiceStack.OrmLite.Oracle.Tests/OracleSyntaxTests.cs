using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixture]
    public class OracleSyntaxTests : OrmLiteTestBase
    {
        [Test]
        public void can_generate_correct_paging_if_first_column_must_be_quoted()
        {
            using(var db = OpenDbConnection()) {
                db.CreateTable<FirstColMustBeQuoted>(true);

                var noRows = db.Select(db.From<FirstColMustBeQuoted>().Limit(100));

                Assert.AreEqual(0, noRows.Count());

                for(int i = 0; i < 150; i++) {
                    db.Insert(new FirstColMustBeQuoted { COMMENT = "row #" + i });
                }

                var hundredRows = db.Select(db.From<FirstColMustBeQuoted>().Limit(100));
                Assert.AreEqual(100, hundredRows.Count());
            }
        }

        [Test]
        public void can_generate_correct_paging_if_first_column_dont_have_to_be_quoted()
        {
            using(var db = OpenDbConnection()) {
                db.CreateTable<FirstColNoQuotes>(true);

                var noRows = db.Select(db.From<FirstColNoQuotes>().Limit(100));

                Assert.AreEqual(0, noRows.Count());

                for(int i = 0; i < 150; i++) {
                    db.Insert(new FirstColNoQuotes { COMMENT = "row #" + i });
                }

                var hundredRows = db.Select(db.From<FirstColNoQuotes>().Limit(100));
                Assert.AreEqual(100, hundredRows.Count());
            }
        }

        [Test]
        public void can_generate_correct_paging_if_TABLE_NAME_must_be_quoted_and_first_column_have_to_be_quoted()
        {
            using(var db = OpenDbConnection()) {
                db.CreateTable<COMMENT_first>(true);

                var noRows = db.Select(db.From<COMMENT_first>().Limit(100));

                Assert.AreEqual(0, noRows.Count());

                for (var i = 0; i < 150; i++) {
                    db.Insert(new COMMENT_first { COMMENT = "COMMENT row #" + i });
                }

                var hundredRows = db.Select(db.From<COMMENT_first>().Limit(100));
                Assert.AreEqual(100, hundredRows.Count());
            }
        }

        [Test]
        public void can_generate_correct_paging_if_TABLE_NAME_must_be_quoted_and_first_column_dont_have_to_be_quoted()
        {
            using(var db = OpenDbConnection()) {
                db.CreateTable<COMMENT_other>(true);

                var noRows = db.Select(db.From<COMMENT_other>().Limit(100));

                Assert.AreEqual(0, noRows.Count());

                for (var i = 0; i < 150; i++) {
                    db.Insert(new COMMENT_other { COMMENT = "COMMENT row #" + i });
                }

                var hundredRows = db.Select(db.From<COMMENT_other>().Limit(100));
                Assert.AreEqual(100, hundredRows.Count());
            }
        }

        [Alias("COMMENT")]
        private class COMMENT_first
        {
            public string COMMENT { get; set; }

            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
        }

        [Alias("COMMENT")]
        private class COMMENT_other
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string COMMENT { get; set; }
        }

        private class FirstColMustBeQuoted
        {
            public string COMMENT { get; set; }

            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
        }

        private class FirstColNoQuotes//Oracle: max 30 characters...
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string COMMENT { get; set; }
        }
    }
}
