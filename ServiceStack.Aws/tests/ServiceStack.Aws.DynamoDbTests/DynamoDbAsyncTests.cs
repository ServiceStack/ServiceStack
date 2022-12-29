using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbAsyncTests : DynamoTestBase
    {
        [Test]
        public async Task Can_create_Tables_Async()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Poco>();

            await db.DeleteTableAsync<Poco>();

            var tables = (await db.GetTableNamesAsync()).ToList();
            Assert.That(!tables.Contains(nameof(Poco)));

            await db.InitSchemaAsync();

            tables = db.GetTableNames().ToList();
            Assert.That(tables.Contains(nameof(Poco)));
        }
    }
}