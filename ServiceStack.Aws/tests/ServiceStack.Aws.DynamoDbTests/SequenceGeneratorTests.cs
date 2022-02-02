using System;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class SequenceGeneratorTests : DynamoTestBase
    {
        [Test]
        public void Can_increment_Seq()
        {
            var db = CreatePocoDynamo();
            db.InitSchema();

            var key = Guid.NewGuid().ToString();

            var nextId = db.Increment<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(1));

            nextId = db.Increment<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(2));

            nextId = db.Increment<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(12));

            nextId = db.Increment<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(22));

            nextId = db.Increment<Seq>(key, "Counter", 0);
            Assert.That(nextId, Is.EqualTo(22));
        }

        [Test]
        public void Can_decrement_Seq()
        {
            var db = CreatePocoDynamo();
            db.InitSchema();

            var key = Guid.NewGuid().ToString();

            var nextId = db.DecrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-1));

            nextId = db.DecrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-2));

            nextId = db.DecrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-12));

            nextId = db.DecrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-22));
        }

        [Test]
        public void Can_increment_Seq_by_expression()
        {
            var db = CreatePocoDynamo();
            db.InitSchema();

            var key = Guid.NewGuid().ToString();

            var nextId = db.IncrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));

            nextId = db.IncrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(2));

            nextId = db.DecrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));
        }

        [Test]
        public void Does_get_next_sequences()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            db.Sequences.Reset<Customer>();

            var nextId = db.Sequences.Increment<Customer>();

            Assert.That(nextId, Is.EqualTo(1));

            var nextIds = db.Sequences.GetNextSequences<Customer>(5);
            var expected = new[] { 2, 3, 4, 5, 6 };
            Assert.That(nextIds, Is.EquivalentTo(expected));

            nextIds = db.Sequences.GetNextSequences<Customer>(5);
            expected = new[] { 7, 8, 9, 10, 11 };
            Assert.That(nextIds, Is.EquivalentTo(expected));
        }
    }
}