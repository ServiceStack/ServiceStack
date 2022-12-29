using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    class RefTypeProps
    {
        public string S { get; set; }
        public int I { get; set; }
        public long L { get; set; }
        public double D { get; set; }
    }

    struct ValueTypeProps
    {
        public string S { get; set; }
        public int I { get; set; }
    }

    struct ValueTypeGenericProps<T>
    {
        public string S { get; set; }
        public int I { get; set; }
        public T G { get; set; }
    }

    public class TypePropertiesTests
    {
        static RefTypeProps CreateTypedTuple() =>
            new RefTypeProps { S = "foo", I = 1, L = 2, D = 3.3 };

        [Test]
        public void Can_cache_ValueTuple_field_accessors()
        {
            var typeProperties = TypeProperties.Get(typeof(RefTypeProps));

            var oTuple = (object)CreateTypedTuple();

            typeProperties.GetPublicSetter("S")(oTuple, "bar");
            typeProperties.GetPublicSetter("I")(oTuple, 10);
            typeProperties.GetPublicSetter("L")(oTuple, 20L);
            typeProperties.GetPublicSetter("D")(oTuple, 4.4d);

            Assert.That(typeProperties.GetPublicGetter("S")(oTuple), Is.EqualTo("bar"));
            Assert.That(typeProperties.GetPublicGetter("I")(oTuple), Is.EqualTo(10));
            Assert.That(typeProperties.GetPublicGetter("L")(oTuple), Is.EqualTo(20));
            Assert.That(typeProperties.GetPublicGetter("D")(oTuple), Is.EqualTo(4.4));

            var tuple = (RefTypeProps)oTuple;

            Assert.That(tuple.S, Is.EqualTo("bar"));
            Assert.That(tuple.I, Is.EqualTo(10));
            Assert.That(tuple.L, Is.EqualTo(20));
            Assert.That(tuple.D, Is.EqualTo(4.4));
        }

        [Test]
        public void Can_use_getter_and_setter_on_RefTypeProps()
        {
            var typeFields = TypeProperties.Get(typeof(RefTypeProps));

            var o = (object)new RefTypeProps { S = "foo", I = 1 };

            typeFields.GetPublicSetter("S")(o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetter("I")(o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }

        [Test]
        public void Can_use_getter_and_setter_on_ValueTypeProps()
        {
            var typeFields = TypeProperties.Get(typeof(ValueTypeProps));

            var o = (object)new ValueTypeProps { S = "foo", I = 1 };

            typeFields.GetPublicSetter("S")(o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetter("I")(o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }

        [Test]
        public void Can_use_getter_and_setter_on_ValueTypeGenericProps()
        {
            var typeFields = TypeProperties.Get(typeof(ValueTypeGenericProps<string>));

            var o = (object)new ValueTypeGenericProps<string> { S = "foo", I = 1, G = "foo" };

            typeFields.GetPublicSetter("S")(o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetter("I")(o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }
    }
}