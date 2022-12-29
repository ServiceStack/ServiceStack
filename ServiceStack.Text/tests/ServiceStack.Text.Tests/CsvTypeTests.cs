using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class CsvTypeTests
    {
        private int id;
        static string[] Names = new[] { "Foo", "Bar" };

        object Create(string name)
        {
            return new { id = ++id, name = name };
        }

        [SetUp]
        public void SetUp()
        {
            id = 0;
        }

        [Test]
        public void Can_serialize_Dynamic_List()
        {
            List<dynamic> rows = Names.Map(Create);
            var csv = rows.ToCsv();
            Assert.That(csv, Is.EqualTo("id,name\r\n1,Foo\r\n2,Bar\r\n"));
        }

        [Test]
        public void Can_serialize_Dynamic_Objects()
        {
            List<object> rows = Names.Map(Create);
            var csv = rows.ToCsv();
            Assert.That(csv, Is.EqualTo("id,name\r\n1,Foo\r\n2,Bar\r\n"));
        }
    }
}