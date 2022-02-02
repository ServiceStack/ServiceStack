using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class SequenceGeneratorTestsAsync : DynamoTestBase
    {
        [Test]
        public async Task Can_increment_Seq()
        {
            var db = CreatePocoDynamo();
            await db.InitSchemaAsync();

            var key = Guid.NewGuid().ToString();

            var nextId = await db.IncrementAsync<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(1));

            nextId = await db.IncrementAsync<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(2));

            nextId = await db.IncrementAsync<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(12));

            nextId = await db.IncrementAsync<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(22));

            nextId = await db.IncrementAsync<Seq>(key, "Counter", 0);
            Assert.That(nextId, Is.EqualTo(22));
        }

        [Test]
        public async Task Can_decrement_Seq()
        {
            var db = CreatePocoDynamo();
            await db.InitSchemaAsync();

            var key = Guid.NewGuid().ToString();

            var nextId = await db.DecrementByIdAsync<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-1));

            nextId = await db.DecrementByIdAsync<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-2));

            nextId = await db.DecrementByIdAsync<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-12));

            nextId = await db.DecrementByIdAsync<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-22));
        }

        [Test]
        public async Task Can_increment_Seq_by_expression()
        {
            var db = CreatePocoDynamo();
            await db.InitSchemaAsync();

            var key = Guid.NewGuid().ToString();

            var nextId = await db.IncrementByIdAsync<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));

            nextId = await db.IncrementByIdAsync<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(2));

            nextId = await db.DecrementByIdAsync<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));
        }

        [Test]
        public async Task Does_get_next_sequences()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            await db.InitSchemaAsync();

            await db.SequencesAsync.ResetAsync<Customer>();

            var nextId = await db.SequencesAsync.IncrementAsync<Customer>();

            Assert.That(nextId, Is.EqualTo(1));

            var nextIds = await db.SequencesAsync.GetNextSequencesAsync<Customer>(5);
            var expected = new[] { 2, 3, 4, 5, 6 };
            Assert.That(nextIds, Is.EquivalentTo(expected));

            nextIds = await db.SequencesAsync.GetNextSequencesAsync<Customer>(5);
            expected = new[] { 7, 8, 9, 10, 11 };
            Assert.That(nextIds, Is.EquivalentTo(expected));
        }
    }
}