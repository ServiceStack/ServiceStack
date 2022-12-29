using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class SqlDialectTests : OrmLiteProvidersTestBase
    {
        public SqlDialectTests(DialectContext context) : base(context) { }

        [Test]
        public void Does_spread_values()
        {
            using var db = OpenDbConnection();
            var dialect = db.GetDialectProvider();
            Assert.That(dialect.SqlSpread((int[])null), Is.EqualTo(""));
            Assert.That(dialect.SqlSpread((string[])null), Is.EqualTo(""));
            Assert.That(dialect.SqlSpread(new int[0]), Is.EqualTo(""));
            Assert.That(dialect.SqlSpread(new string[0]), Is.EqualTo(""));
            Assert.That(dialect.SqlSpread(1, 2, 3), Is.EqualTo("1,2,3"));
            Assert.That(dialect.SqlSpread("A", "B", "C"), Is.EqualTo("'A','B','C'"));
            Assert.That(dialect.SqlSpread("A'B", "C\"D"), 
                Is.EqualTo("'A''B','C\"D'").Or.EqualTo("'A\\'B','C\"D'")); //MySql
        }
    }
}