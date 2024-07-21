using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests.Async;

public class PocoWithBytes : IHasGuidId
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public byte[] Image { get; set; }

    public string ContentType { get; set; }
}

[TestFixtureOrmLite]
public class SelectAsyncTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_SELECT_SingleAsync()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Poco>();

        await db.InsertAsync(new Poco { Id = 1 });

        var row = await db.SingleAsync(db.From<Poco>().Where(x => x.Id == 1));

        Assert.That(row.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_SELECT_SingleAsyncForStrangeClass()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<PocoWithBytes>();

        var bar = new PocoWithBytes { Id = Guid.NewGuid(), Image = new byte[1024 * 10], ContentType = "image/jpeg" };
        await db.InsertAsync(bar);

        var blah = await db.SingleAsync(db.From<PocoWithBytes>().Where(x => x.Id == bar.Id));
        Assert.That(blah, Is.Not.Null);
    }
}