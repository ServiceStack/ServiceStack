using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ObjectUtilsTests
    {
        public class Node
        {
            public Node(int id, params Node[] children)
            {
                Id = id;
                Children = children;
            }

            public int Id { get; set; }

            public Node[] Children { get; set; }
        }

        [Test]
        public void Can_detect_Circular_References_in_models()
        {
            var node = new Node(1,
                new Node(11, new Node(111)),
                new Node(12, new Node(121)));

            Assert.That(!ObjectUtils.HasCircularReferences(node));

            var root = new Node(1,
                new Node(11));

            var cyclicalNode = new Node(1, root);
            root.Children[0].Children = new[] { cyclicalNode };

            Assert.That(ObjectUtils.HasCircularReferences(root));
        }
    }
}