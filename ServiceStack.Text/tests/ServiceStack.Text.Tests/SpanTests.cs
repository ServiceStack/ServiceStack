using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text.Pools;

namespace ServiceStack.Text.Tests
{
    public class SpanTests
    {
        [Test]
        public void Can_use_Span()
        {
            ReadOnlySpan<char> a = "foo bar".AsSpan();
            
            var foo = a.Slice(0,3).ToArray();

            Assert.That(foo, Is.EqualTo("foo".ToCharArray()));
        }

        [Test]
        public void Can_not_detect_null_empty_string_spans()
        {
            var n = ((string)null).AsSpan();
            var e = "".AsSpan();
            
            Assert.That(n.SequenceEqual(e)); //null + "" spans are considered equal
        }

        [Test]
        public void Can_read_lines_with_TryReadLine_using_Span()
        {
            var str = "A\nB\r\nC\rD\r\n";
            var expected = new[] {"A", "B", "C", "D"};

            var i = 0;
            var buf = str.AsSpan();
            var pos = 0;
            while (buf.TryReadLine(out var line, ref pos))
            {
                Assert.That(line.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));
        }

        [Test]
        public void Can_read_parts_with_TryReadPart_using_Span()
        {
            var str = "A.BB.CCC.DD DD";
            var expected = new[] { "A", "BB", "CCC", "DD DD" };

            var i = 0;
            var buf = str.AsSpan();
            var pos = 0;
            while (buf.TryReadPart(".", out ReadOnlySpan<char> part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));

            str = "A||BB||CCC||DD DD";

            i = 0;
            buf = str.AsSpan();
            pos = 0;
            while (buf.TryReadPart("||", out ReadOnlySpan<char> part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));
        }

        [Test]
        public void Can_SplitOnFirst_using_Span()
        {
            "a:b:c".AsSpan().SplitOnFirst(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a"));
            Assert.That(last.ToString(), Is.EqualTo("b:c"));

            "a::b::c".AsSpan().SplitOnFirst("::", out first, out last);
            Assert.That(first.ToString(), Is.EqualTo("a"));
            Assert.That(last.ToString(), Is.EqualTo("b::c"));
        }

        [Test]
        public void Can_SplitOnLast_using_Span()
        {
            "a:b:c".AsSpan().SplitOnLast(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a:b"));
            Assert.That(last.ToString(), Is.EqualTo("c"));

            "a::b::c".AsSpan().SplitOnLast("::", out first, out last);
            Assert.That(first.ToString(), Is.EqualTo("a::b"));
            Assert.That(last.ToString(), Is.EqualTo("c"));
        }

        [Test]
        public void Can_ToUtf8_and_FromUtf8_using_Span()
        {
            foreach (var test in Utf8Case.Source)
            {
                ReadOnlyMemory<byte> bytes = test.expectedString.AsSpan().ToUtf8();
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
        public void Can_ToUtf8_and_FromUtf8_in_place_using_Span()
        {
            foreach (var test in Utf8Case.Source)
            {
                var chars = test.expectedString.AsSpan();
                Memory<byte> buffer = BufferPool.GetBuffer(MemoryProvider.Instance.GetUtf8ByteCount(chars));
                var bytesWritten = MemoryProvider.Instance.ToUtf8(chars, buffer.Span);
                var bytes = buffer.Slice(0, bytesWritten);
                
                Assert.That(bytes.Length, Is.EqualTo(test.count));
                Assert.That(bytes.ToArray(), Is.EquivalentTo(test.expectedBytes));

                Memory<char> charBuff = CharPool.GetBuffer(MemoryProvider.Instance.GetUtf8CharCount(bytes.Span));
                var charsWritten = MemoryProvider.Instance.FromUtf8(bytes.Span, charBuff.Span);
                chars = charBuff.Slice(0, charsWritten).Span;

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

        [Test]
        public void Can_deserialize_JSON_with_UTF8_BOM()
        {
            var from = new Person { Id = 1, Name = "Foo" };
            var json = from.ToJson();
            var jsonBytes = json.ToUtf8Bytes();

            var bytes = new List<byte>(new byte[] { 0xEF, 0xBB, 0xBF });
            bytes.AddRange(jsonBytes);

            var mergedBytes = bytes.ToArray();

            var jsonWithBOM = mergedBytes.FromUtf8Bytes();

            var fromJsonWithBOM = jsonWithBOM.FromJson<Person>();
            
            Assert.That(fromJsonWithBOM, Is.EqualTo(from));
        }
    }

    public class Utf8Case
    {
        //https://github.com/dotnet/corefx/blob/master/src/System.Text.Encoding/tests/UTF8Encoding/UTF8EncodingDecode.cs
        public static readonly Utf8Case[] Source = {
            new Utf8Case(new byte[] {70, 111, 111, 66, 65, 208, 128, 82}, 0, 8, "FooBA\u0400R"),
            new Utf8Case(new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, 0, 9, "\u00C0nima\u0300l"),
            new Utf8Case(new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, 0, 12, "Test\uD803\uDD75Test"),
            new Utf8Case(new byte[] { 0, 84, 101, 10, 115, 116, 0, 9, 0, 84, 15, 101, 115, 116, 0 }, 0, 15, "\0Te\nst\0\t\0T\u000Fest\0"),
            new Utf8Case(new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, 0, 12, "\uD803\uDD75\uD803\uDD75\uD803\uDD75"),
            new Utf8Case(new byte[] { 196, 176 }, 0, 2, "\u0130"),
            new Utf8Case(new byte[] { 0x61, 0xCC, 0x8A }, 0, 3, "\u0061\u030A"),
            new Utf8Case(new byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 }, 0, 16, "\u00A4\u00D0aR|{AnGe\u00A3\u00A4"),
            new Utf8Case(new byte[] { 0x00, 0x7F }, 0, 2, "\u0000\u007F"),
            new Utf8Case(new byte[] { 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F }, 0, 14, "\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F"),
            new Utf8Case(new byte[] { 0xC2, 0x80, 0xDF, 0xBF }, 0, 4, "\u0080\u07FF"),
            
            // Long ASCII strings
            new Utf8Case(new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, 0, 10, "TestString"),
            new Utf8Case(new byte[] { 84, 101, 115, 116, 84, 101, 115, 116 }, 0, 8, "TestTest"),
            
            // Control codes
            new Utf8Case(new byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, "\u001F\u0010\u0000\u0009"),
            new Utf8Case(new byte[] { 0x1F, 0x00, 0x10, 0x09 }, 0, 4, "\u001F\u0000\u0010\u0009"),
            new Utf8Case(new byte[] { 0x00, 0x1F, 0x10, 0x09 }, 0, 4, "\u0000\u001F\u0010\u0009"),
            
            // BOM
            new Utf8Case(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 0, 4, "\uFEFF\u0041"),
            
            // U+FDD0 - U+FDEF
            new Utf8Case(new byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 0, 6, "\uFDD0\uFDEF"),
            
            // 2 byte encoding
            new Utf8Case(new byte[] { 0xC3, 0xA1 }, 0, 2, "\u00E1"),
            new Utf8Case(new byte[] { 0xC3, 0x85 }, 0, 2, "\u00C5"),
            
            // 3 byte encoding
            new Utf8Case(new byte[] { 0xE8, 0x80, 0x80 }, 0, 3, "\u8000"),
            new Utf8Case(new byte[] { 0xE2, 0x84, 0xAB }, 0, 3, "\u212B"),
            
            // Surrogate pairs
            new Utf8Case(new byte[] { 240, 144, 128, 128 }, 0, 4, "\uD800\uDC00"),
            new Utf8Case(new byte[] { 97, 240, 144, 128, 128, 98 }, 0, 6, "a\uD800\uDC00b"), 
            
            // High BMP non-chars
            new Utf8Case(new byte[] { 239, 191, 189 }, 0, 3, "\uFFFD"),
            
            // Empty strings
            new Utf8Case(new byte[0], 0, 0, string.Empty),
        };

        public byte[] expectedBytes;
        public int index;
        public int count;
        public string expectedString;
            
        public Utf8Case(byte[] expectedBytes, int index, int count, string expectedString)
        {
            this.expectedBytes = expectedBytes;
            this.index = index;
            this.count = count;
            this.expectedString = expectedString;
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }

        protected bool Equals(Person other)
        {
            return Id == other.Id && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Person) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
   
}