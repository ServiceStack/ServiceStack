using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Reflection;

namespace ServiceStack.Common.Tests.Reflection
{
    [TestFixture]
    public class PropertyAccessorTests
    {
        [Test]
        public void Can_access_ModelWithIdAndName()
        {
            var idAccessor = new PropertyAccessor<ModelWithIdAndName>("Id");
            var nameAccessor = new PropertyAccessor<ModelWithIdAndName>("Name");

            var obj = new ModelWithIdAndName { Id = 1, Name = "A" };

            Assert.That(idAccessor.GetPropertyFn()(obj), Is.EqualTo(1));
            Assert.That(nameAccessor.GetPropertyFn()(obj), Is.EqualTo("A"));

            idAccessor.SetPropertyFn()(obj, 2);
            nameAccessor.SetPropertyFn()(obj, "B");

            Assert.That(obj.Id, Is.EqualTo(2));
            Assert.That(obj.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Can_access_ModelWithFieldsOfDifferentTypes()
        {
            var idAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("Id");
            var nameAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("Name");
            var longIdAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("LongId");
            var guidAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("Guid");
            var boolAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("Bool");
            var dateTimeAccessor = new PropertyAccessor<ModelWithFieldsOfDifferentTypes>("DateTime");

            var original = ModelWithFieldsOfDifferentTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.GetPropertyFn()(original), Is.EqualTo(original.Id));
            Assert.That(nameAccessor.GetPropertyFn()(original), Is.EqualTo(original.Name));
            Assert.That(longIdAccessor.GetPropertyFn()(original), Is.EqualTo(original.LongId));
            Assert.That(guidAccessor.GetPropertyFn()(original), Is.EqualTo(original.Guid));
            Assert.That(boolAccessor.GetPropertyFn()(original), Is.EqualTo(original.Bool));
            Assert.That(dateTimeAccessor.GetPropertyFn()(original), Is.EqualTo(original.DateTime));

            var to = ModelWithFieldsOfDifferentTypesFactory.Instance.CreateInstance(2);

            idAccessor.SetPropertyFn()(original, to.Id);
            nameAccessor.SetPropertyFn()(original, to.Name);
            longIdAccessor.SetPropertyFn()(original, to.LongId);
            guidAccessor.SetPropertyFn()(original, to.Guid);
            boolAccessor.SetPropertyFn()(original, to.Bool);
            dateTimeAccessor.SetPropertyFn()(original, to.DateTime);

            ModelWithFieldsOfDifferentTypesFactory.Instance.AssertIsEqual(original, to);
        }

        [Test]
        public void Can_access_ModelWithComplexTypes()
        {
            var idAccessor = new PropertyAccessor<ModelWithComplexTypes>("Id");
            var stringListAccessor = new PropertyAccessor<ModelWithComplexTypes>("StringList");
            var intListAccessor = new PropertyAccessor<ModelWithComplexTypes>("IntList");
            var stringMapAccessor = new PropertyAccessor<ModelWithComplexTypes>("StringMap");
            var intMapAccessor = new PropertyAccessor<ModelWithComplexTypes>("IntMap");
            var childAccessor = new PropertyAccessor<ModelWithComplexTypes>("Child");

            var original = ModelWithComplexTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.GetPropertyFn()(original), Is.EqualTo(original.Id));
            Assert.That(stringListAccessor.GetPropertyFn()(original), Is.EqualTo(original.StringList));
            Assert.That(intListAccessor.GetPropertyFn()(original), Is.EqualTo(original.IntList));
            Assert.That(stringMapAccessor.GetPropertyFn()(original), Is.EqualTo(original.StringMap));
            Assert.That(intMapAccessor.GetPropertyFn()(original), Is.EqualTo(original.IntMap));
            Assert.That(childAccessor.GetPropertyFn()(original), Is.EqualTo(original.Child));

            var to = ModelWithComplexTypesFactory.Instance.CreateInstance(2);

            idAccessor.SetPropertyFn()(original, to.Id);
            stringListAccessor.SetPropertyFn()(original, to.StringList);
            intListAccessor.SetPropertyFn()(original, to.IntList);
            stringMapAccessor.SetPropertyFn()(original, to.StringMap);
            intMapAccessor.SetPropertyFn()(original, to.IntMap);
            childAccessor.SetPropertyFn()(original, to.Child);

            ModelWithComplexTypesFactory.Instance.AssertIsEqual(original, to);
        }

        [Test]
        public void Can_access_ModelWithFieldsOfDifferentAndNullableTypes()
        {
            var idAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("Id");
            var idNAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NId");
            var longIdAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NLongId");
            var guidAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NGuid");
            var boolAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NBool");
            var dateTimeAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NDateTime");
            var floatAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NFloat");
            var doubleAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NDouble");
            var decimalAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NDecimal");
            var timespanAccessor = new PropertyAccessor<ModelWithFieldsOfNullableTypes>("NTimeSpan");

            var original = ModelWithFieldsOfNullableTypesFactory.Instance.CreateInstance(1);

            Assert.That(idAccessor.GetPropertyFn()(original), Is.EqualTo(original.Id));
            Assert.That(idNAccessor.GetPropertyFn()(original), Is.EqualTo(original.NId));
            Assert.That(longIdAccessor.GetPropertyFn()(original), Is.EqualTo(original.NLongId));
            Assert.That(guidAccessor.GetPropertyFn()(original), Is.EqualTo(original.NGuid));
            Assert.That(boolAccessor.GetPropertyFn()(original), Is.EqualTo(original.NBool));
            Assert.That(dateTimeAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDateTime));
            Assert.That(floatAccessor.GetPropertyFn()(original), Is.EqualTo(original.NFloat));
            Assert.That(doubleAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDouble));
            Assert.That(decimalAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDecimal));
            Assert.That(timespanAccessor.GetPropertyFn()(original), Is.EqualTo(original.NTimeSpan));

            var to = ModelWithFieldsOfNullableTypesFactory.Instance.CreateInstance(2);

            idAccessor.SetPropertyFn()(original, to.Id);
            idNAccessor.SetPropertyFn()(original, to.NId);
            longIdAccessor.SetPropertyFn()(original, to.NLongId);
            guidAccessor.SetPropertyFn()(original, to.NGuid);
            boolAccessor.SetPropertyFn()(original, to.NBool);
            dateTimeAccessor.SetPropertyFn()(original, to.NDateTime);
            floatAccessor.SetPropertyFn()(original, to.NFloat);
            doubleAccessor.SetPropertyFn()(original, to.NDouble);
            decimalAccessor.SetPropertyFn()(original, to.NDecimal);
            timespanAccessor.SetPropertyFn()(original, to.NTimeSpan);

            ModelWithFieldsOfNullableTypesFactory.Instance.AssertIsEqual(original, to);

            //Can handle nulls
            original = new ModelWithFieldsOfNullableTypes();

            Assert.That(idAccessor.GetPropertyFn()(original), Is.EqualTo(original.Id));
            Assert.That(idNAccessor.GetPropertyFn()(original), Is.EqualTo(original.NId));
            Assert.That(longIdAccessor.GetPropertyFn()(original), Is.EqualTo(original.NLongId));
            Assert.That(guidAccessor.GetPropertyFn()(original), Is.EqualTo(original.NGuid));
            Assert.That(boolAccessor.GetPropertyFn()(original), Is.EqualTo(original.NBool));
            Assert.That(dateTimeAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDateTime));
            Assert.That(floatAccessor.GetPropertyFn()(original), Is.EqualTo(original.NFloat));
            Assert.That(doubleAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDouble));
            Assert.That(decimalAccessor.GetPropertyFn()(original), Is.EqualTo(original.NDecimal));
            Assert.That(timespanAccessor.GetPropertyFn()(original), Is.EqualTo(original.NTimeSpan));
        }

    }
}