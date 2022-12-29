using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Common.Tests.Reflection
{
    [TestFixture]
    public class PropertyAccessorTests
    {
        [Test]
        public void Can_access_ModelWithIdAndName()
        {
            var accessor = TypeProperties<ModelWithIdAndName>.Instance;

            var obj = new ModelWithIdAndName { Id = 1, Name = "A" };

            Assert.That(accessor.GetPublicGetter("Id")(obj), Is.EqualTo(1));
            Assert.That(accessor.GetPublicGetter("Name")(obj), Is.EqualTo("A"));

            accessor.GetPublicSetter("Id")(obj, 2);
            accessor.GetPublicSetter("Name")(obj, "B");

            Assert.That(obj.Id, Is.EqualTo(2));
            Assert.That(obj.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Can_access_ModelWithFieldsOfDifferentTypes()
        {
            var idAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("Id");
            var nameAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("Name");
            var longIdAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("LongId");
            var guidAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("Guid");
            var boolAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("Bool");
            var doubleAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("Double");
            var dateTimeAccessor = TypeProperties<ModelWithFieldsOfDifferentTypes>.GetAccessor("DateTime");

            var original = ModelWithFieldsOfDifferentTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.PublicGetter(original), Is.EqualTo(original.Id));
            Assert.That(nameAccessor.PublicGetter(original), Is.EqualTo(original.Name));
            Assert.That(longIdAccessor.PublicGetter(original), Is.EqualTo(original.LongId));
            Assert.That(guidAccessor.PublicGetter(original), Is.EqualTo(original.Guid));
            Assert.That(boolAccessor.PublicGetter(original), Is.EqualTo(original.Bool));
            Assert.That(doubleAccessor.PublicGetter(original), Is.EqualTo(original.Double).Within(0.1));
            Assert.That(dateTimeAccessor.PublicGetter(original), Is.EqualTo(original.DateTime));

            var to = ModelWithFieldsOfDifferentTypesFactory.Instance.CreateInstance(2);

            idAccessor.PublicSetter(original, to.Id);
            nameAccessor.PublicSetter(original, to.Name);
            longIdAccessor.PublicSetter(original, to.LongId);
            guidAccessor.PublicSetter(original, to.Guid);
            boolAccessor.PublicSetter(original, to.Bool);
            doubleAccessor.PublicSetter(original, to.Double);
            dateTimeAccessor.PublicSetter(original, to.DateTime);

            ModelWithFieldsOfDifferentTypesFactory.Instance.AssertIsEqual(original, to);
        }

        [Test]
        public void Can_access_ModelWithComplexTypes()
        {
            var idAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("Id");
            var stringListAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("StringList");
            var intListAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("IntList");
            var stringMapAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("StringMap");
            var intMapAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("IntMap");
            var childAccessor = TypeProperties<ModelWithComplexTypes>.GetAccessor("Child");

            var original = ModelWithComplexTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.PublicGetter(original), Is.EqualTo(original.Id));
            Assert.That(stringListAccessor.PublicGetter(original), Is.EqualTo(original.StringList));
            Assert.That(intListAccessor.PublicGetter(original), Is.EqualTo(original.IntList));
            Assert.That(stringMapAccessor.PublicGetter(original), Is.EqualTo(original.StringMap));
            Assert.That(intMapAccessor.PublicGetter(original), Is.EqualTo(original.IntMap));
            Assert.That(childAccessor.PublicGetter(original), Is.EqualTo(original.Child));

            var to = ModelWithComplexTypesFactory.Instance.CreateInstance(2);

            idAccessor.PublicSetter(original, to.Id);
            stringListAccessor.PublicSetter(original, to.StringList);
            intListAccessor.PublicSetter(original, to.IntList);
            stringMapAccessor.PublicSetter(original, to.StringMap);
            intMapAccessor.PublicSetter(original, to.IntMap);
            childAccessor.PublicSetter(original, to.Child);

            ModelWithComplexTypesFactory.Instance.AssertIsEqual(original, to);
        }

        [Test]
        public void Can_access_ModelWithFieldsOfDifferentAndNullableTypes()
        {
            var idAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("Id");
            var idNAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NId");
            var longIdAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NLongId");
            var guidAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NGuid");
            var boolAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NBool");
            var dateTimeAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NDateTime");
            var floatAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NFloat");
            var doubleAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NDouble");
            var decimalAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NDecimal");
            var timespanAccessor = TypeProperties<ModelWithFieldsOfNullableTypes>.GetAccessor("NTimeSpan");

            var original = ModelWithFieldsOfNullableTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.PublicGetter(original), Is.EqualTo(original.Id));
            Assert.That(idNAccessor.PublicGetter(original), Is.EqualTo(original.NId));
            Assert.That(longIdAccessor.PublicGetter(original), Is.EqualTo(original.NLongId));
            Assert.That(guidAccessor.PublicGetter(original), Is.EqualTo(original.NGuid));
            Assert.That(boolAccessor.PublicGetter(original), Is.EqualTo(original.NBool));
            Assert.That(dateTimeAccessor.PublicGetter(original), Is.EqualTo(original.NDateTime));
            Assert.That(floatAccessor.PublicGetter(original), Is.EqualTo(original.NFloat));
            Assert.That(doubleAccessor.PublicGetter(original), Is.EqualTo(original.NDouble));
            Assert.That(decimalAccessor.PublicGetter(original), Is.EqualTo(original.NDecimal));
            Assert.That(timespanAccessor.PublicGetter(original), Is.EqualTo(original.NTimeSpan));

            var to = ModelWithFieldsOfNullableTypesFactory.Instance.CreateInstance(2);

            idAccessor.PublicSetter(original, to.Id);
            idNAccessor.PublicSetter(original, to.NId);
            longIdAccessor.PublicSetter(original, to.NLongId);
            guidAccessor.PublicSetter(original, to.NGuid);
            boolAccessor.PublicSetter(original, to.NBool);
            dateTimeAccessor.PublicSetter(original, to.NDateTime);
            floatAccessor.PublicSetter(original, to.NFloat);
            doubleAccessor.PublicSetter(original, to.NDouble);
            decimalAccessor.PublicSetter(original, to.NDecimal);
            timespanAccessor.PublicSetter(original, to.NTimeSpan);

            ModelWithFieldsOfNullableTypesFactory.Instance.AssertIsEqual(original, to);

            //Can handle nulls
            original = new ModelWithFieldsOfNullableTypes();

            Assert.That(idAccessor.PublicGetter(original), Is.EqualTo(original.Id));
            Assert.That(idNAccessor.PublicGetter(original), Is.EqualTo(original.NId));
            Assert.That(longIdAccessor.PublicGetter(original), Is.EqualTo(original.NLongId));
            Assert.That(guidAccessor.PublicGetter(original), Is.EqualTo(original.NGuid));
            Assert.That(boolAccessor.PublicGetter(original), Is.EqualTo(original.NBool));
            Assert.That(dateTimeAccessor.PublicGetter(original), Is.EqualTo(original.NDateTime));
            Assert.That(floatAccessor.PublicGetter(original), Is.EqualTo(original.NFloat));
            Assert.That(doubleAccessor.PublicGetter(original), Is.EqualTo(original.NDouble));
            Assert.That(decimalAccessor.PublicGetter(original), Is.EqualTo(original.NDecimal));
            Assert.That(timespanAccessor.PublicGetter(original), Is.EqualTo(original.NTimeSpan));
        }

    }
}