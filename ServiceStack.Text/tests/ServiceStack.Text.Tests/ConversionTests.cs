using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class ConversionTests
    {
        [Test]
        public void Converting_ObjectDictionary_ToStringDictionary_converts_collection_to_jsv()
        {
            var objDictionary = new Dictionary<string, object>
            {
                {"string", "foo,bar" },
                {"intArray", new[] {1, 2} },
                {"stringArray", new[] {"foo", "bar"} },
                {"stringEscapeChars", "a, 'b" },
                {"stringArrayEscapeChars", new[] { "a, b", "c 'd"} },
            };

            var strDictionary = objDictionary.ToStringDictionary();

            Assert.That(strDictionary["string"], Is.EqualTo("foo,bar"));
            Assert.That(strDictionary["intArray"], Is.EqualTo("[1,2]"));
            Assert.That(strDictionary["stringArray"], Is.EqualTo("[foo,bar]"));
            Assert.That(strDictionary["stringEscapeChars"], Is.EqualTo("a, 'b"));
            Assert.That(strDictionary["stringArrayEscapeChars"], Is.EqualTo("[\"a, b\",c 'd]"));
        }
    }
}