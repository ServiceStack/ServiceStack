using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class DumpTests
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

            Assert.That(!TypeSerializer.HasCircularReferences(node));

            var root = new Node(1,
                new Node(11));

            var cyclicalNode = new Node(1, root);
            root.Children[0].Children = new[] { cyclicalNode };

            Assert.That(TypeSerializer.HasCircularReferences(root));
        }

        [Test]
        public void Can_PrintDump_ToSafeJson_ToSafeJsv_recursive_Node()
        {
            var node = new Node(1,
                new Node(11, new Node(111)),
                new Node(12, new Node(121)));

            var root = new Node(1,
                new Node(11, new Node(111)),
                node);

            var cyclicalNode = new Node(1, root);
            root.Children[0].Children[0].Children = new[] { cyclicalNode };

            root.PrintDump();
            root.ToSafeJson().Print();
            root.ToSafeJsv().Print();
        }

        public class CustomExecption : Exception
        {
            public string[] CustomData { get; set; }
        }

        [Test]
        public void Can_PrintDump_ToSafeJson_ToSafeJsv_Exception()
        {
            try
            {
                throw new ArgumentException("param",
                    new CustomExecption
                    {
                        CustomData = new[] { "A", "B", "C"}
                    });
            }
            catch (Exception ex)
            {
                ex.PrintDump();
                ex.ToSafeJson().Print();
                ex.ToSafeJsv().Print();
            }
        }
    }
}