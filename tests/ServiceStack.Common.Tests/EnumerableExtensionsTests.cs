using System.Collections.Concurrent;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        readonly int[] IntValues = new[] { 1, 2, 3 };
        readonly int[] NoValues = new int[] { };
        readonly int[] DifferentValues = new[] { 5, 6, 7 };
        readonly int[] MoreIntValues = new[] { 1, 2, 3, 4 };
        readonly int[] LessIntValues = new[] { 1, 2 };
        readonly int[] UnorderedIntValues = new[] { 3, 2, 1 };

        readonly string[] StringValues = new[] { "A", "B", "C" };
        readonly string[] NoStringValues = new string[] { };

        [Test]
        public void Can_Join()
        {
            Assert.That(IntValues.Join(), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void EquivalentTo_self()
        {
            Assert.That(IntValues.EquivalentTo(IntValues), Is.True);
        }

        [Test]
        public void EquivalentTo_List()
        {
            Assert.That(IntValues.EquivalentTo(IntValues.ToList()), Is.True);
        }

        [Test]
        public void Not_EquivalentTo_NoValues()
        {
            Assert.That(IntValues.EquivalentTo(NoValues), Is.False);
        }

        [Test]
        public void Not_EquivalentTo_DifferentValues()
        {
            Assert.That(IntValues.EquivalentTo(DifferentValues), Is.False);
        }

        [Test]
        public void Not_EquivalentTo_LessIntValues()
        {
            Assert.That(IntValues.EquivalentTo(LessIntValues), Is.False);
        }

        [Test]
        public void Not_EquivalentTo_MoreIntValues()
        {
            Assert.That(IntValues.EquivalentTo(MoreIntValues), Is.False);
        }

        [Test]
        public void Not_EquivalentTo_UnorderedIntValues()
        {
            Assert.That(IntValues.EquivalentTo(UnorderedIntValues), Is.False);
        }

        [Test]
        public void Not_EquivalentTo_null()
        {
            Assert.That(IntValues.EquivalentTo(null), Is.False);
        }

        [Test]
        public void EquivalentTo_StringValues()
        {
            Assert.That(StringValues.EquivalentTo(NoStringValues), Is.False);
            Assert.That(NoStringValues.EquivalentTo(StringValues), Is.False);
            Assert.That(NoStringValues.EquivalentTo(NoStringValues), Is.True);
            Assert.That(StringValues.EquivalentTo(StringValues), Is.True);

            Assert.That(StringValues.EquivalentTo(new string[] { null }), Is.False);
            Assert.That(new string[] { null }.EquivalentTo(StringValues), Is.False);
        }

        [Test]
        public void EquivalentTo_Dictionary_Ordered()
        {
            var a = new Dictionary<string, int>
            {
                {"A",1},
                {"B",2},
                {"C",3},
            };
            var b = new ConcurrentDictionary<string, int>();
            b["A"] = 1;
            b["B"] = 2;
            b["C"] = 3;

            Assert.That(a.EquivalentTo(b));
        }

        [Test]
        public void EquivalentTo_Dictionary_Unordered()
        {
            var a = new Dictionary<string, int>
            {
                {"A",1},
                {"B",2},
                {"C",3},
            };
            var b = new Dictionary<string, int>
            {
                {"C",3},
                {"A",1},
                {"B",2},
            };

            Assert.That(a.EquivalentTo(b));
        }
    }
}