using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class InvalidParsingIssues
    {
        [Test]
        public void Parsing_string_into_string_array_splits_on_spaces()
        {
            var str = "string with a bunch of words";
            var result = str.FromJson<string[]>();
            
            Assert.That(result, Is.EquivalentTo(str.Split(' ')));
        }
    }
}