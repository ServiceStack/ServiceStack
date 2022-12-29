using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests.Issues
{
    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public AvailableTags Tag { get; set; }
        public AvailableFlags Tags { get; set; }
    }

    public enum AvailableTags
    {
        other,
        glam,
        hiphop,
        grunge,
        funk
    }

    [Flags]
    public enum AvailableFlags
    {
        other,
        glam,
        hiphop,
        grunge,
        funk
    }

    [TestFixture, Explicit]
    public class SelectExpressionIssues
        : OrmLiteTestBase
    {
        [Test]
        public void Does_select_using_tags_collection()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Contact>();

                var tags = new[] { AvailableTags.funk, AvailableTags.glam }.ToList();

                db.Insert(new Contact
                {
                    Name = "Michael Jackson",
                    Email = "demo+mike@servicestack.net",
                    Age = 50,
                    Tag = AvailableTags.glam,
                    Tags = AvailableFlags.funk | AvailableFlags.glam,
                });

                var contacts = db.Select<Contact>(c => tags.Contains(c.Tag));
                db.GetLastSql().Print();
                contacts.PrintDump();

                contacts = db.Select<Contact>("Tags & @has = @has", new { has = AvailableFlags.glam });
                db.GetLastSql().Print();
                contacts.PrintDump();
            }
        }
    }
}