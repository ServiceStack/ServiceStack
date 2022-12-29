using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture, Explicit]
    public class AddHocPocoDynamoTests : DynamoTestBase
    {
        [Test]
        public void Can_get_Customer()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            var dbCustomer = db.GetItem<Customer>(1);

            dbCustomer.PrintDump();
        }

        [Test]
        public void Can_Put_Get_and_Delete_Deeply_Nested_Nodes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Node>();
            db.InitSchema();

            var nodes = new Node(1, "/root",
                new List<Node>
                {
                    new Node(2,"/root/2", new[] {
                        new Node(4, "/root/2/4", new [] {
                            new Node(5, "/root/2/4/5", new[] {
                                new Node(6, "/root/2/4/5/6"),
                            }),
                        }),
                    }),
                    new Node(3, "/root/3"),
                });

            db.PutItem(nodes);

            var dbNodes = db.GetItem<Node>(1);

            dbNodes.PrintDump();

            Assert.That(dbNodes, Is.EqualTo(nodes));
        }
    }
}