using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class StringSpanTests
    {
        [Test]
        public void Can_read_lines_with_TryReadLine()
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
        public void Can_read_parts_with_TryReadPart()
        {
            var str = "A.BB.CCC.DD DD";
            var expected = new[] { "A", "BB", "CCC", "DD DD" };

            var i = 0;
            var buf = str.AsSpan();
            var pos = 0;
            while (buf.TryReadPart(".", out var part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));

            str = "A||BB||CCC||DD DD";

            i = 0;
            buf = str.AsSpan();
            pos = 0;
            while (buf.TryReadPart("||", out var part, ref pos))
            {
                Assert.That(part.ToString(), Is.EqualTo(expected[i++]));
            }

            Assert.That(pos, Is.EqualTo(buf.Length));
            Assert.That(i, Is.EqualTo(expected.Length));
        }

        [Test]
        public void Can_SplitOnFirst()
        {
            "a:b:c".AsSpan().SplitOnFirst(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a"));
            Assert.That(last.ToString(), Is.EqualTo("b:c"));

            "a::b::c".AsSpan().SplitOnFirst("::", out first, out last);
            Assert.That(first.EqualTo("a"));
            Assert.That(last.EqualTo("b::c"));
        }

        [Test]
        public void Can_SplitOnLast()
        {
            "a:b:c".AsSpan().SplitOnLast(':', out var first, out var last);
            Assert.That(first.ToString(), Is.EqualTo("a:b"));
            Assert.That(last.ToString(), Is.EqualTo("c"));

            "a::b::c".AsSpan().SplitOnLast("::", out first, out last);
            Assert.That(first.EqualTo("a::b"));
            Assert.That(last.EqualTo("c"));
        }

        [Test]
        public void Does_convert_to_UTF8_bytes()
        {
            var str = "this is a UTF8 test string";
            var seg = str.AsSpan();
            var ut8Test = seg.Slice(seg.IndexOf("UTF8"), "UTF8 test".Length);

            var segBytes = ut8Test.ToUtf8Bytes();
            Assert.That(segBytes, Is.EquivalentTo("UTF8 test".ToUtf8Bytes()));
        }

        [Test]
        public void Does_parse_into_preferred_signed_number_type()
        {
            Assert.That(int.MinValue.ToString().AsSpan().ParseSignedInteger() is int);
            Assert.That(int.MaxValue.ToString().AsSpan().ParseSignedInteger() is int);
            Assert.That((int.MinValue - (long)1).ToString().AsSpan().ParseSignedInteger() is long);
            Assert.That((int.MaxValue + (long)1).ToString().AsSpan().ParseSignedInteger() is long);
        }

    }
}