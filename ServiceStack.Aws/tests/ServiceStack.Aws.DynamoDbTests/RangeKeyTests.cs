using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class RangeTest
    {
        public string Id { get; set; }

        [DynamoDBRangeKey]
        public DateTime CreatedDate { get; set; }

        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }

        protected bool Equals(RangeTest other)
        {
            
            var datesCloseEnough = Math.Abs(CreatedDate.Subtract(other.CreatedDate).TotalSeconds) < 1;
            return Id == other.Id && datesCloseEnough && string.Equals(Data, other.Data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RangeTest)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ (CreatedDate != default ? CreatedDate.GetHashCode() : 0);
            }
        }
    }

    public class RangeKeyTests : DynamoTestBase
    {
        [Test]
        public void Can_Create_RangeTest()
        {
            var db = CreatePocoDynamo();

            db.CreateTableIfMissing(DynamoMetadata.RegisterTable<RangeTest>());

            var createdDate = DateTime.UtcNow;
            db.PutItem(new RangeTest {
                Id = "test",
                CreatedDate = createdDate,
                Data = "Data",
            });

            var dto = db.GetItem<RangeTest>("test", createdDate);

            dto.PrintDump();

            Assert.That(dto.Id, Is.EqualTo("test"));
            Assert.That(dto.Data, Is.EqualTo("Data"));
            Assert.That(dto.CreatedDate, Is.EqualTo(createdDate)
                .Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public async Task Can_Create_RangeTest_Async()
        {
            var db = CreatePocoDynamo();

            await db.CreateTableIfMissingAsync(DynamoMetadata.RegisterTable<RangeTest>());

            var createdDate = DateTime.UtcNow;
            await db.PutItemAsync(new RangeTest {
                Id = "test",
                CreatedDate = createdDate,
                Data = "Data",
            });

            var dto = await db.GetItemAsync<RangeTest>("test", createdDate);

            dto.PrintDump();

            Assert.That(dto.Id, Is.EqualTo("test"));
            Assert.That(dto.Data, Is.EqualTo("Data"));
            Assert.That(dto.CreatedDate, Is.EqualTo(createdDate)
                .Within(TimeSpan.FromMinutes(1)));
        }
    }
}