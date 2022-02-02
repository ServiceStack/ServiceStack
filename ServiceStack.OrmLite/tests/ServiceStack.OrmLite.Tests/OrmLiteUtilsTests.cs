using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixture]
    public class OrmLiteUtilsTests
    {
        [Test]
        public void ParseTokens()
        {
            var sql =
                @"(CASE WHEN NOT (((""table_1"".""_display_name"") is null or (""table_1"".""_display_name"") = '')) THEN (""table_1"".""_display_name"") ELSE ""table_1"".""_name"" END) + ""tabel_2"".""_name""";
            var tokens = OrmLiteUtils.ParseTokens(sql);

            Assert.AreEqual(1,  tokens.Count);
        }

        [Test]
        public void Can_UnquotedColumnName()
        {
            Assert.That(OrmLiteUtils.UnquotedColumnName("Col"), Is.EqualTo("Col"));
            Assert.That(OrmLiteUtils.UnquotedColumnName("\"Col\""), Is.EqualTo("Col"));
            Assert.That(OrmLiteUtils.UnquotedColumnName("Table.Col"), Is.EqualTo("Col"));
            Assert.That(OrmLiteUtils.UnquotedColumnName("\"Table\".\"Col\""), Is.EqualTo("Col"));
        }
    }
}