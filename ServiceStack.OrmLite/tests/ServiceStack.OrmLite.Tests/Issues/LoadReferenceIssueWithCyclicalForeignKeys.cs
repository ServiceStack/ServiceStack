using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLiteDialects(Dialect.Sqlite | Dialect.MySql)]
public class LoadReferenceIssueWithCyclicalForeignKeys(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class BaseEntity
    {
        [AutoIncrement]
        [PrimaryKey]
        public long Id { get; set; }
    }

    public class ResearchEntity : BaseEntity
    {
        [References(typeof(NameEntity))]
        public long? PrimaryNameId { get; set; }

        [Reference]
        public NameEntity PrimaryName { get; set; }

        [Reference]
        public List<NameEntity> Names { get; set; } = new List<NameEntity>();
    }

    public class NameEntity : BaseEntity
    {
        public string Value { get; set; }

        [References(typeof(ResearchEntity))]
        public long ResearchId { get; set; }

        [Reference]
        public ResearchEntity Research { get; set; }
    }

    private void RecreateTables(IDbConnection db)
    {
        db.DisableForeignKeysCheck();
        db.DropTable<NameEntity>();
        db.DropTable<ResearchEntity>();
        db.CreateTable<NameEntity>();
        db.CreateTable<ResearchEntity>();
        db.EnableForeignKeysCheck();
    }

    [Test]
    public void Does_update_self_FK_Key_when_saving_references()
    {
        using var db = OpenDbConnection();
        RecreateTables(db);

        for (var i = 1; i <= 5; i++)
        {
            var research = new ResearchEntity();
            research.Names.Add(new NameEntity {Value = $"test {1 + i}"});
            research.Names.Add(new NameEntity {Value = $"test {2 + i}"});
            research.Names.Add(new NameEntity {Value = $"test {3 + i}"});

            db.Save(research, references: true);
            research.PrimaryNameId = research.Names[1].Id;
            db.Save(research);
        }

        var res = db.LoadSelect(
                db.From<ResearchEntity>().Where(x => x.Id == 5))
            .FirstNonDefault();
        Assert.That(res.PrimaryName.Id, Is.EqualTo(res.PrimaryNameId));
    }

    [Test]
    public async Task Does_update_self_FK_Key_when_saving_references_Async()
    {
        using var db = await OpenDbConnectionAsync();
        RecreateTables(db);

        for (var i = 1; i <= 5; i++)
        {
            var research = new ResearchEntity();
            research.Names.Add(new NameEntity {Value = $"test {1 + i}"});
            research.Names.Add(new NameEntity {Value = $"test {2 + i}"});
            research.Names.Add(new NameEntity {Value = $"test {3 + i}"});

            await db.SaveAsync(research, references: true);
            research.PrimaryNameId = research.Names[1].Id;
            await db.SaveAsync(research);
        }

        var res = (await db.LoadSelectAsync(
                db.From<ResearchEntity>().Where(x => x.Id == 5)))
            .FirstNonDefault();
        Assert.That(res.PrimaryName.Id, Is.EqualTo(res.PrimaryNameId));
    }
}