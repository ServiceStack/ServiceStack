using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.MySql.Tests
{
    public class OrmLiteSaveStringValueTests : OrmLiteTestBase
    {
        [Test]
        public void Can_save_string_including_single_quote()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<StringTable>();
                db.CreateTable<StringTable>(true);

                var text = "It worked! Didn't it?";
                var row = new StringTable() {Value = text};

                db.Save(row);
                var id = db.LastInsertId();

                var selectedRow = db.SingleById<StringTable>(id);
                Assert.AreEqual(text, selectedRow.Value);
            }
        }

        [Test]
        public void Can_save_string_including_double_quote()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<StringTable>();
                db.CreateTable<StringTable>(true);

                var text = "\"It worked!\"";
                var row = new StringTable() { Value = text };

                db.Save(row);
                var id = db.LastInsertId();

                var selectedRow = db.SingleById<StringTable>(id);
                Assert.AreEqual(text, selectedRow.Value);
            }
        }

        [Test]
        public void Can_save_string_including_backslash()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<StringTable>();
                db.CreateTable<StringTable>(true);

                var text = "\\\\mycomputer\\hasashareddirectory";
                var row = new StringTable() { Value = text };

                db.Save(row);
                var id = db.LastInsertId();

                var selectedRow = db.SingleById<StringTable>(id);
                Assert.AreEqual(text, selectedRow.Value);
            }
        }
    }

    public class StringTable
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
