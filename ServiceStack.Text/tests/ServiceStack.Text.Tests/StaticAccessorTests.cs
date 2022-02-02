using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class AccessorBase
    {
        public string Base { get; set; }
        public string BaseField;
    }

    public class Accessor
    {
        public string Declared { get; set; }
    }

    public class SubAccessor : AccessorBase
    {
        public string Sub { get; set; }
        public string SubField;
    }


    [TestFixture]
    public class StaticAccessorTests
    {
        [Test]
        public void Can_get_accessor_in_declared_and_base_class()
        {
            var baseProperty = typeof(AccessorBase).GetProperty("Base");
            var declaredProperty = typeof(Accessor).GetProperty("Declared");

            var baseSetter = baseProperty.CreateGetter<AccessorBase>();
            Assert.That(baseSetter, Is.Not.Null);

            var declaredSetter = declaredProperty.CreateSetter<Accessor>();
            Assert.That(declaredSetter, Is.Not.Null);
        }

        [Test]
        public void Can_get_property_accessor_from_sub_and_super_types()
        {
            var sub = new SubAccessor();
            var subGet = typeof(SubAccessor).GetProperty("Sub").CreateGetter<SubAccessor>();
            var subSet = typeof(SubAccessor).GetProperty("Sub").CreateSetter<SubAccessor>();

            subSet(sub, "sub");
            Assert.That(subGet(sub), Is.EqualTo("sub"));

            var sup = new AccessorBase();
            var supGet = typeof(AccessorBase).GetProperty("Base").CreateGetter<AccessorBase>();
            var supSet = typeof(AccessorBase).GetProperty("Base").CreateSetter<AccessorBase>();

            supSet(sup, "base");
            Assert.That(supGet(sup), Is.EqualTo("base"));
            supSet(sub, "base");
            Assert.That(supGet(sub), Is.EqualTo("base"));
        }

        [Test]
        public void Can_get_field_accessor_from_sub_and_super_types()
        {
            var sub = new SubAccessor();
            var subGet = typeof(SubAccessor).GetField("SubField").CreateGetter<SubAccessor>();
            var subSet = typeof(SubAccessor).GetField("SubField").CreateSetter<SubAccessor>();

            subSet(sub, "sub");
            Assert.That(subGet(sub), Is.EqualTo("sub"));

            var sup = new AccessorBase();
            var supGet = typeof(AccessorBase).GetField("BaseField").CreateGetter<AccessorBase>();
            var supSet = typeof(AccessorBase).GetField("BaseField").CreateSetter<AccessorBase>();

            supSet(sup, "base");
            Assert.That(supGet(sup), Is.EqualTo("base"));
            supSet(sub, "base");
            Assert.That(supGet(sub), Is.EqualTo("base"));
        }
    }


}