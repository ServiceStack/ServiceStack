using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoDbContainsTests : DynamoTestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        private static Collection PutCollection(IPocoDynamo db)
        {
            db.RegisterTable<Collection>();
            db.InitSchema();

            var row = new Collection
            {
                Id = 1,
                Title = "Title 1"
            }
            .InitStrings(10.Times(i => ((char)('A' + i)).ToString()).ToArray())
            .InitInts(10.Times(i => i).ToArray());

            db.PutItem(row);
            return row;
        }

        [Test]
        public void Can_Scan_complex_expression()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>(x =>
                x.SetStrings.Contains("A")
                && x.Title.StartsWith("T")
                && x.Title.Contains("itl")
                && x.ArrayInts.Length == 10)).ToList();

            Assert.That(results[0], Is.EqualTo(row));
        }

        [Test]
        public void Can_Scan_with_contains_sets()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>().Filter("contains(SetStrings, :s)", new { s = "A" })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter("contains(SetInts, :i)", new { i = 1 })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => x.SetStrings.Contains("A"))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => x.SetInts.Contains(1))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            //Does not match
            results = db.Scan(db.FromScan<Collection>().Filter("contains(SetStrings, :s)", new { s = "K" })).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>().Filter("contains(SetStrings, :i)", new { i = 10 })).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => x.SetStrings.Contains("K"))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => x.SetInts.Contains(10))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Scan_with_not_contains_sets()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>().Filter("not contains(SetStrings, :s)", new { s = "A" })).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => !x.SetStrings.Contains("A"))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => !x.ListStrings.Contains("A"))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => !x.SetInts.Contains(1))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            results = db.Scan(db.FromScan<Collection>(x => !x.ListInts.Contains(1))).ToList();
            Assert.That(results.Count, Is.EqualTo(0));

            /* does not match */
            results = db.Scan(db.FromScan<Collection>(x => !x.SetStrings.Contains("K"))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => !x.ListInts.Contains(10))).ToList();
            Assert.That(results[0], Is.EqualTo(row));
        }

        [Test]
        public void Can_Scan_with_contains_on_Lists()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>().Filter("contains(ListStrings, :s)", new { s = "A" })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter("contains(ListInts, :i)", new { i = 1 })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter(x => x.ListStrings.Contains("A"))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter(x => x.ListInts.Contains(1))).ToList();
            Assert.That(results[0], Is.EqualTo(row));
        }

        [Test]
        public void Can_Scan_with_contains_on_Arrays()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>().Filter("contains(ArrayStrings, :s)", new { s = "A" })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter("contains(ArrayInts, :i)", new { i = 1 })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => x.ArrayStrings.Contains("A"))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => x.ArrayInts.Contains(1))).ToList();
            Assert.That(results[0], Is.EqualTo(row));
        }

        [Test]
        public void Can_Scan_with_attribute_type()
        {
            var db = CreatePocoDynamo();
            var row = PutCollection(db);

            var results = db.Scan(db.FromScan<Collection>().Filter("attribute_type(Title, :s)", new { s = "S" })).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>(x => Dynamo.AttributeType(x.Title, DynamoType.String))).ToList();
            Assert.That(results[0], Is.EqualTo(row));

            results = db.Scan(db.FromScan<Collection>().Filter("attribute_type(Title, :s)", new { s = "N" })).ToList();
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Filter_field_IN_Collection()
        {
            var roles = new[] { "A", "B", "C" };
            var db = CreatePocoDynamo();
            db.RegisterTable<UserAuthRole>();
            db.InitSchema();

            db.PutItem(new UserAuthRole {
                UserAuthId = 1,
                Id = 2,
                Role = "B"
            });

            var dbRoles = db.FromQuery<UserAuthRole>(x => x.Id == 2)
                .Filter(x => roles.Contains(x.Role))
                .Exec()
                .ToList();

            Assert.That(dbRoles.Count, Is.EqualTo(1));

            var rolesList = roles.ToList();
            dbRoles = db.FromQuery<UserAuthRole>(x => x.Id == 2)
                .Filter(x => rolesList.Contains(x.Role))
                .Exec()
                .ToList();

            dbRoles.PrintDump();

            Assert.That(dbRoles.Count, Is.EqualTo(1));
        }
    }
}