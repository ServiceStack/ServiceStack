using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class LRDCategoria : IHasId<int>
    {
        [Alias("IDDCATEGORIA")]
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Alias("CODICE")]
        [Required]
        [Index(Unique = true)]
        [StringLength(50)]
        public string Codice { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    public class NoUpdateIssue : OrmLiteProvidersTestBase
    {
        public NoUpdateIssue(DialectContext context) : base(context) {}

        [Test]
        public void Does_update_record()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<LRDCategoria>();

                db.Insert(new LRDCategoria { Codice = "A" });

                var row = db.Select<LRDCategoria>().FirstOrDefault();

                row.Codice = "";

                db.Update(row);

                row = db.Select<LRDCategoria>().FirstOrDefault();

                row.PrintDump();

                Assert.That(row.Codice, Is.EqualTo(""));
            }
        }
    }
}