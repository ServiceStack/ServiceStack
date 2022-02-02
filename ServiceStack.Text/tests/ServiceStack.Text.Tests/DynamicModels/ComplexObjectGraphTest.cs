using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels.DataModel;

namespace ServiceStack.Text.Tests.DynamicModels
{
    [TestFixture]
    public class ComplexObjectGraphTest
    {
        [Test]
        public void ShouldSerializeCustomCollection()
        {
            var orig = new CustomCollection
            {
                AddressUri = new Uri("http://www.example.com/"),
                IntValue = 123,
                SomeType = typeof(CustomCollection)
            };

            var s = TypeSerializer.SerializeToString(orig);

            //FYI TypeSerializer is recognized as an IList<CustomCollectionItem>
            //Where CustomCollectionItem has a property of 'object Value' where it 
            //is unable to work out the original types so there deserialized back out as strings
            var clone = TypeSerializer.DeserializeFromString<CustomCollection>(s);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Has.All.No.Null);
            Assert.That(clone.Count, Is.EqualTo(orig.Count));
            Assert.That(clone.AddressUri, Is.EqualTo(orig.AddressUri));
            Assert.That(clone.IntValue, Is.EqualTo(orig.IntValue));
            Assert.That(clone.SomeType, Is.EqualTo(orig.SomeType));

            //Collections are not same, one has object values, the other has string values as explained above
            //Assert.That(clone, Is.EquivalentTo(orig));
        }

        [Test]
        public void ShouldSerializeCustomCollectionDto()
        {
            var orig = new CustomCollectionDto
            {
                //Only saves the Message, i.e. not InnerEx, StackTrace etc.
                Exception = new Exception("Exception Test"),
                CustomException = new CustomException("CustomException Test"),

                AddressUri = new Uri("http://www.example.com/"),
                IntValue = 123,
                SomeType = typeof(CustomCollection),
            };

            var s = TypeSerializer.SerializeToString(orig);

            var clone = TypeSerializer.DeserializeFromString<CustomCollectionDto>(s);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.Exception.Message, Is.EqualTo(orig.Exception.Message));
            Assert.That(clone.Exception.GetType(), Is.EqualTo(orig.Exception.GetType()));
            Assert.That(clone.CustomException.Message, Is.EqualTo(orig.CustomException.Message));
            Assert.That(clone.CustomException.GetType(), Is.EqualTo(orig.CustomException.GetType()));
            Assert.That(clone.AddressUri, Is.EqualTo(orig.AddressUri));
            Assert.That(clone.IntValue, Is.EqualTo(orig.IntValue));
            Assert.That(clone.SomeType, Is.EqualTo(orig.SomeType));
        }

        [Test]
        public void ShouldSerializeCustomCollectionItem()
        {
            var orig = new CustomCollectionItem("Test", "Some Value");

            var s = TypeSerializer.SerializeToString(orig);

            var clone = TypeSerializer.DeserializeFromString<CustomCollectionItem>(s);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.Name, Is.EqualTo(orig.Name));
            Assert.That(clone.Value, Is.EqualTo(orig.Value));
        }

        [Test]
        public void ShouldSerializeDataContainer()
        {
            var orig = new DataContainer
            {
                Exception = new Exception("Test"),
                Identifier = Guid.NewGuid(),
                Object = "Test", //Can't deserialize 'new object()' as an empty object has no serialized form
                Type = this.GetType(),
                TypeList = new List<Type> { typeof(string), this.GetType(), typeof(Int32) },

                //Can't serialize a list of objects, will have no idea what to deserialize it to
                //ObjectList = new List<object> { typeof(string), new Exception("Another Test"), "Teststring" },
            };

            var s = TypeSerializer.SerializeToString(orig);
            Console.WriteLine(s);

            var clone = TypeSerializer.DeserializeFromString<DataContainer>(s);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.Exception.Message, Is.EqualTo(orig.Exception.Message));
            Assert.That(clone.Exception.GetType(), Is.EqualTo(orig.Exception.GetType()));
            Assert.That(clone.Identifier, Is.EqualTo(orig.Identifier));
            Assert.That(clone.Object, Is.EqualTo(orig.Object));
            //Assert.That(clone.ObjectList, Has.All.Not.Null);
            //Assert.That(clone.ObjectList, Is.EquivalentTo(orig.ObjectList));
            Assert.That(clone.Type, Is.EqualTo(orig.Type));
            Assert.That(clone.TypeList, Has.All.Not.Null);
            Assert.That(clone.TypeList, Is.EquivalentTo(orig.TypeList));
        }

        [Test]
        public void ShouldSerializeObjectGraph()
        {
            var dc = new DataContainer
            {
                Exception = new Exception("Test"),
                Identifier = Guid.NewGuid(),
                Object = "Test Object",
                //ObjectList = new List<object> { typeof(string), new Exception("Another Test"), "Teststring" },
                Type = this.GetType(),
                TypeList = new List<Type> { typeof(string), this.GetType(), typeof(Int32) }
            };

            var orig = new ObjectGraph
            {
                AddressUri = new Uri("http://www.example.com/"),
                IntValue = 123,
                SomeType = typeof(CustomCollection),
                Data = dc
            };

            var s = TypeSerializer.SerializeToString(orig);

            var clone = TypeSerializer.DeserializeFromString<ObjectGraph>(s);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.MyCollection, Is.Not.Null);
            Assert.That(clone.MyCollection, Has.All.Not.Null);
            //Collections are not same, one has object values, the other has string values
            //Assert.That(clone.MyCollection, Is.EquivalentTo(orig.MyCollection)); 
            Assert.That(clone.Data, Is.Not.Null);
            //Can't do ref comparisons, they are not the same
            //Assert.That(clone.Data, Is.EqualTo(orig.Data)); 
            Assert.That(clone.AddressUri, Is.EqualTo(orig.AddressUri));
            Assert.That(clone.IntValue, Is.EqualTo(orig.IntValue));
            Assert.That(clone.SomeType, Is.EqualTo(orig.SomeType));
        }

        [Test]
        public void Can_JSV_Serialize_DynamicType_and_Deserialize_into_StrictType()
        {
            var orig = new DynamicType
            {
                Name = "Dynamic Type",
                Type = typeof(CustomCollectionDto),
                Value = new CustomCollectionDto
                {
                    AddressUri = new Uri("http://www.example.com/"),
                    IntValue = 123,
                    SomeType = typeof(int)
                }
            };

            var jsv = TypeSerializer.SerializeToString(orig);
            var strictType = TypeSerializer.DeserializeFromString<StrictType>(jsv);
            var dynamicType = TypeSerializer.DeserializeFromString<DynamicType>(jsv);

            var origDto = (CustomCollectionDto)orig.Value;

            Assert.That(strictType, Is.Not.Null);
            Assert.That(strictType.Name, Is.EqualTo(orig.Name));
            Assert.That(strictType.Type, Is.EqualTo(orig.Type));
            Assert.That(strictType.Value.AddressUri, Is.EqualTo(origDto.AddressUri));
            Assert.That(strictType.Value.IntValue, Is.EqualTo(origDto.IntValue));
            Assert.That(strictType.Value.SomeType, Is.EqualTo(origDto.SomeType));

            Assert.That(dynamicType, Is.Not.Null);
            Assert.That(dynamicType.Name, Is.EqualTo(orig.Name));
            Assert.That(dynamicType.Type, Is.EqualTo(orig.Type));
            var dynamicValue = (CustomCollectionDto)dynamicType.GetTypedValue();
            Assert.That(dynamicValue.AddressUri, Is.EqualTo(origDto.AddressUri));
            Assert.That(dynamicValue.IntValue, Is.EqualTo(origDto.IntValue));
            Assert.That(dynamicValue.SomeType, Is.EqualTo(origDto.SomeType));
        }

        [Test]
        public void Can_JSON_Serialize_DynamicType_and_Deserialize_into_StrictType()
        {
            var orig = new DynamicType
            {
                Name = "Dynamic Type",
                Type = typeof(CustomCollectionDto),
                Value = new CustomCollectionDto
                {
                    AddressUri = new Uri("http://www.example.com/"),
                    IntValue = 123,
                    SomeType = typeof(int)
                }
            };

            var jsv = JsonSerializer.SerializeToString(orig);
            var strictType = JsonSerializer.DeserializeFromString<StrictType>(jsv);
            var dynamicType = JsonSerializer.DeserializeFromString<DynamicType>(jsv);

            var origDto = (CustomCollectionDto)orig.Value;

            Assert.That(strictType, Is.Not.Null);
            Assert.That(strictType.Name, Is.EqualTo(orig.Name));
            Assert.That(strictType.Type, Is.EqualTo(orig.Type));
            Assert.That(strictType.Value.AddressUri, Is.EqualTo(origDto.AddressUri));
            Assert.That(strictType.Value.IntValue, Is.EqualTo(origDto.IntValue));
            Assert.That(strictType.Value.SomeType, Is.EqualTo(origDto.SomeType));

            Assert.That(dynamicType, Is.Not.Null);
            Assert.That(dynamicType.Name, Is.EqualTo(orig.Name));
            Assert.That(dynamicType.Type, Is.EqualTo(orig.Type));
            var dynamicValue = (CustomCollectionDto)dynamicType.GetTypedValue();
            Assert.That(dynamicValue.AddressUri, Is.EqualTo(origDto.AddressUri));
            Assert.That(dynamicValue.IntValue, Is.EqualTo(origDto.IntValue));
            Assert.That(dynamicValue.SomeType, Is.EqualTo(origDto.SomeType));
        }


    }
}