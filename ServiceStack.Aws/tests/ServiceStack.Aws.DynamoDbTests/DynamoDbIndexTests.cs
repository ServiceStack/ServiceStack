using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    //Poco Data Model for OrmLite + SeedData 
    [Route("/rockstars", "POST")]
    [References(typeof(RockstarAgeIndex))]
    [References(typeof(RockstarAgeAllIndex))]
    public class Rockstar
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public bool Alive { get; set; }

        public string Url => $"/stars/{(Alive ? "alive" : "dead")}/{LastName.ToLower()}/";

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age, bool alive)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            Alive = alive;
        }
    }

    public class RockstarAgeIndex : IGlobalIndex<Rockstar>
    {
        [HashKey]
        public int Age { get; set; }

        [RangeKey]
        public int Id { get; set; }
    }

    [ProjectionType(DynamoProjectionType.All)]
    public class RockstarAgeAllIndex : IGlobalIndex<Rockstar>
    {
        [HashKey]
        public int Age { get; set; }

        [RangeKey]
        public int Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Alive { get; set; }
    }

    public class SparseLocalIndex
    {
        public string HashKey { get; set; }
        public int RangeKey { get; set; }

        [Index]
        public string LocalIndex { get; set; }
    }

    [TestFixture]
    public class DynamoDbIndexTests : DynamoTestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_not_create_or_project_readonly_fields()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Rockstar>();
            db.InitSchema();

            var expectedFields = "Id, FirstName, LastName, Age, Alive";
            var table = db.GetTableMetadata<Rockstar>();
            Assert.That(string.Join(", ", table.Fields.Map(x => x.Name)), Is.EqualTo(expectedFields));

            var q = db.FromQueryIndex<RockstarAgeIndex>(x => x.Age == 27);
            q.Projection<Rockstar>();

            q.ProjectionExpression.Print();
            Assert.That(q.ProjectionExpression, Is.EqualTo(expectedFields));
        }

        [Test]
        public void Does_Create_Index_with_ALL_KEYS_projection()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Rockstar>();
            db.InitSchema();

            var expectedFields = "Id, FirstName, LastName, Age, Alive";
            var table = db.GetTableMetadata<Rockstar>();
            Assert.That(string.Join(", ", table.Fields.Map(x => x.Name)), Is.EqualTo(expectedFields));

            var q = db.FromQueryIndex<RockstarAgeAllIndex>(x => x.Age == 27);
            q.Projection<Rockstar>();

            q.ProjectionExpression.Print();
            Assert.That(q.ProjectionExpression, Is.EqualTo(expectedFields));
        }

        [Test]
        public void Can_Insert_Sparse_Index()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<SparseLocalIndex>();
            db.InitSchema();

            db.PutItem(new SparseLocalIndex { HashKey = "A", RangeKey = 1, LocalIndex = "Foo" });

            var result = db.FromQuery<SparseLocalIndex>(x => x.HashKey == "A")
                .LocalIndex(x => x.LocalIndex == "Foo")
                .Exec()
                .First();

            Assert.That(result.RangeKey, Is.EqualTo(1));

            db.PutItem(new SparseLocalIndex { HashKey = "A", RangeKey = 2, LocalIndex = null });

            result = db.FromQuery<SparseLocalIndex>(x => x.HashKey == "A")
                .Filter(x => Dynamo.AttributeNotExists(x.LocalIndex))
                .Exec()
                .First();

            Assert.That(result.RangeKey, Is.EqualTo(2));
        }
    }
}