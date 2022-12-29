using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [Alias("aliased_table")]
    public class AliasedTable
    {
        [Alias("hash_key")]
        public string HashKey { get; set; }
        
        [Alias("range_key")]
        public string RangeKey { get; set; }
        
        [Alias("the_field")]
        public string TheField { get; set; }
    }
    
    public class DynamoDbQueryTests : DynamoTestBase
    {
        [Test]
        public void Query_does_uses_aliases()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<AliasedTable>();

            var q = db.FromQuery<AliasedTable>(
                    x => x.HashKey == "A" && x.RangeKey == "B")
                .Filter(x => x.TheField == "C");

            Assert.That(q.KeyConditionExpression, Is.EqualTo("((hash_key = :k0) AND (range_key = :k1))"));
            Assert.That(q.FilterExpression, Is.EqualTo("(the_field = :p0)"));
            Assert.That(q.TableName, Is.EqualTo("aliased_table"));
        }

        [Test]
        public void Scan_does_uses_aliases()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<AliasedTable>();

            var q = db.FromScan<AliasedTable>(
                    x => x.HashKey == "A" && x.RangeKey == "B" && x.TheField == "C");


            Assert.That(q.FilterExpression, Is.EqualTo("(((hash_key = :p0) AND (range_key = :p1)) AND (the_field = :p2))"));
            Assert.That(q.TableName, Is.EqualTo("aliased_table"));
        }
    }
}