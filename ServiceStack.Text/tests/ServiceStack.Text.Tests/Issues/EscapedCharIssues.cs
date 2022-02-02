using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class EscapedCharIssues
    {
        public class MyChar
        {
            public char Char { get; set; }
        }


        [Test]
        public void Does_unescaped_unicode_char()
        {
            var dto = new MyChar();

            var json = dto.ToJson();

            Assert.That(json, Is.EqualTo("{\"Char\":\"\\u0000\"}"));

            var fromDto = json.FromJson<MyChar>();

            Assert.That(fromDto.Char, Is.EqualTo(dto.Char));
        }
    }
}