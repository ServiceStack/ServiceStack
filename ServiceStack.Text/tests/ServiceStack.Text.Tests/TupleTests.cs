using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class TupleTests
    {
        [Test]
        public void can_get_single_tuple()
        {
            var tuple = Tuple.Create(true);
            var tstr = JsonSerializer.SerializeToString(tuple);
            Assert.True(tstr.Contains(":true"));

            var deser = JsonSerializer.DeserializeFromString<Tuple<bool>>(tstr);
            Assert.True(deser.Item1); //fails
        }

        [Test]
        public void can_get_double_tuple()
        {
            var tuple = Tuple.Create<bool, bool>(true, true);
            var tstr = JsonSerializer.SerializeToString(tuple);
            Assert.True(tstr.Contains(":true"));

            var deser = JsonSerializer.DeserializeFromString<Tuple<bool, bool>>(tstr);
            Assert.True(deser.Item1); //fails
            Assert.True(deser.Item2);
        }

        private class OtherType
        {
            public bool Item1 { get; set; }
        }

        [Test]
        public void can_get_othertype()
        {
            var item = new OtherType() { Item1 = true };
            var tstr = JsonSerializer.SerializeToString(item);
            Assert.True(tstr.Contains(":true"));

            var deser = JsonSerializer.DeserializeFromString<OtherType>(tstr);
            Assert.True(deser.Item1); //works
        }

        private class OtherType<T>
        {
            public T Item1 { get; set; }
        }

        [Test]
        public void can_get_othertype_typed()
        {
            var item = new OtherType<bool>() { Item1 = true };
            var tstr = JsonSerializer.SerializeToString(item);
            Assert.True(tstr.Contains(":true"));

            var deser = JsonSerializer.DeserializeFromString<OtherType<bool>>(tstr);
            Assert.True(deser.Item1); //works
        }
    }
}