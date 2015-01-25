using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.ServiceClient.Web
{
    [TestFixture]
    public class UrlExtensionsTests
    {
        [Test]
        public void FormatVariable_DateTimeOffsetValue_ValueIsUrlEncoded()
        {
            var dateTimeOffset = DateTimeOffset.Now;
            var formattedVariable = RestRoute.FormatVariable(dateTimeOffset);
            var jsv = dateTimeOffset.ToJsv();
            Assert.AreEqual(Uri.EscapeDataString(jsv), formattedVariable);
        }

        [Test]
        public void FormatQueryParameterValue_DateTimeOffsetValue_ValueIsUrlEncoded()
        {
            var dateTimeOffset = DateTimeOffset.Now;
            var formattedVariable = RestRoute.FormatQueryParameterValue(dateTimeOffset);
            var jsv = dateTimeOffset.ToJsv();
            Assert.AreEqual(Uri.EscapeDataString(jsv), formattedVariable);
        }

        [Test]
        public void Can_get_operation_name()
        {
            Assert.That(typeof(Root).GetOperationName(), Is.EqualTo("Root"));
            Assert.That(typeof(Root.Nested).GetOperationName(), Is.EqualTo("Root.Nested"));
        }

        [Test]
        public void Can_use_nested_classes_as_Request_DTOs()
        {
            using (var appHost = new BasicAppHost(typeof(NestedService).Assembly){}.Init())
            {
                var root = (Root)appHost.ExecuteService(new Root { Id = 1 });
                Assert.That(root.Id, Is.EqualTo(1));

                var nested = (Root.Nested)appHost.ExecuteService(new Root.Nested { Id = 2 });
                Assert.That(nested.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_expand_generic_List()
        {
            var genericName = typeof(List<Poco>).ExpandTypeName();

            genericName.Print();

            Assert.That(genericName, Is.EqualTo("List<Poco>"));
        }

        [Test]
        public void Can_expand_generic_List_and_Dictionary()
        {
            //System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[Namespace.Poco, Assembly.Name, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]

            var genericName = typeof(Dictionary<string,List<Poco>>).ExpandTypeName();

            genericName.Print();

            Assert.That(genericName, Is.EqualTo("Dictionary<String,List<Poco>>"));
        }

        [Test]
        public void Can_parse_Single_Type()
        {
            var fullGenericTypeName = "Poco";

            var textNode = MetadataExtensions.ParseTypeIntoNodes(fullGenericTypeName);

            textNode.PrintDump();

            Assert.That(textNode.Text, Is.EqualTo("Poco"));
            Assert.That(textNode.Children.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_parse_generic_List_of_Poco()
        {
            var fullGenericTypeName = "List<Poco>";

            var textNode = MetadataExtensions.ParseTypeIntoNodes(fullGenericTypeName);

            textNode.PrintDump();

            Assert.That(textNode.Text, Is.EqualTo("List"));
            Assert.That(textNode.Children.Count, Is.EqualTo(1));
            Assert.That(textNode.Children[0].Text, Is.EqualTo("Poco"));
        }

        [Test]
        public void Can_parse_generic_List_Dictionary_of_String_and_Poco()
        {
            var fullGenericTypeName = "List<Dictionary<String,Poco>>";

            var textNode = MetadataExtensions.ParseTypeIntoNodes(fullGenericTypeName);

            textNode.PrintDump();

            Assert.That(textNode.Text, Is.EqualTo("List"));
            Assert.That(textNode.Children.Count, Is.EqualTo(1));

            Assert.That(textNode.Children[0].Text, Is.EqualTo("Dictionary"));
            Assert.That(textNode.Children[0].Children.Count, Is.EqualTo(2));
            Assert.That(textNode.Children[0].Children[0].Text, Is.EqualTo("String"));
            Assert.That(textNode.Children[0].Children[1].Text, Is.EqualTo("Poco"));
        }

        [Test]
        public void Can_parse_generic_List_Dictionary_of_Dictionary_of_String_and_Poco()
        {
            var fullGenericTypeName = "List<Dictionary<String,Dictionary<String,Poco>>>";

            var textNode = MetadataExtensions.ParseTypeIntoNodes(fullGenericTypeName);

            textNode.PrintDump();

            Assert.That(textNode.Text, Is.EqualTo("List"));
            Assert.That(textNode.Children.Count, Is.EqualTo(1));

            Assert.That(textNode.Children[0].Text, Is.EqualTo("Dictionary"));
            Assert.That(textNode.Children[0].Children.Count, Is.EqualTo(2));
            Assert.That(textNode.Children[0].Children[0].Text, Is.EqualTo("String"));

            Assert.That(textNode.Children[0].Children[1].Text, Is.EqualTo("Dictionary"));
            Assert.That(textNode.Children[0].Children[1].Children.Count, Is.EqualTo(2));
            Assert.That(textNode.Children[0].Children[1].Children[0].Text, Is.EqualTo("String"));
            Assert.That(textNode.Children[0].Children[1].Children[1].Text, Is.EqualTo("Poco"));
        }

    }

    public class Root
    {
        public int Id { get; set; }

        public class Nested
        {
            public int Id { get; set; }
        }
    }

    public class NestedService : Service
    {
        public object Any(Root request)
        {
            return request;
        }

        public object Any(Root.Nested request)
        {
            return request;
        }
    }
}
