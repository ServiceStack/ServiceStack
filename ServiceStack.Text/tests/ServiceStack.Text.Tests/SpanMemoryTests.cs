using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text.Pools;

namespace ServiceStack.Text.Tests
{
    public class SpanMemoryTests
    {
        [Test]
        public void Can_use_Memory()
        {
            ReadOnlyMemory<char> a = "foo bar".AsMemory();

            var foo = a.Slice(0, 3).ToArray();

            Assert.That(foo, Is.EqualTo("foo".ToCharArray()));
        }

        [Test]
        public void Can_not_detect_null_empty_string_memory()
        {
            var n = ((string) null).AsMemory();
            var e = "".AsMemory();

            Assert.That(!n.Equals(e)); //null + "" memory are not equal

            Assert.That(n.Equals(((string) null).AsMemory()));
            Assert.That(e.Equals("".AsMemory()));

            Assert.That(n.Equals(default(ReadOnlyMemory<char>)));
            Assert.That(!e.Equals(default(ReadOnlyMemory<char>)));

            Assert.That(n.IsEmpty);
            Assert.That(e.IsEmpty);
        }

        [Test]
        public void Can_read_lines_with_TryReadLine_using_Memory()
        {
            var str = "A\nB\r\nC\rD\r\n";
            var expected = new[] {"A", "B", "C", "D"};

            var i = 0;
            var buf = str.AsMemory();
            var pos = 0;
            while (buf.TryReadLine(out ReadOnlyMemory<char> line, ref pos))
            {
                Assert.That(line.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));
        }

        [Test]
        public void Can_read_parts_with_TryReadPart_using_Memory()
        {
            var str = "A.BB.CCC.DD DD";
            var expected = new[] {"A", "BB", "CCC", "DD DD"};

            var i = 0;
            var buf = str.AsMemory();
            var pos = 0;
            while (buf.TryReadPart(".".AsMemory(), out ReadOnlyMemory<char> part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));

            str = "A||BB||CCC||DD DD";

            i = 0;
            buf = str.AsMemory();
            pos = 0;
            while (buf.TryReadPart("||".AsMemory(), out ReadOnlyMemory<char> part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));
        }

        [Test]
        public void Can_SplitOnFirst_using_Memory()
        {
            "a:b:c".AsMemory().SplitOnFirst(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a"));
            Assert.That(last.ToString(), Is.EqualTo("b:c"));

            "a::b::c".AsMemory().SplitOnFirst("::".AsMemory(), out first, out last);
            Assert.That(first.ToString(), Is.EqualTo("a"));
            Assert.That(last.ToString(), Is.EqualTo("b::c"));
        }

        [Test]
        public void Can_SplitOnLast_using_Span()
        {
            "a:b:c".AsMemory().SplitOnLast(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a:b"));
            Assert.That(last.ToString(), Is.EqualTo("c"));

            "a::b::c".AsMemory().SplitOnLast("::".AsMemory(), out first, out last);
            Assert.That(first.ToString(), Is.EqualTo("a::b"));
            Assert.That(last.ToString(), Is.EqualTo("c"));
        }

        [Test]
        public void Can_ToUtf8_and_FromUtf8_using_Memory()
        {
            foreach (var test in Utf8Case.Source)
            {
                ReadOnlyMemory<byte> bytes = test.expectedString.AsMemory().ToUtf8();
                Assert.That(bytes.Length, Is.EqualTo(test.count));
                Assert.That(bytes.ToArray(), Is.EquivalentTo(test.expectedBytes));

                ReadOnlyMemory<char> chars = bytes.FromUtf8();
                Assert.That(chars.Length, Is.EqualTo(test.expectedString.Length)
                    .Or.EqualTo(test.expectedString.WithoutBom().Length));
                Assert.That(chars.ToString(), Is.EqualTo(test.expectedString)
                    .Or.EqualTo(test.expectedString.WithoutBom()));
            }
        }

        [Test]
        public async Task Can_deserialize_from_MemoryStream_using_Memory()
        {
            var from = new Person { Id = 1, Name = "FooBA\u0400R" };
            var json = from.ToJson();

            var ms = MemoryStreamFactory.GetStream(json.ToUtf8Bytes());

            var to = (Person)await MemoryProvider.Instance.DeserializeAsync(ms, typeof(Person), JsonSerializer.DeserializeFromSpan);
            
            Assert.That(to, Is.EqualTo(from));
        }
        
    }
}