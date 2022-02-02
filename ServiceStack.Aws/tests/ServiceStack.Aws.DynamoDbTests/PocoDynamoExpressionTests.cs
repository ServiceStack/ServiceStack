using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class PocoDynamoExpressionTests : DynamoTestBase
    {
        public PocoDynamoExpression Parse<T>(Expression<Func<T, bool>> predicate)
        {
            return PocoDynamoExpression.Create(typeof(T), predicate);
        }

        private static IPocoDynamo InitTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.RegisterTable<Collection>();
            db.InitSchema();
            return db;
        }

        [Test]
        public void Does_Parse_complex_expression()
        {
            InitTypes();

            var q = Parse<Collection>(x =>
                x.SetStrings.Contains("A")
                && x.Title.StartsWith("N")
                && x.Title.Contains("ame")
                && x.ArrayInts.Length == 10);

            Assert.That(q.FilterExpression, Is.EqualTo(
                "(((contains(SetStrings, :p0) AND begins_with(Title, :p1)) AND contains(Title, :p2)) AND (size(ArrayInts) = :p3))"));
            Assert.That(q.Params.Count, Is.EqualTo(4));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
            Assert.That(q.Params[":p1"], Is.EqualTo("N"));
            Assert.That(q.Params[":p2"], Is.EqualTo("ame"));
            Assert.That(q.Params[":p3"], Is.EqualTo(10));
        }

        [Test]
        public void Does_serialize_expression()
        {
            InitTypes();

            var q = Parse<Poco>(x => x.Id < 5);

            Assert.That(q.FilterExpression, Is.EqualTo("(Id < :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo(5));
        }

        [Test]
        public void Does_serialize_begins_with()
        {
            InitTypes();

            var q = Parse<Poco>(x => x.Title.StartsWith("Name 1"));

            Assert.That(q.FilterExpression, Is.EqualTo("begins_with(Title, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("Name 1"));

            q = Parse<Poco>(x => Dynamo.BeginsWith(x.Title, "Name 1"));
            Assert.That(q.FilterExpression, Is.EqualTo("begins_with(Title, :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo("Name 1"));
        }

        [Test]
        public void Does_serialize_contains_set()
        {
            InitTypes();

            var q = Parse<Collection>(x => x.SetStrings.Contains("A"));

            Assert.That(q.FilterExpression, Is.EqualTo("contains(SetStrings, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));

            q = Parse<Collection>(x => Dynamo.Contains(x.ListInts, 1));
            Assert.That(q.FilterExpression, Is.EqualTo("contains(ListInts, :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(1));
        }

        [Test]
        public void Does_serialize_not_contains_set()
        {
            InitTypes();

            var q = Parse<Collection>(x => !x.SetStrings.Contains("A"));

            Assert.That(q.FilterExpression, Is.EqualTo("not contains(SetStrings, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));

            q = Parse<Collection>(x => !Dynamo.Contains(x.ListInts, 1));
            Assert.That(q.FilterExpression, Is.EqualTo("not contains(ListInts, :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(1));
        }

        [Test]
        public void Does_serialize_In()
        {
            InitTypes();

            var letters = new[] { "A", "B", "C" };

            var q = Parse<Collection>(x => letters.Contains(x.Title));

            Assert.That(q.FilterExpression, Is.EqualTo("Title IN (:p0,:p1,:p2)"));
            Assert.That(q.Params.Count, Is.EqualTo(3));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
            Assert.That(q.Params[":p1"], Is.EqualTo("B"));
            Assert.That(q.Params[":p2"], Is.EqualTo("C"));

            q = Parse<Collection>(x => Dynamo.In(x.Title, letters));
            Assert.That(q.FilterExpression, Is.EqualTo("Title IN (:p0,:p1,:p2)"));
            Assert.That(q.Params.Count, Is.EqualTo(3));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
            Assert.That(q.Params[":p1"], Is.EqualTo("B"));
            Assert.That(q.Params[":p2"], Is.EqualTo("C"));
        }

        [Test]
        public void Does_serialize_Between()
        {
            InitTypes();

            var q = Parse<Collection>(x => Dynamo.Between(x.Title, "A", "Z"));
            Assert.That(q.FilterExpression, Is.EqualTo("Title BETWEEN :p0 AND :p1"));
            Assert.That(q.Params.Count, Is.EqualTo(2));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
            Assert.That(q.Params[":p1"], Is.EqualTo("Z"));
        }

        [Test]
        public void Does_serialize_attribute_type()
        {
            InitTypes();

            var q = Parse<Collection>(x => Dynamo.AttributeType(x.Title, DynamoType.String));

            Assert.That(q.FilterExpression, Is.EqualTo("attribute_type(Title, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("S"));
        }

        [Test]
        public void Does_serialize_attribute_exists()
        {
            InitTypes();

            var q = Parse<Collection>(x => Dynamo.AttributeExists(x.Title));

            Assert.That(q.FilterExpression, Is.EqualTo("attribute_exists(Title)"));
            Assert.That(q.Params.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_serialize_attribute_not_exists()
        {
            InitTypes();

            var q = Parse<Collection>(x => Dynamo.AttributeNotExists(x.Title));

            Assert.That(q.FilterExpression, Is.EqualTo("attribute_not_exists(Title)"));
            Assert.That(q.Params.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_serialize_Size()
        {
            InitTypes();

            var q = Parse<Collection>(x => Dynamo.Size(x.Title) > 3);

            Assert.That(q.FilterExpression, Is.EqualTo("(size(Title) > :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));

            q = Parse<Collection>(x => Dynamo.Size(x.SetInts) > 3);
            Assert.That(q.FilterExpression, Is.EqualTo("(size(SetInts) > :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));

            q = Parse<Collection>(x => Dynamo.Size(x.ListStrings) > 3);
            Assert.That(q.FilterExpression, Is.EqualTo("(size(ListStrings) > :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));
        }

        [Test]
        public void Does_serialize_Length()
        {
            InitTypes();

            var q = Parse<Collection>(x => x.Title.Length > 3);

            Assert.That(q.FilterExpression, Is.EqualTo("(size(Title) > :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));

            q = Parse<Collection>(x => x.SetInts.Count > 3);
            Assert.That(q.FilterExpression, Is.EqualTo("(size(SetInts) > :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));

            q = Parse<Collection>(x => x.ArrayInts.Length > 3);
            Assert.That(q.FilterExpression, Is.EqualTo("(size(ArrayInts) > :p0)"));
            Assert.That(q.Params[":p0"], Is.EqualTo(3));
        }
    }
}