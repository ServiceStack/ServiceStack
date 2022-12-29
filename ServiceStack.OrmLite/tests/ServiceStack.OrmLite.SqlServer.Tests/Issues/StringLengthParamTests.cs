using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    class StringLengthParamTest
    {
        [AutoIncrement]
        public int Id { get; set; }

        //[CustomField("VARCHAR(MAX)")]
        [StringLength(StringLengthAttribute.MaxText)]
        public string Name { get; set; }
    }

    public class StringLengthParamTests
        : OrmLiteTestBase
    {
        [Test]
        public void Can_select_param_greater_than_default_length()
        {
            var hold = OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode;
            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = false;

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<StringLengthParamTest>();

                var name = "a" + new string('b', 8000) + "c";

                db.Insert(new StringLengthParamTest
                {
                    Name = name
                });

                var people = db.Select<StringLengthParamTest>(p => p.Name == name);

                Assert.That(people.Count, Is.EqualTo(1));
            }

            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = hold;
        }

        [Test]
        public void Can_insert_only_param_greater_than_default_length()
        {
            var hold = OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode;
            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = true;

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<StringLengthParamTest>();

                var name = "a" + new string('b', 8000) + "c";

                db.InsertOnly(new StringLengthParamTest
                {
                    Name = name
                }, s => s.Name);

                var people = db.Select<StringLengthParamTest>(p => p.Name == name);

                Assert.That(people.Count, Is.EqualTo(1));
            }

            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = hold;
        }
    }
}