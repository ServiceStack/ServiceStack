using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

public partial class OrmLiteSelectTests
{
    class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Area { get; set; }
        public NttType DstType { get; set; }
        public int Delete { get; set; }
    }

    [Alias(nameof(BaseResult))]
    class BaseResult
    {
        [ PrimaryKey]
        public string Id { get; set; }

        public string Code { get; set; }

        public virtual NttType DstType { get; set; }

        public string ResultType { get; set; }

        public virtual TestType Type { get; set; }
    }

    [Alias(nameof(BaseResult))]
    class VqdStateResult : BaseResult
    {
        public int Loss { get; set; }
        public int Color { get; set; }
    }

    [Alias(nameof(BaseResult))]
    class OcrResult : BaseResult
    {

    }

    [Alias(nameof(BaseResult))]
    class LinkResult : BaseResult
    {
        public StateType State { get; set; } = StateType.Unknow;
    }

    class Region
    {
        [ StringLength(60), PrimaryKey]
        public string Id { get; set; }
        public int Type { get; set; } = 0;
    }

    public enum StateType
    {
        Unknow = -1
    }

    enum TestType
    {
        Vqd,
        Osd,
        Link
    }

    enum NttType
    {
        Channel
    }

    [Test]
    public void Can_JoinAlias_In_Expression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Channel>();
        db.DropAndCreateTable<VqdStateResult>();
        db.DropAndCreateTable<Region>();

        if (!db.ColumnExists<LinkResult>(x => x.State))
            db.AddColumn<LinkResult>(x => x.State);

        var sqlExpression = db
            .From<Channel>(x =>
            {
                x.UseSelectPropertiesAsAliases = true;
                x.UseJoinTypeAsAliases = true;
            })
            .LeftJoin<Channel, Region>((x, y) => x.Area == y.Id)
            .LeftJoin<Channel, VqdStateResult>(
                (x, z) => x.Id == z.Code && (z.Type == TestType.Vqd) && (z.DstType == NttType.Channel),
                new TableOptions()
                {
                    Alias = "z"
                })
            .LeftJoin<Channel, OcrResult>(
                (x, m) => x.Id == m.Code && m.Type == TestType.Osd && m.DstType == NttType.Channel, new TableOptions()
                {
                    Alias = "m"
                })
            .LeftJoin<Channel, LinkResult>(
                (x, h) => x.Id == h.Code && h.Type == TestType.Link && h.DstType == NttType.Channel, new TableOptions()
                {
                    Alias = "h"
                })
            .Where(x => x.DstType == NttType.Channel && x.Delete == 0)
            .Select<Channel, Region, VqdStateResult, OcrResult, LinkResult>((x, y, z, m, h) => new
            {
                y.Type,
                LinkState = Sql.TableAlias(h.State, "z").ToString(),
                LinkState1 = h.State.ToString(),
                Loss = z.Loss,
                Color = z.Color,
            });

        var p = db.GetDialectProvider();
        var sql = sqlExpression.ToMergedParamsSelectStatement();
        Assert.AreEqual(true, sql.Contains($"z.{p.GetQuotedColumnName("State")}"));
        Assert.AreEqual(true, sql.Contains($"{p.GetQuotedColumnName("h")}.{p.GetQuotedColumnName("State")}"));
        Assert.AreEqual(true, sql.Contains($"{p.GetQuotedColumnName("z")}.{p.GetQuotedColumnName("Loss")}"));
        Assert.AreEqual(true, sql.Contains($"{p.GetQuotedColumnName("z")}.{p.GetQuotedColumnName("Color")}"));
    }
}
