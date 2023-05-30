using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

public partial class OrmLiteSelectTests
{
    [Alias("实体_配置")]
    class Channel
    {
        [Alias("编码"), Index]
        public string Id { get; set; }
        [Alias("名称")]
        public string Name { get; set; }
        [Alias("区域编码")]
        public string Area { get; set; }
        [Alias("类型"), Index]
        public NttType DstType { get; set; }
        [Alias("删除状态")]
        public int Delete { get; set; }
    }

    [Alias("实体_状态")]
    class BaseResult
    {
        [Alias("编码"), PrimaryKey]
        public string Id { get; set; }

        [Alias("目标编码")]
        public string Code { get; set; }

        [Alias("目标类型")]
        public virtual NttType DstType { get; set; }

        [Alias("状态类型")]
        public string ResultType { get; set; }

        [Alias("检测项类型")]
        public virtual TestType Type { get; set; }
    }

    class VqdStateResult : BaseResult
    {
        [Alias("丢失")]
        public int 丢失状态 { get; set; }
        [Alias("失色")]
        public int 失色状态 { get; set; }
    }

    class OcrResult : BaseResult
    {

    }

    class LinkResult : BaseResult
    {
        [Alias("文1")]
        public StateType State { get; set; } = StateType.未知;
    }

    [Alias("框架_区域")]
    class Region
    {
        [Alias("编码"), StringLength(60), PrimaryKey]
        public string Id { get; set; }
        [Alias("类型")]
        public int Type { get; set; } = 0;
    }

    public enum StateType
    {
        未知 = -1
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
        // DbFactory.DialectProvider = MySqlDialect.Provider;
        // DbFactory.ConnectionString = "Server=82.157.15.104;Port=3306;User Id=root;Password=tnT1R*k0+Eku;Database=demo-ormlite;Pooling=true;Allow User Variables=True;SslMode=none;";

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
                z.丢失状态
            });

        var p = db.GetDialectProvider();
        var sql = sqlExpression.ToMergedParamsSelectStatement();
        Assert.AreEqual(true, sql.Contains($"z.{p.GetQuotedColumnName("文1")}"));
        Assert.AreEqual(true, sql.Contains($"{p.GetQuotedColumnName("h")}.{p.GetQuotedColumnName("文1")}"));
        Assert.AreEqual(true, sql.Contains($"{p.GetQuotedColumnName("z")}.{p.GetQuotedColumnName("丢失")}"));
    }
}
