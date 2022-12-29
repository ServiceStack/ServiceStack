using NUnit.Framework;

namespace ServiceStack.Text.Tests.UseCases
{
    public class MyType
    {
        public long LongProp { get; set; }
        public string StringProp { get; set; }

        public long LongField;
        public string StringField;
    }

    public class TypedAccessors_API_Examples
    {
        [Test]
        public void Can_access_IntProp()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance();
            var pi = runtimeType.GetProperty("LongProp");
            var setter = pi.CreateSetter();
            var getter = pi.CreateGetter();

            setter(instance, 1L);
            var value = getter(instance);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_access_IntProp_Typed()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance<MyType>();
            var pi = runtimeType.GetProperty("LongProp");
            var setter = pi.CreateSetter<MyType>();
            var getter = pi.CreateGetter<MyType>();

            setter(instance, 1L);
            var value = getter(instance);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_access_StringProp()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance();
            var pi = runtimeType.GetProperty("StringProp");
            var setter = pi.CreateSetter();
            var getter = pi.CreateGetter();

            setter(instance, "foo");
            var value = getter(instance);

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_access_StringProp_Typed()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance<MyType>();
            var pi = runtimeType.GetProperty("StringProp");
            var setter = pi.CreateSetter<MyType>();
            var getter = pi.CreateGetter<MyType>();

            setter(instance, "foo");
            var value = getter(instance);

            Assert.That(value, Is.EqualTo("foo"));
        }


        [Test]
        public void Can_access_IntField()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance();
            var pi = runtimeType.GetField("LongField");
            var setter = pi.CreateSetter();
            var getter = pi.CreateGetter();

            setter(instance, 1L);
            var value = getter(instance);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_access_IntField_Typed()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance<MyType>();
            var pi = runtimeType.GetField("LongField");
            var setter = pi.CreateSetter<MyType>();
            var getter = pi.CreateGetter<MyType>();

            setter(instance, 1L);
            var value = getter(instance);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_access_StringField()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance();
            var pi = runtimeType.GetField("StringField");
            var setter = pi.CreateSetter();
            var getter = pi.CreateGetter();

            setter(instance, "foo");
            var value = getter(instance);

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_access_StringField_Typed()
        {
            var runtimeType = typeof(MyType);

            var instance = runtimeType.CreateInstance<MyType>();
            var pi = runtimeType.GetField("StringField");
            var setter = pi.CreateSetter<MyType>();
            var getter = pi.CreateGetter<MyType>();

            setter(instance, "foo");
            var value = getter(instance);

            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_use_TypedProperties_accessor()
        {
            var runtimeType = typeof(MyType);
            var typeProps = TypeProperties.Get(runtimeType); //Equivalent to:
            //  typeProps = TypeProperties<MyType>.Instance;

            var instance = runtimeType.CreateInstance();

            var propAccessor = typeProps.GetAccessor("LongProp");
            propAccessor.PublicSetter(instance, 1L);
            Assert.That(propAccessor.PublicGetter(instance), Is.EqualTo(1));

            typeProps.GetPublicSetter("StringProp")(instance, "foo");
            var value = typeProps.GetPublicGetter("StringProp")(instance);
            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_use_TypedFields_accessor()
        {
            var runtimeType = typeof(MyType);
            var typeProps = TypeFields.Get(runtimeType); //Equivalent to:
            //  typeProps = TypeFields<MyType>.Instance;

            var instance = runtimeType.CreateInstance();

            var propAccessor = typeProps.GetAccessor("LongField");
            propAccessor.PublicSetter(instance, 1L);
            Assert.That(propAccessor.PublicGetter(instance), Is.EqualTo(1));

            typeProps.GetPublicSetter("StringField")(instance, "foo");
            var value = typeProps.GetPublicGetter("StringField")(instance);
            Assert.That(value, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_use_TypedFields_ValueType_Accessor()
        {
            var typeFields = TypeFields.Get(typeof((string s, int i)));

            var oTuple = (object)("foo", 1);

            typeFields.GetPublicSetterRef("Item1")(ref oTuple, "bar");
            typeFields.GetPublicSetterRef("Item2")(ref oTuple, 2);

            var tuple = ((string s, int i))oTuple;
            Assert.That(tuple.s, Is.EqualTo("bar"));
            Assert.That(tuple.i, Is.EqualTo(2));

            var item1Accessor = typeFields.GetAccessor("Item1");
            var item2Accessor = typeFields.GetAccessor("Item2");
            item1Accessor.PublicSetterRef(ref oTuple, "qux");
            item2Accessor.PublicSetterRef(ref oTuple, 3);

            Assert.That(item1Accessor.PublicGetter(oTuple), Is.EqualTo("qux"));
            Assert.That(item2Accessor.PublicGetter(oTuple), Is.EqualTo(3));
        }
    }
}