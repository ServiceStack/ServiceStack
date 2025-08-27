using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

public class Tasked
{
    [AutoIncrement]
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public DateTime Created { get; set; }
}
    
public class AliasedTable
{
    [AutoIncrement]
    [PrimaryKey]
    [Alias("MAINID")]
    public int Id { get; set; }

    [Alias("DESCRIPTION")]
    public string Description { get; set; }
}

public class AliasedTableAlt
{
    [AutoIncrement]
    [PrimaryKey]
    [Alias("ALTID")]
    public int Id { get; set; }

    [Alias("DESCRIPTION")]
    public string Description { get; set; }
}

[TestFixtureOrmLite]
public class JoinAliasTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    [IgnoreDialect(Tests.Dialect.AnyPostgreSql | Tests.Dialect.AnyMySql, "Invalid Custom SQL for provider naming convention")]
    public void Can_use_JoinAlias_in_condition()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        var childId = db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity:true);

        var fromDateTime = new DateTime(2000, 02, 02);

        var q = db.From<Tasked>()
            .CustomJoin("LEFT JOIN Tasked history ON (Tasked.Id = history.ParentId)")
            .Where("history.\"Created\" >= {0} OR Tasked.\"Created\" >= {0}", fromDateTime);

        //doesn't work with Self Joins
        //var q = db.From<Task>()
        //    .LeftJoin<Task, Task>((parent, history) => (parent.Id == history.ParentId)
        //            && (history.CreatedAt >= fromDateTime || parent.CreatedAt >= fromDateTime)
        //        ,db.JoinAlias("history"));

        var results = db.Select(q);

        db.GetLastSql().Print();

        results.PrintDump();
    }

    [Test]
    public void Can_use_Column_to_resolve_properties()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        var childId = db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity: true);

        var q = db.From<Tasked>();

        var leftJoin = 
            $"LEFT JOIN Tasked history ON ({q.Column<Tasked>(t => t.Id, prefixTable:true)} = history.{q.Column<Tasked>(t => t.ParentId)})";
        Assert.That(leftJoin, Is.EqualTo(
            $"LEFT JOIN Tasked history ON ({q.Table<Tasked>()}.{q.Column<Tasked>(t => t.Id)} = history.{q.Column<Tasked>(t => t.ParentId)})"));
        Assert.That(leftJoin, Is.EqualTo(
            $"LEFT JOIN Tasked history ON ({q.Table<Tasked>()}.{q.Column<Tasked>(nameof(Tasked.Id))} = history.{q.Column<Tasked>(nameof(Tasked.ParentId))})"));

        q.CustomJoin(leftJoin);

        var results = db.Select(q);

        db.GetLastSql().Print();

        Assert.That(results.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Can_get_RowCount_of_duplicate_aliases_in_AliasedTable()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<AliasedTable>();
        db.DropAndCreateTable<AliasedTableAlt>();

        var q = db.From<AliasedTable>()
            .Join<AliasedTable, AliasedTableAlt>((mainTable, altTable) => mainTable.Id == altTable.Id)
            .Where(x => x.Id == 1);

        var result = await db.RowCountAsync(q);
    }

    [Test]
    public void Can_use_JoinAlias_on_source_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        var childId = db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity: true);

        var q = db.From<Tasked>(db.TableAlias("s"))
            .Join<Tasked>((t1, t2) => t1.ParentId == t2.Id, db.TableAlias("t"));

        var rows = db.Select(q);
        rows.PrintDump();
                
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Id, Is.EqualTo(2));
        Assert.That(rows[0].ParentId, Is.EqualTo(1));
    }

    [Test]
    public void Can_create_mixed_join_expression_with_inner_classes()
    {
        using var db = OpenDbConnection();
        /*
          SELECT  DISTINCT m.Id
            FROM  Core.Context cx
             LEFT JOIN Core.Client_Contact cc ON cx.ContactId = cc.ContactId AND cx.SystemId = cc.SystemId
             LEFT JOIN Core.Matter m ON (cc.ClientId = m.ClientId AND cc.SystemId = m.SystemId)
                    OR (cx.MatterId = m.Id AND cx.SystemId = m.SystemId)
           WHERE  cx.ContactId = 231
            AND   cx.SystemId = 0
         */

        var contactId = 231;
        var systemId = 0;
        var expr = "(cx.MatterId = m.Id AND cx.SystemId = m.SystemId)";

        var q = db.From<DbCore.Context>(db.TableAlias("cx"));
        expr = string.Format("(cx.{0} = m.{1} AND cx.{2} = m.{3})",
            q.Column<DbCore.Context>(cx => cx.MatterId),
            q.Column<DbCore.Matter>(m => m.Id),
            q.Column<DbCore.Context>(cx => cx.SystemId),
            q.Column<DbCore.Matter>(m => m.SystemId));

        var query = q
            .LeftJoin<DbCore.Context, DbCore.ClientContact>(
                (cx, cc) => cx.ContactId == cc.ContactId && cx.SystemId == cc.SystemId, db.TableAlias("cc"))
            .LeftJoin<DbCore.ClientContact, DbCore.Matter>((cc, m) =>
                (Sql.TableAlias(cc.ClientId, "cc") == m.ClientId && 
                 Sql.TableAlias(cc.SystemId, "cc") == m.SystemId)
                || Sql.Custom<bool>(expr), db.TableAlias("m"))
            .Where(cx => cx.ContactId == contactId && cx.SystemId == systemId)
            .SelectDistinct<DbCore.Matter>(m => m.Id);
        
        query.ToSelectStatement().Print();
        
        Assert.That(query.ToSelectStatement().NormalizeSql(), 
        Does.Contain("left join matter m on (((cc.clientid = m.clientid) and (cc.systemid = m.systemid))"));
    }
}

public class DbCore
{
    public class Context
    {
        public int ContactId { get; set; }
        public int MatterId { get; set; }
        public int SystemId { get; set; }
        public int ClientId { get; set; }
    }
    public class ClientContact
    {
        public int SystemId { get; set; }
        public int ContactId { get; set; }
        public int ClientId { get; set; }
    }
    public class Matter
    {
        public int ClientId { get; set; }
        public int SystemId { get; set; }
        public int Id { get; set; }
    }
}