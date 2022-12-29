using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.Utils
{
    [TestFixture]
    public class JsvFormatterTests
    {
        [Test]
        public void Can_PrettyFormat_generic_type()
        {
            var model = new ModelWithIdAndName { Id = 1, Name = "Name" };
            var modelStr = model.Dump();

            Assert.That(modelStr,
                        Is.EqualTo(
                            "{"
                            + Environment.NewLine
                            + "\tId: 1,"
                            + Environment.NewLine
                            + "\tName: Name"
                            + Environment.NewLine
                            + "}"
                        ));
        }

        [Test]
        public void Can_PrettyFormat_object()
        {
            object model = new ModelWithIdAndName { Id = 1, Name = "Name" };
            var modelStr = model.Dump();

            Console.WriteLine(modelStr);

            Assert.That(modelStr,
                        Is.EqualTo(
                            "{"
                            + Environment.NewLine
                               + "\tId: 1,"
                            + Environment.NewLine
                            + "\tName: Name"
                            + Environment.NewLine
                            + "}"
                        ));
        }

        internal class TestModel
        {
            public TestModel()
            {
                this.Int = 1;
                this.String = "One";
                this.DateTime = DateTime.UtcNow.Date;
                this.Guid = Guid.NewGuid();
                this.EmptyIntList = new List<int>();
                this.IntList = new List<int> { 1, 2, 3 };
                this.StringList = new List<string> { "one", "two", "three" };
                this.StringIntMap = new Dictionary<string, int>
                    {
                        {"a", 1},{"b", 2},{"c", 3},
                    };
            }

            public int Int { get; set; }
            public string String { get; set; }
            public DateTime DateTime { get; set; }
            public Guid Guid { get; set; }
            public List<int> EmptyIntList { get; set; }
            public List<int> IntList { get; set; }
            public List<string> StringList { get; set; }
            public Dictionary<string, int> StringIntMap { get; set; }
        }

        [Test]
        public void Can_DumpModel()
        {
            var model = new TestModel();
            model.PrintDump();
        }
    }
}