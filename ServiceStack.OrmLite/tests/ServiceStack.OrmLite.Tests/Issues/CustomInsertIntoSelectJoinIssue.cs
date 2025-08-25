using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

public class InsertIntoSource
{
    [AutoIncrement]
    public long Id { get; set; }

    public string Url { get; set; }
    public string Provider { get; set; }
    public string DomainKey { get; set; }
    public bool NoFollow { get; set; }
    public HttpStatusCode HttpStatusCode { get; set; }
    public DateTime? LastScanTime { get; set; }
    public string Anchors { get; set; }
    public int? OutboundLinks { get; set; }
    public long TargetDomainRecordId { get; set; }
    public long UserAuthCustomId { get; set; }
}

public class InertIntoTarget
{
    [PrimaryKey]
    public long WatchedUrlRecordId { get; set; }

    public string Url { get; set; }
    public string DomainKey { get; set; }
    public long TargetDomainRecordId { get; set; }
    public string TargetDomainKey { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public int Tries { get; set; }
    public DateTime? DeferUntil { get; set; }
    public long UserAuthCustomId { get; set; }
    public bool FirstScan { get; set; }
}

public class InsertIntoJoin
{
    [AutoIncrement]
    public long Id { get; set; }

    public string Url { get; set; }
    public string DomainKey { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.Now;
    public DateTime? DeleteDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool Active { get; set; } = true;
    public long UserAuthCustomId { get; set; }
}

public class CustomInsertIntoSelectJoinIssue : OrmLiteTestBase
{
    [Test]
    public void Can_custom_join_into_select()
    {
        using var db = OpenDbConnection();
            
        db.DropAndCreateTable<InsertIntoSource>();
        db.DropAndCreateTable<InsertIntoJoin>();
        db.DropAndCreateTable<InertIntoTarget>();
           
        var ids = new[] {1, 2, 3};
        // OrmLiteUtils.PrintSql();

        var q = db.From<InsertIntoSource>()
            .Join<InsertIntoJoin>((w, t) => w.TargetDomainRecordId == t.Id)
            .Where(x => Sql.In(x.Id, ids))
            .Select<InsertIntoSource, InsertIntoJoin>((w, t) => new {
                UserAuthCustomId = w.UserAuthCustomId,
                DomainKey = w.DomainKey,
                CreateDate = DateTime.UtcNow,
                DeferUntil = (DateTime?) null,
                TargetDomainKey = t.DomainKey,
                Tries = 0,
                TargetDomainRecordId = w.TargetDomainRecordId,
                Url = w.Url,
                WatchedUrlRecordId = w.Id
            });

        var inserted = db.InsertIntoSelect<InertIntoTarget>(q, dbCmd => dbCmd.OnConflictIgnore());
    }
}