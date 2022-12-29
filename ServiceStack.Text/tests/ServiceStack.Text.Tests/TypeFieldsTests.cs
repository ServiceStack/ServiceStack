using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    class RefTypeFields
    {
        public string S;
        public int I;
        public long L = 0;
        public double DL = 0;
    }

    struct ValueTypeFields
    {
        public string S;
        public int I;
    }

    struct ValueTypeGenericFields<T>
    {
        public string S;
        public int I;
        public T G;
    }

    public class TypeFieldsTests
    {
        static (string s, int i, long l, double d) CreateValueTuple() =>
            ("foo", 1, 2, 3.3);

        [Test]
        public void Can_cache_ValueTuple_field_accessors()
        {
            var typeFields = TypeFields.Get(typeof((string s, int i, long l, double d)));

            var oTuple = (object)CreateValueTuple();

            typeFields.GetPublicSetterRef("Item1")(ref oTuple, "bar");
            typeFields.GetPublicSetterRef("Item2")(ref oTuple, 10);
            typeFields.GetPublicSetterRef("Item3")(ref oTuple, 20L);
            typeFields.GetPublicSetterRef("Item4")(ref oTuple, 4.4d);

            Assert.That(typeFields.GetPublicGetter("Item1")(oTuple), Is.EqualTo("bar"));
            Assert.That(typeFields.GetPublicGetter("Item2")(oTuple), Is.EqualTo(10));
            Assert.That(typeFields.GetPublicGetter("Item3")(oTuple), Is.EqualTo(20));
            Assert.That(typeFields.GetPublicGetter("Item4")(oTuple), Is.EqualTo(4.4));

            var tuple = ((string s, int i, long l, double d))oTuple;

            Assert.That(tuple.s, Is.EqualTo("bar"));
            Assert.That(tuple.i, Is.EqualTo(10));
            Assert.That(tuple.l, Is.EqualTo(20));
            Assert.That(tuple.d, Is.EqualTo(4.4));
        }

        [Test]
        public void Can_use_getter_and_setter_on_RefTypeFields()
        {
            var typeFields = TypeFields.Get(typeof(RefTypeFields));

            var o = (object)new RefTypeFields { S = "foo", I = 1 };

            typeFields.GetPublicSetter("S")(o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetter("I")(o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }

        [Test]
        public void Can_use_getter_and_setter_on_ValueTypeFields()
        {
            var typeFields = TypeFields.Get(typeof(ValueTypeFields));

            var o = (object)new ValueTypeFields { S = "foo", I = 1 };

            typeFields.GetPublicSetter("S")(o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetter("I")(o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }

        [Test]
        public void Can_use_getter_and_setter_on_ValueTypeFields_ref()
        {
            var typeFields = TypeFields.Get(typeof(ValueTypeFields));

            var o = (object)new ValueTypeFields { S = "foo", I = 1 };

            typeFields.GetPublicSetterRef("S")(ref o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetterRef("I")(ref o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }

        [Test]
        public void Can_use_getter_and_setter_on_ValueTypeGenericFields()
        {
            var typeFields = TypeFields.Get(typeof(ValueTypeGenericFields<string>));

            var o = (object)new ValueTypeGenericFields<string> { S = "foo", I = 1, G = "foo" };

            typeFields.GetPublicSetterRef("S")(ref o, "bar");
            Assert.That(typeFields.GetPublicGetter("S")(o), Is.EqualTo("bar"));

            typeFields.GetPublicSetterRef("I")(ref o, 2);
            Assert.That(typeFields.GetPublicGetter("I")(o), Is.EqualTo(2));
        }
    }
}