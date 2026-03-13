using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

public class UpdateIssueTests
{
    [Test]
    public async Task Can_update()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            ":memory:",
            SqliteDialect.Provider);

        using var db = await dbFactory.OpenDbConnectionAsync();

        db.DropAndCreateTable<Entity1>();
        db.DropAndCreateTable<Entity2>();

        var entityId = 1;
        var now = DateTime.UtcNow.AddMinutes(-10);

        await db.InsertAsync(new Entity1
        {
            Id = entityId,
            Count = 10,
            ModifiedDate = now
        });

        await db.InsertAsync(new Entity2
        {
            Id = 1,
            Entity1Id = entityId,
            Count = 20,
            ModifiedDate = now
        });

        await db.UpdateAddAsync(
            () => new Entity1 { Count = 5 },
            where: p => p.Id == entityId);

        await db.UpdateAddAsync(
            () => new Entity2 { Count = 5 },
            where: p => p.Entity1Id == entityId);

        var updatedEntity1 = await db.SingleByIdAsync<Entity1>(entityId);
        var updatedEntity2 = await db.SingleAsync<Entity2>(x => x.Entity1Id == entityId);
    }

    public class Entity1 
    {
        public int Id { get; set; }
    
        public int Count { get; set; }

        public DateTime ModifiedDate { get; set; }
    }

    public class Entity2
    {
        public int Id { get; set; }

        public int Entity1Id { get; set; }
    
        public int Count { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
    
}
