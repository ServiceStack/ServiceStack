using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Expression;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrderByTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_order_by_random()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        10.Times(i => db.Insert(new LetterFrequency { Letter = ('A' + i).ToString() }));

        var rowIds1 = db.Select(db.From<LetterFrequency>().OrderBy(x => x.Id)).Map(x => x.Id);
        var rowIds2 = db.Select(db.From<LetterFrequency>().OrderBy(x => x.Id)).Map(x => x.Id);

        Assert.That(rowIds1.SequenceEqual(rowIds2));

        rowIds1 = db.Select(db.From<LetterFrequency>().OrderByRandom()).Map(x => x.Id);
        rowIds2 = db.Select(db.From<LetterFrequency>().OrderByRandom()).Map(x => x.Id);

        Assert.That(!rowIds1.SequenceEqual(rowIds2));
    }

    [Test]
    public void Can_OrderBy_and_ThenBy()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency {Letter = "C" });
        db.Insert(new LetterFrequency {Letter = "C" });
        db.Insert(new LetterFrequency {Letter = "B" });
        db.Insert(new LetterFrequency {Letter = "A" });

        var q = db.From<LetterFrequency>();
        q.OrderBy(nameof(LetterFrequency.Letter))
            .ThenBy(nameof(LetterFrequency.Id));

        var tracks = db.Select(q);
                
        Assert.That(tracks.First().Letter, Is.EqualTo("A"));
        Assert.That(tracks.Last().Letter, Is.EqualTo("C"));
    }

    [Test]
    public void Can_OrderBy_multi_table_expression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterWeighting>();

        var letters = "A,B,C,D,E".Split(',');
        var i = 0;
        letters.Each(letter => {
            var id = db.Insert(new LetterFrequency {Letter = letter}, selectIdentity: true);
            db.Insert(new LetterWeighting {LetterFrequencyId = id, Weighting = ++i * 10});
        });

        var q = db.From<LetterFrequency>()
            .Join<LetterWeighting>()
            .OrderBy<LetterFrequency, LetterWeighting>((f, w) => f.Id > w.Weighting ? f.Id : w.Weighting);

        var results = db.Select(q);
    }

    [Test]
    public void Can_OrderByFields()
    {
        using var db = OpenDbConnection();
        var d = db.GetDialectProvider();
        Assert.That(OrmLiteUtils.OrderByFields(d,"a").NormalizeQuotes(), Is.EqualTo("'a'"));
        Assert.That(OrmLiteUtils.OrderByFields(d,"a,b").NormalizeQuotes(), Is.EqualTo("'a', 'b'"));
        Assert.That(OrmLiteUtils.OrderByFields(d,"-a,b").NormalizeQuotes(), Is.EqualTo("'a' desc, 'b'"));
        Assert.That(OrmLiteUtils.OrderByFields(d,"a,-b").NormalizeQuotes(), Is.EqualTo("'a', 'b' desc"));
    }
        
    [Test]
    public void Can_Multi_OrderBy_AliasValue()
    {
        using var db = OpenDbConnection();

        db.DropAndCreateTable<LetterFrequency>();
        var items = 5.Times(x => new LetterFrequency {
            Letter = $"{'A' + x}",
            Value = x + 1
        });
        db.InsertAll(items);

        OrmLiteUtils.PrintSql();
        var q = db.From<LetterFrequency>();
        q.OrderBy(x => new { x.Value });
        // q.OrderBy(x => x.Value);
        var results = db.Select(q);
            
        Assert.That(results.Map(x => x.Value), Is.EqualTo(new[]{ 1, 2, 3, 4, 5}));
    }
}