using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixture]
    public class DateTimeColumnTest
        : OrmLiteTestBase
    {
        [Test]
        public void Can_create_table_containing_DateTime_column()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<Analyze>(true);
            }
        }

        [Test]
        public void Can_store_DateTime_Value()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<Analyze>(true);

                var obj = new Analyze {
                    Id = 1,
                    Date = DateTime.Now,
                    Url = "http://www.google.com"
                };

                db.Save(obj);
            }
        }

        [Test]
        public void Can_store_and_retrieve_DateTime_Value()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<Analyze>(true);

                var obj = new Analyze {
                    Id = 1,
                    Date = DateTime.Now,
                    Url = "http://www.google.com"
                };

                db.Save(obj);

                var target = db.SingleById<Analyze>(obj.Id);

                Assert.IsNotNull(target);
                Assert.AreEqual(obj.Id, target.Id);
                Assert.AreEqual(obj.Date.ToString("yyyy-MM-dd HH:mm:ss"), target.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                Assert.AreEqual(obj.Url, target.Url);
            }
        }

        /// <summary>
        /// Provided by RyogoNA in issue #38 https://github.com/ServiceStack/ServiceStack.OrmLite/issues/38#issuecomment-4625178
        /// </summary>
        [Alias("Analyzes")]
        public class Analyze : IHasId<int>
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id
            {
                get;
                set;
            }
            [Alias("AnalyzeDate")]
            public DateTime Date
            {
                get;
                set;
            }
            public string Url
            {
                get;
                set;
            }
        }

        [Test]
        public void Can_update_record_with_DateTime_Value()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<Analyze>(true);

                var obj = new Analyze
                {
                    Id = 1,
                    Date = DateTime.Now,
                    Url = "http://www.google.com"
                };

                db.Save(obj);

                obj = db.SingleById<Analyze>(obj.Id);
                Assert.IsNotNull(obj);


                obj.Date = new DateTime(1899, 12, 31);
                db.Update(obj); // this line throws the following exception: Oracle.DataAccess.Client.OracleException : ORA-00932: inconsistent datatypes: expected TIMESTAMP got NUMBER

                var target = db.SingleById<Analyze>(obj.Id);

                Assert.AreEqual(obj.Id, target.Id);
                Assert.AreEqual(obj.Date.ToString("yyyy-MM-dd HH:mm:ss"), target.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                Assert.AreEqual(obj.Url, target.Url);
            }
        }
    }
}
